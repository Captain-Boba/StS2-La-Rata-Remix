using System.Text.Json;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

internal static class NightRelicChecks
{
    private static readonly PlayerChoiceContext Context=new BlockingPlayerChoiceContext();
    private static void Require(bool value,string reason){if(!value)throw new Exception(reason);}
    private static bool Active(ref bool __result){__result=false;return false;}
    private static bool InProgress(ref bool __result){__result=true;return false;}
    private static decimal Metric(RelicModel r,string key)=>RelicMetrics.Read(r).Metrics.Where(m=>m.Key==key).Sum(m=>m.Value);
    private static T Obtain<T>(Player p) where T:RelicModel,new(){var r=(T)ModelDb.Relic<T>().ToMutable();p.AddRelicInternal(r);return r;}
    private static T Card<T>(RunState run,Player p,PileType pile=PileType.Hand) where T:CardModel,new()
    {var c=(T)p.Creature.CombatState!.CreateCard(ModelDb.Card<T>(),p);pile.GetPile(p).AddInternal(c);return c;}
    private static CardPlay Play(CardModel c,int energy,bool auto=false,int index=0)=>new()
    {Card=c,Target=null,ResultPile=PileType.Discard,Resources=new(){EnergySpent=energy,EnergyValue=energy,StarsSpent=0,StarValue=0},IsAutoPlay=auto,PlayIndex=index,PlayCount=index+1};
    private static T Restore<T>(T relic,Player owner) where T:RelicModel
    {
        string json=JsonSerializer.Serialize(relic.ToSerializable(),JsonSerializationUtility.GetTypeInfo<SerializableRelic>());
        var result=(T)RelicModel.FromSerializable(JsonSerializer.Deserialize(json,JsonSerializationUtility.GetTypeInfo<SerializableRelic>())!);
        result.Owner=owner;return result;
    }
    private static (RunState run,Player a,Player b,Creature[] enemies) Setup()
    {
        var a=NativeConsoleFixture.Player(701);var b=NativeConsoleFixture.Player(702);
        var run=RunState.CreateForTest(players:[a,b],seed:"NIGHTRELICS");
        NativeConsoleFixture.Set(RunManager.Instance,"State",run);
        NativeConsoleFixture.Set(RunManager.Instance,nameof(RunManager.AscensionManager),new MegaCrit.Sts2.Core.Entities.Ascension.AscensionManager(0));
        var combat=new CombatState(ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable(),run);
        AccessTools.Field(typeof(CombatManager),"_state").SetValue(CombatManager.Instance,combat);
        foreach(var p in new[]{a,b})
        {
            var body=new Creature(p,50,50){CombatState=combat};NativeConsoleFixture.Set(p,nameof(p.Creature),body);
            p.ResetCombatState();combat.AddCreature(body);p.PlayerCombatState!.Energy=10;p.PlayerCombatState.Stars=10;
        }
        var enemies=Enumerable.Range(0,3).Select(i=>new Creature(ModelDb.Monster<TwoTailedRat>().ToMutable(),CombatSide.Enemy,i.ToString()){CombatState=combat}).ToArray();
        foreach(var enemy in enemies)combat.AddCreature(enemy);
        return(run,a,b,enemies);
    }
    internal static void Run(Harmony harmony)
    {
        var oldRun=RunManager.Instance.DebugOnlyGetState();
        var oldAscension=RunManager.Instance.AscensionManager;
        var stateField=AccessTools.Field(typeof(CombatManager),"_state");var oldCombat=stateField.GetValue(CombatManager.Instance);
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager),nameof(CombatManager.IsInProgress)),prefix:new(typeof(NightRelicChecks),nameof(InProgress)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager),nameof(CombatManager.IsOverOrEnding)),prefix:new(typeof(NightRelicChecks),nameof(Active)));
        try
        {
            var failures=new List<Exception>();
            foreach(var test in new Action[]{Ticket,Key,Bell,Scavenger,Stopper,StopperCostSignals})
                try{test();}catch(Exception e){Console.WriteLine("FAIL "+test.Method.Name+": "+e);failures.Add(e);}
            if(failures.Count>0)throw new AggregateException(failures);
            var expected=new[]{typeof(SootboundTicket),typeof(StairwellKey),typeof(MockingBell),typeof(BrassScavenger),typeof(MoonlitStopper)};
            var pool=ModelDb.RelicPool<SharedRelicPool>().AllRelics.ToArray();
            foreach(var type in expected)Require(pool.Count(r=>r.GetType()==type)==1,"Missing/duplicated shared pool relic: "+type.Name);
            Console.WriteLine("PASS: five new native relic models, owner/cap/non-trigger cases, native effect commands and SavedProperty JSON checkpoints. Direct hook boundaries and desktop/combat guards substituted; no full gameplay, disk save or network evidence.");
        }
        finally{NativeConsoleFixture.Set(RunManager.Instance,"State",oldRun);NativeConsoleFixture.Set(RunManager.Instance,nameof(RunManager.AscensionManager),oldAscension);stateField.SetValue(CombatManager.Instance,oldCombat);}
    }
    private static void Ticket()
    {
        var(run,a,b,_)=Setup();var ticket=Obtain<SootboundTicket>(a);
        var high=Card<Bash>(run,a);var foreign=Card<Bash>(run,b,PileType.Exhaust);
        var discard=Card<DefendIronclad>(run,a,PileType.Discard);
        int rng=run.Rng.CombatCardSelection.Counter;
        ticket.AfterCardExhausted(Context,foreign,false).GetAwaiter().GetResult();
        ticket.AfterCardExhausted(Context,high,true).GetAwaiter().GetResult();
        Require(!ticket.TriggeredThisTurn&&run.Rng.CombatCardSelection.Counter==rng,"Ticket foreign/Ethereal consumed trigger/RNG");
        CardCmd.Exhaust(Context,high,skipVisuals:true).GetAwaiter().GetResult();
        Require(!ticket.TriggeredThisTurn&&discard.Pile?.Type==PileType.Discard&&
            run.Rng.CombatCardSelection.Counter==rng&&Metric(ticket,"cards_recovered")==0,
            "Ticket retained the retired exhaust recovery effect or statistics");
        // Retained legacy save field must not activate the retired effect or
        // consume the new first-card-per-merchant allowance.
        ticket.TriggeredThisTurn=true; ticket=Restore(ticket,a);
        ticket.AfterCardExhausted(Context,high,false).GetAwaiter().GetResult();
        Require(discard.Pile?.Type==PileType.Discard&&ticket.SpentMerchant=="",
            "Legacy Ticket flag changed the new contract");
        var entry=new MerchantCardEntry(a,null,[ModelDb.Card<Bash>()],CardType.Attack);
        Require(ticket.Rarity==RelicRarity.Rare&&ticket.ModifyMerchantPrice(a,entry,120)==120,
            "Ticket rarity or non-shop behavior differs");
        run.PushRoom(new MerchantRoom());run.AppendToMapPointHistory(MapPointType.Shop,RoomType.Shop,null);
        Require(ticket.ModifyMerchantPrice(a,entry,120)==0&&ticket.ModifyMerchantPrice(b,entry,120)==120,
            "Ticket free price must be owner-scoped and ignore legacy exhaust state");
        Require(ticket.SpentMerchant=="","Price preview consumed Ticket");
        ticket.SpentMerchant=SootboundMerchantPurchase.MerchantKey(a);
        var spent=Restore(ticket,a);
        Require(spent.ModifyMerchantPrice(a,entry,120)==120,"Spent merchant receipt lost in native JSON");
        run.CurrentMapPointHistoryEntry!.Rooms.Add(new() { RoomType=RoomType.Shop });
        Require(spent.ModifyMerchantPrice(a,entry,120)==0,"New merchant did not restore free-card allowance");
        Console.WriteLine("PASS Ticket: retired exhaust effect inert, legacy JSON retained, Rare, owner-only free-card price, preview does not consume, spent receipt JSON and next merchant. Native purchase/cancel/packet cases are in --content-wave-relics.");
    }
    private static void Key()
    {
        var(run,a,b,_)=Setup();var key=Obtain<StairwellKey>(a);var c=Card<Bash>(run,a);
        Require(c.EnergyCost.GetAmountToSpend()==1&&!key.TriggeredThisTurn,"First-card price/preview");
        var zero=Card<Bash>(run,a);zero.EnergyCost.SetUntilPlayed(0);
        zero.SpendResources().GetAwaiter().GetResult();
        key.BeforeCardPlayed(Play(Card<Bash>(run,b),0)).GetAwaiter().GetResult();
        Require(!key.TriggeredThisTurn,"Foreign card consumed first play");
        key.BeforeCardPlayed(Play(zero,0)).GetAwaiter().GetResult();
        Require(key.TriggeredThisTurn&&c.EnergyCost.GetAmountToSpend()==2&&Restore(key,a).TriggeredThisTurn,
            "First zero-cost card must consume availability, including native JSON");
        int energy=a.PlayerCombatState!.Energy,hand=a.PlayerCombatState.Hand.Cards.Count;
        foreach(int cost in new[]{2,1,0})key.AfterCardPlayed(Context,Play(c,cost)).GetAwaiter().GetResult();
        Require(a.PlayerCombatState.Energy==energy&&a.PlayerCombatState.Hand.Cards.Count==hand,"Old Energy/Draw effect remains");
        key.BeforeSideTurnStart(Context,CombatSide.Player,[a.Creature,b.Creature],a.Creature.CombatState!).GetAwaiter().GetResult();
        Require(!key.TriggeredThisTurn&&c.EnergyCost.GetAmountToSpend()==1,"Next turn failed to reset first-card discount");
        Console.WriteLine("PASS Key: first-card discount, zero-cost consumption, owner isolation, native JSON and turn reset; old Energy/Draw effect removed. Extended cases in --content-wave-relics.");
    }
    private static void Bell()
    {
        {
            var sample=Setup();var fractional=Obtain<MockingBell>(sample.a);
            PowerCmd.Apply<PoisonPower>(Context,sample.enemies[0],.5m,sample.a.Creature,null).GetAwaiter().GetResult();
            Require(!fractional.TriggeredThisTurn&&sample.enemies.Sum(c=>c.GetPowerAmount<WeakPower>())==0,
                $"Bell triggered without an actual integer Debuff: Poison={sample.enemies[0].GetPowerAmount<PoisonPower>()}, Weak={sample.enemies.Sum(c=>c.GetPowerAmount<WeakPower>())}, armed={fractional.TriggeredThisTurn}");
        }
        {
            var sample=Setup();var blocked=Obtain<MockingBell>(sample.a);
            ModelDb.Power<ArtifactPower>().ToMutable().ApplyInternal(sample.enemies[0],1,true);
            PowerCmd.Apply<PoisonPower>(Context,sample.enemies[0],2,sample.a.Creature,null).GetAwaiter().GetResult();
            Require(!blocked.TriggeredThisTurn&&sample.enemies.Sum(c=>c.GetPowerAmount<WeakPower>())==0,"Artifact-blocked Debuff triggered Bell");
        }
        var(run,a,b,enemies)=Setup();var bell=Obtain<MockingBell>(a);
        var poison=ModelDb.Power<PoisonPower>().ToMutable();poison.ApplyInternal(enemies[0],2,true);
        bell.AfterPowerAmountChanged(Context,poison,2,b.Creature,null).GetAwaiter().GetResult();
        bell.AfterPowerAmountChanged(Context,poison,-1,a.Creature,null).GetAwaiter().GetResult();
        Require(!bell.TriggeredThisTurn,"Bell foreign/debuff reduction counted");
        PowerCmd.Apply<PoisonPower>(Context,enemies[0],2,a.Creature,null).GetAwaiter().GetResult();
        Require(bell.TriggeredThisTurn&&enemies[0].GetPowerAmount<WeakPower>()==0&&enemies.Skip(1).Sum(c=>c.GetPowerAmount<WeakPower>())==1,"Bell wrong target/recursive payout");
        Require(Metric(bell,"power_applied:other")==0,"Disabled Bell statistics recorded an effect");
        bell.AfterPowerAmountChanged(Context,poison,2,a.Creature,null).GetAwaiter().GetResult();
        Require(enemies.Sum(c=>c.GetPowerAmount<WeakPower>())==1&&Restore(bell,a).TriggeredThisTurn,"Bell cap/JSON differs");
        Console.WriteLine("PASS Bell: native Weak on a different enemy, no recursion, foreign/reduction negative cases, cap and JSON.");
    }
    private static void Scavenger()
    {
        var(run,a,b,enemies)=Setup();var relic=Obtain<BrassScavenger>(a);var card=Card<Bash>(run,a);var foreign=Card<Bash>(run,b);
        var result=new DamageResult(enemies[0],ValueProp.Move){BlockedDamage=20,WasBlockBroken=true};int gold=a.Gold;
        relic.AfterDamageGiven(Context,b.Creature,result,ValueProp.Move,enemies[0],foreign).GetAwaiter().GetResult();
        Require(!relic.UsedThisCombat&&a.Gold==gold,"Scavenger foreign damage paid");
        relic.AfterDamageGiven(Context,a.Creature,result,ValueProp.Move,enemies[0],card).GetAwaiter().GetResult();
        relic.AfterDamageGiven(Context,a.Creature,result,ValueProp.Move,enemies[0],card).GetAwaiter().GetResult();
        Require(relic.UsedThisCombat&&a.Gold==gold+20&&Restore(relic,a).UsedThisCombat,"Scavenger actual uncapped gold/once-per-combat/JSON differs");
        Require(Metric(relic,"gold_gained")==0,"Disabled Scavenger statistics recorded gold");
        Console.WriteLine("PASS Scavenger: supplied native DamageResult boundary, foreign owner rejected, full 20 blocked damage paid once, JSON; statistics remain disabled.");
    }
    private static void StopperCostSignals()
    {
        var(run,a,b,_)=Setup();var stopper=Obtain<MoonlitStopper>(a);
        var own=Card<Bash>(run,a);var stars=Card<FallingStar>(run,a);
        var foreign=Card<Bash>(run,b);var x=Card<Whirlwind>(run,a);
        int ownSignals=0,starSignals=0,foreignSignals=0,xSignals=0;
        own.EnergyCostChanged+=()=>ownSignals++;
        stars.EnergyCostChanged+=()=>starSignals++;
        foreign.EnergyCostChanged+=()=>foreignSignals++;
        x.EnergyCostChanged+=()=>xSignals++;
        var potion=ModelDb.Potion<StrengthPotion>().ToMutable();a.AddToMaxPotionCount(3);a.AddPotionInternal(potion);
        PotionCmd.Discard(potion).GetAwaiter().GetResult();
        Require(ownSignals==1&&starSignals==1&&foreignSignals==0&&xSignals==0,"Stopper arm did not invalidate only own non-X hand costs");
        for(int i=0;i<5;i++)_ = own.EnergyCost.GetAmountToSpend();
        stopper.Armed=true;
        Require(ownSignals==1&&starSignals==1,"Preview/no-op arm emitted cost changes");
        stopper.AfterCardPlayed(Context,Play(own,0)).GetAwaiter().GetResult();
        Require(ownSignals==2&&starSignals==2&&own.EnergyCost.GetAmountToSpend()==2,"Consumption did not restore visible cost signal");
        stopper.BeforeCombatStart().GetAwaiter().GetResult();stopper.AfterPotionDiscarded(potion).GetAwaiter().GetResult();
        stopper.BeforeSideTurnEnd(Context,CombatSide.Player,[a.Creature,b.Creature]).GetAwaiter().GetResult();
        Require(ownSignals==4&&starSignals==4&&!stopper.Armed,"Turn expiry did not invalidate costs");
        stopper.BeforeCombatStart().GetAwaiter().GetResult();stopper.AfterPotionDiscarded(potion).GetAwaiter().GetResult();
        a.RemoveRelicInternal(stopper,true);stopper.AfterRemoved().GetAwaiter().GetResult();
        Require(ownSignals==6&&starSignals==6&&!stopper.Armed&&own.EnergyCost.GetAmountToSpend()==2,"Removal did not restore hand costs");
        Require(foreignSignals==0&&xSignals==0,"Foreign/X cards received unrelated invalidation");
        Console.WriteLine("PASS Stopper native hand-cost signals: arm, no-op/query, consume, turn expiry, removal; Energy/Stars cards included, foreign/X excluded. UI rendering is a separate live check.");
    }
    private static void Stopper()
    {
        var(run,a,b,_)=Setup();var stopper=Obtain<MoonlitStopper>(a);var card=Card<Bash>(run,a);var foreign=Card<Bash>(run,b);
        var potion=ModelDb.Potion<StrengthPotion>().ToMutable();potion.Owner=b;
        stopper.AfterPotionDiscarded(potion).GetAwaiter().GetResult();Require(!stopper.Armed,"Stopper foreign potion armed");
        potion=ModelDb.Potion<StrengthPotion>().ToMutable();a.AddToMaxPotionCount(3);a.AddPotionInternal(potion);
        PotionCmd.Discard(potion).GetAwaiter().GetResult();
        Require(stopper.Armed&&stopper.UsedThisCombat&&Restore(stopper,a).Armed,"Stopper arming/JSON failed");
        var xCard=Card<Whirlwind>(run,a);
        Require(!stopper.TryModifyEnergyCostInCombatLate(xCard,3,out _),"Stopper discounted X card");
        stopper.AfterCardPlayed(Context,Play(xCard,3)).GetAwaiter().GetResult();Require(stopper.Armed,"X card consumed Stopper");
        Require(stopper.TryModifyEnergyCostInCombatLate(card,2,out var reduced)&&reduced==0&&!stopper.TryModifyEnergyCostInCombatLate(foreign,2,out _),"Stopper owner discount wrong");
        for(int i=0;i<5;i++)_ = card.EnergyCost.GetAmountToSpend();Require(Metric(stopper,"energy_saved")==0,"Stopper preview counted savings");
        int energy=a.PlayerCombatState!.Energy;card.SpendResources().GetAwaiter().GetResult();
        Require(a.PlayerCombatState.Energy==energy&&Metric(stopper,"energy_saved")==0,"Stopper zero payment or disabled statistics differs");
        stopper.AfterCardPlayed(Context,Play(card,0,true)).GetAwaiter().GetResult();Require(stopper.Armed,"Stopper autoplay consumed arming");
        stopper.AfterCardPlayed(Context,Play(card,0)).GetAwaiter().GetResult();Require(!stopper.Armed,"Stopper manual play did not consume");
        stopper.AfterPotionDiscarded(potion).GetAwaiter().GetResult();Require(!stopper.Armed,"Stopper second combat trigger allowed");
        stopper.BeforeCombatStart().GetAwaiter().GetResult();stopper.AfterPotionDiscarded(potion).GetAwaiter().GetResult();
        var starCard=Card<FallingStar>(run,a);int stars=a.PlayerCombatState.Stars;
        starCard.SpendResources().GetAwaiter().GetResult();
        Require(a.PlayerCombatState.Stars==stars&&Metric(stopper,"stars_saved")==0,"Stopper zero Stars payment or disabled statistics differs");
        stopper.BeforeSideTurnEnd(Context,CombatSide.Player,[a.Creature,b.Creature]).GetAwaiter().GetResult();
        Require(!stopper.Armed&&stopper.UsedThisCombat,"Stopper end turn did not expire or reset combat cap");
        Console.WriteLine("PASS Stopper: owner arm/JSON, actual zero Energy/Stars payments, auto/manual consumption and combat cap; statistics remain disabled.");
    }
}
