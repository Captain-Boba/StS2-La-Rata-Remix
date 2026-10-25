using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using System.Text.Json;

// Acceptance expectations come from the user's follow-up and K2's explicit
// design contract, not from outputs sampled from the implementation.
internal static class FollowupCardChecks
{
    private static readonly PlayerChoiceContext Context = new BlockingPlayerChoiceContext();
    private static void Require(bool ok,string why){if(!ok)throw new Exception(why);}
    private static bool Active(ref bool __result){__result=false;return false;}
    private static bool InProgress(ref bool __result){__result=true;return false;}
    private static TaskCompletionSource? refundGate;
    private static bool PauseFirstRefund(ref Task __result)
    {
        if(refundGate==null)return true;
        __result=refundGate.Task;refundGate=null;return false;
    }
    private static (RunState run,Player a,Player b,Creature[] enemies) Setup()
    {
        var a=NativeConsoleFixture.Player(801);var b=NativeConsoleFixture.Player(802);
        var run=RunState.CreateForTest(players:[a,b],seed:"CARDDELTA");
        NativeConsoleFixture.Set(RunManager.Instance,"State",run);
        NativeConsoleFixture.Set(RunManager.Instance,nameof(RunManager.AscensionManager),new MegaCrit.Sts2.Core.Entities.Ascension.AscensionManager(0));
        var combat=new CombatState(ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable(),run);
        AccessTools.Field(typeof(CombatManager),"_state").SetValue(CombatManager.Instance,combat);
        foreach(var p in new[]{a,b})
        {
            var body=new Creature(p,100,100){CombatState=combat};NativeConsoleFixture.Set(p,nameof(p.Creature),body);
            p.ResetCombatState();combat.AddCreature(body);p.PlayerCombatState!.Energy=10;
        }
        var enemies=Enumerable.Range(0,4).Select(i=>new Creature(ModelDb.Monster<TwoTailedRat>().ToMutable(),CombatSide.Enemy,i.ToString()){CombatState=combat}).ToArray();
        foreach(var e in enemies)combat.AddCreature(e);
        return(run,a,b,enemies);
    }
    private static T Card<T>(Player p,bool upgraded=false,PileType pile=PileType.Hand) where T:CardModel,new()
    {
        var c=(T)p.Creature.CombatState!.CreateCard(ModelDb.Card<T>(),p);
        if(upgraded){c.UpgradeInternal();c.FinalizeUpgradeInternal();}
        pile.GetPile(p).AddInternal(c);return c;
    }
    private static CardPlay Play(CardModel card,Creature? target=null)=>new(){Card=card,Target=target,ResultPile=PileType.Discard,Resources=new(){EnergySpent=0,EnergyValue=0,StarsSpent=0,StarValue=0},IsAutoPlay=false,PlayIndex=0,PlayCount=1};
    private static void Resolve(CardModel card,Creature? target=null)
    {
        CardPileCmd.Add(card,PileType.Play,skipVisuals:true).GetAwaiter().GetResult();
        ((Task)AccessTools.Method(card.GetType(),"OnPlay").Invoke(card,[Context,Play(card,target)])!).GetAwaiter().GetResult();
    }
    private static void Power<T>(Creature c,int n) where T:PowerModel,new()=>ModelDb.Power<T>().ToMutable().ApplyInternal(c,n,true);
    internal static void Run(Harmony harmony)
    {
        var oldRun=RunManager.Instance.DebugOnlyGetState();var oldAscension=RunManager.Instance.AscensionManager;
        var field=AccessTools.Field(typeof(CombatManager),"_state");var oldCombat=field.GetValue(CombatManager.Instance);
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager),nameof(CombatManager.IsInProgress)),prefix:new(typeof(FollowupCardChecks),nameof(InProgress)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager),nameof(CombatManager.IsOverOrEnding)),prefix:new(typeof(FollowupCardChecks),nameof(Active)));
        try
        {
            var errors=new List<Exception>();
            foreach(var test in new Action[]{CardWaveChecks.Run,Thorn,Graveyard,Court,Shadow,Alchemy,Reset,CardIdentityRoundtrip,RoyalFollowupChecks.Run})
                try{test();Console.WriteLine("PASS follow-up "+test.Method.Name);}catch(Exception e){Console.WriteLine("FAIL follow-up "+test.Method.Name+": "+e);errors.Add(e);}
            try{ResetAwaitBoundaries(harmony);Console.WriteLine("PASS follow-up ResetAwaitBoundaries");}catch(Exception e){Console.WriteLine("FAIL follow-up ResetAwaitBoundaries: "+e);errors.Add(e);}
            if(errors.Count>0)throw new AggregateException(errors);
            Console.WriteLine("PASS: changed card effects use native status removal/application, pile draws and block commands. OnPlay/card-hook boundaries and active-combat guards are supplied; not full native card-play, disk save, UI or live networking.");
        }
        finally{NativeConsoleFixture.Set(RunManager.Instance,"State",oldRun);NativeConsoleFixture.Set(RunManager.Instance,nameof(RunManager.AscensionManager),oldAscension);field.SetValue(CombatManager.Instance,oldCombat);}
    }
    private static void Thorn()
    {
        foreach(bool upgraded in new[]{false,true})
        {
            var(_,a,b,es)=Setup();Power<ThornsPower>(a.Creature,7);Power<ThornsPower>(b.Creature,9);
            Power<ThornsPower>(es[1],1);Power<ThornsPower>(es[2],19);Power<ArtifactPower>(es[2],1);Power<ArtifactPower>(es[3],1);
            for(int i=0;i<3;i++)Card<DefendIronclad>(a,pile:PileType.Draw);
            var c=Card<SerKevsThornBuster>(a,upgraded);
            Require(c.EnergyCost.GetWithModifiers(CostModifiers.None)==1 && c.Keywords.Contains(CardKeyword.Exhaust),"Thorn cost/Exhaust");
            Resolve(c);
            Require(es.All(e=>e.GetPowerAmount<ThornsPower>()==0),"Thorn removal incomplete");
            Require(es.Select(e=>e.GetPowerAmount<VulnerablePower>()).SequenceEqual(new[]{0,3,0,0}),"Vulnerable target selection or Artifact differs");
            Require(es[2].GetPowerAmount<ArtifactPower>()==0&&es[3].GetPowerAmount<ArtifactPower>()==1,"Artifact consumed without removed Thorns");
            Require(a.Creature.GetPowerAmount<ThornsPower>()==7&&b.Creature.GetPowerAmount<ThornsPower>()==9,"Player Thorns modified");
            Require(a.PlayerCombatState!.Hand.Cards.Count==(upgraded?2:0)&&b.PlayerCombatState!.Hand.Cards.Count==0,"Thorn draw count or owner");
        }
    }
    private static void Graveyard()
    {
        foreach(bool upgraded in new[]{false,true})foreach(int vulnerable in new[]{0,1,3})foreach(bool modifiers in new[]{false,true})
        {
            var(_,a,b,es)=Setup();if(vulnerable>0)Power<VulnerablePower>(es[0],vulnerable);
            Power<VulnerablePower>(es[1],4);Power<ArtifactPower>(es[0],1);
            if(modifiers){Power<DexterityPower>(a.Creature,2);Power<FrailPower>(a.Creature,1);}
            var c=Card<GraveyardShift>(a,upgraded);
            Require(c.TargetType==TargetType.AnyEnemy&&c.EnergyCost.GetWithModifiers(CostModifiers.None)==1&&c.Keywords.Contains(CardKeyword.Exhaust),"Graveyard type/cost/Exhaust");
            Resolve(c,es[0]);
            int raw=vulnerable*(upgraded?9:6);
            int expected=vulnerable==0?0:modifiers?(int)Math.Floor((raw+2)*.75m):raw;
            Require(a.Creature.Block==expected&&b.Creature.Block==0,$"Graveyard block should be {expected}, got {a.Creature.Block}");
            Require(es[0].GetPowerAmount<VulnerablePower>()==0&&es[1].GetPowerAmount<VulnerablePower>()==4&&es[0].GetPowerAmount<ArtifactPower>()==1,"Graveyard removal scope/Artifact");
            Require(a.Creature.CurrentHp==100&&a.PlayerCombatState!.Hand.Cards.Count==0,"Old Graveyard HP/draw effect remains");
        }
    }
    private static void Court()
    {
        foreach(bool upgraded in new[]{false,true})foreach(int copies in new[]{1,2})
        {
            var(_,a,b,es)=Setup();var combat=a.Creature.CombatState!;
            for(int i=0;i<copies;i++){var c=Card<CourtOfCinders>(a,upgraded);Require(c.EnergyCost.GetWithModifiers(CostModifiers.None)==1,"Court cost");Resolve(c);}
            var power=a.Creature.GetPower<CourtOfCindersPower>()!;int amount=(upgraded?8:5)*copies;
            CardCmd.Exhaust(Context,Card<DefendIronclad>(b),skipVisuals:true).GetAwaiter().GetResult();
            Require(es.All(e=>e.GetPowerAmount<PoisonPower>()==0),"Partner Exhaust triggered Court");
            CardCmd.Exhaust(Context,Card<DefendIronclad>(a),skipVisuals:true).GetAwaiter().GetResult();
            CardCmd.Exhaust(Context,Card<DefendIronclad>(a),skipVisuals:true).GetAwaiter().GetResult();
            Require(es.All(e=>e.GetPowerAmount<PoisonPower>()==amount)&&a.PlayerCombatState!.Energy==10,"Court cap/stacking or obsolete energy payout");
            NativeConsoleFixture.Set(combat,nameof(CombatState.CurrentSide),CombatSide.Enemy);
            power.BeforeSideTurnStart(Context,CombatSide.Enemy,es,combat).GetAwaiter().GetResult();
            CardCmd.Exhaust(Context,Card<DefendIronclad>(a),causedByEthereal:true,skipVisuals:true).GetAwaiter().GetResult();
            Require(es.All(e=>e.GetPowerAmount<PoisonPower>()==2*amount),"Court opponent-side reset/Ethereal exclusion remains");
        }
    }
    private static void Shadow()
    {
        foreach(bool upgraded in new[]{false,true})
        {
            var(_,a,b,_)=Setup();var c=Card<SecondShadow>(a,upgraded);
            Require(c.EnergyCost.GetWithModifiers(CostModifiers.None)==2,"Shadow upgrade still lowers cost");Resolve(c);
            var power=a.Creature.GetPower<AfterimagePower>()!;Require(power.Amount==(upgraded?2:1),"Shadow Afterimage amount");
            var own=Play(Card<DefendIronclad>(a));var foreign=Play(Card<DefendIronclad>(b));
            foreach(var p in new[]{foreign,own}){power.BeforeCardPlayed(p).GetAwaiter().GetResult();power.AfterCardPlayed(Context,p).GetAwaiter().GetResult();}
            Require(a.Creature.Block==(upgraded?2:1)&&b.Creature.Block==0,"Shadow native per-play owner block");
        }
    }
    private static void Alchemy()
    {
        foreach(bool upgraded in new[]{false,true})
        {
            var(_,a,_,es)=Setup();Power<PoisonPower>(es[0],4);var c=Card<RoyalAlchemyPrize>(a,upgraded);
            Require(c.EnergyCost.GetWithModifiers(CostModifiers.None)==(upgraded?1:2),"Playable Sewer Alchemy cost");Resolve(c,es[0]);
            Require(es[0].GetPowerAmount<PoisonPower>()==(upgraded?16:13)&&es.Skip(1).All(e=>e.GetPowerAmount<PoisonPower>()==0),"Playable Alchemy changed previous poison upgrade or target");
            Require(LaRataCardCatalog.Entries.Single(e=>e.Model is SewerAlchemy).BossEligible==false,"Retired boss Alchemy returned");
        }
    }

    private static void CardIdentityRoundtrip()
    {
        foreach(var (canonical,cost) in new (CardModel,int)[]{
            (ModelDb.Card<SerKevsThornBuster>(),1),(ModelDb.Card<GraveyardShift>(),1),
            (ModelDb.Card<CourtOfCinders>(),1),(ModelDb.Card<SecondShadow>(),2),(ModelDb.Card<RoyalAlchemyPrize>(),1),(ModelDb.Card<LordTibosReset>(),2)})
        {
            var card=canonical.ToMutable();card.UpgradeInternal();card.FinalizeUpgradeInternal();
            var restored=CardModel.FromSerializable(JsonSerializer.Deserialize(
                JsonSerializer.Serialize(card.ToSerializable(),JsonSerializationUtility.GetTypeInfo<SerializableCard>()),
                JsonSerializationUtility.GetTypeInfo<SerializableCard>())!);
            Require(restored.Id==canonical.Id&&restored.CurrentUpgradeLevel==1,"Changed card save identity/upgrade");
            Require(restored.EnergyCost.GetWithModifiers(CostModifiers.None)==cost,"Changed upgrade reconstruction cost");
            Require(restored.Keywords.SequenceEqual(card.Keywords)&&restored.DynamicVars.All(v=>v.Value.BaseValue==card.DynamicVars[v.Key].BaseValue),"Changed upgrade keywords/values roundtrip");
        }
        Console.WriteLine("Native SerializableCard JSON roundtrip only; not a disk save/restart or compatibility proof for every historical save.");
    }

    private static void Reset()
    {
        foreach(bool upgraded in new[]{false,true})foreach(bool otherCasts in new[]{false,true})
        {
            var(_,a,b,_)=Setup();var combat=a.Creature.CombatState!;
            a.PlayerCombatState!.Energy=b.PlayerCombatState!.Energy=20;
            Card<DefendIronclad>(a).SpendResources().GetAwaiter().GetResult();
            Card<Bash>(b).SpendResources().GetAwaiter().GetResult();
            Card<DefendIronclad>(b).SpendResources().GetAwaiter().GetResult();
            // Reset is created only after all earlier payments, deliberately.
            var caster=otherCasts?b:a;var first=Card<LordTibosReset>(caster,upgraded);
            first.SpendResources().GetAwaiter().GetResult();Resolve(first);
            Require(a.PlayerCombatState.Energy==(otherCasts?20:18)&&b.PlayerCombatState.Energy==(otherCasts?18:20),"Reset recipient/payment/self-cost boundary");
            Require(ResetEnergyLedger.Pending(combat).Values.All(v=>v==0),"Reset actual credit was not consumed");
            int beforeA=a.PlayerCombatState.Energy,beforeB=b.PlayerCombatState.Energy;
            Resolve(Card<LordTibosReset>(caster,!upgraded));
            Require(a.PlayerCombatState.Energy==beforeA&&b.PlayerCombatState.Energy==beforeB,"Reset resolution replay/copy duplicates a payment");
            Card<Bash>(a).SpendResources().GetAwaiter().GetResult();
            var next=Card<LordTibosReset>(b,true);next.SpendResources().GetAwaiter().GetResult();Resolve(next);
            Require(a.PlayerCombatState.Energy==beforeA&&b.PlayerCombatState.Energy==beforeB-2,"Reset new eligible payment or other copy's cost");
            Card<DefendIronclad>(a).SpendResources().GetAwaiter().GetResult();
            Hook.BeforeSideTurnStart(combat,CombatSide.Enemy,combat.Enemies).GetAwaiter().GetResult();
            Require(ResetEnergyLedger.Pending(combat).Count==0,"Reset payment survives native turn boundary");
        }
        {
            var(_,a,b,_)=Setup();var combat=a.Creature.CombatState!;
            var free=Card<Bash>(a);free.EnergyCost.SetUntilPlayed(0);free.SpendResources().GetAwaiter().GetResult();
            a.PlayerCombatState!.Stars=5;Card<Stardust>(a).SpendResources().GetAwaiter().GetResult();
            Require(a.PlayerCombatState.Stars==0,"Native star-X setup did not pay");
            Require(ResetEnergyLedger.Pending(combat).Count==0,"Free card created refundable energy");
            a.PlayerCombatState!.Energy=4;Card<Whirlwind>(a).SpendResources().GetAwaiter().GetResult();
            Require(ResetEnergyLedger.Pending(combat).GetValueOrDefault(a.NetId)==4,"X card did not record actual energy debit");
            a.PlayerCombatState.Energy=999999998;
            ResetEnergyLedger.Refund(combat).GetAwaiter().GetResult();
            Require(a.PlayerCombatState.Energy==999999999&&ResetEnergyLedger.Pending(combat).GetValueOrDefault(a.NetId)==3,"Native energy cap falsely consumed unpaid refund");
            a.PlayerCombatState.Energy=20;ResetEnergyLedger.Refund(combat).GetAwaiter().GetResult();
            Require(a.PlayerCombatState.Energy==23&&b.PlayerCombatState!.Energy==10&&ResetEnergyLedger.Pending(combat).Values.All(v=>v==0),"Reset cap remainder/owner isolation");
        }
        {
            var(_,a,_,_)=Setup();var combat=a.Creature.CombatState!;
            a.PlayerCombatState!.Energy=1;Card<Bash>(a).SpendResources().GetAwaiter().GetResult();
            Require(ResetEnergyLedger.Pending(combat).GetValueOrDefault(a.NetId)==1,"Reset recorded requested cost rather than clamped actual debit");
        }
        foreach(bool throwAfterWrite in new[]{false,true})
        {
            var(_,a,b,_)=Setup();var combat=a.Creature.CombatState!;
            Card<DefendIronclad>(a).SpendResources().GetAwaiter().GetResult();
            Card<Bash>(b).SpendResources().GetAwaiter().GetResult();
            bool entered=false;
            Action<int,int> callback=(before,after)=>
            {
                if(entered||after<=before)return;entered=true;
                a.PlayerCombatState!.GainEnergy(3);
                PlayerCmd.GainEnergy(4,a).GetAwaiter().GetResult();
                ResetEnergyLedger.Refund(combat).GetAwaiter().GetResult();
                if(throwAfterWrite)throw new InvalidOperationException("deliberate fixture exception after actual credit");
            };
            a.PlayerCombatState!.EnergyChanged+=callback;
            bool threw=false;
            try{ResetEnergyLedger.Refund(combat).GetAwaiter().GetResult();}
            catch(InvalidOperationException e)when(e.Message=="deliberate fixture exception after actual credit"){threw=true;}
            finally{a.PlayerCombatState.EnergyChanged-=callback;}
            Require(entered&&threw==throwAfterWrite&&a.PlayerCombatState.Energy==17,"Nested direct/native gains or post-write exception behavior");
            Require(ResetEnergyLedger.Pending(combat).GetValueOrDefault(a.NetId)==0&&ResetEnergyLedger.Pending(combat).GetValueOrDefault(b.NetId)==(throwAfterWrite?2:0),"Post-write exception made actual credit refundable twice or lost unissued peer credit");
            ResetEnergyLedger.Refund(combat).GetAwaiter().GetResult();
            Require(a.PlayerCombatState.Energy==17&&b.PlayerCombatState!.Energy==10,"Retry after callback/exception duplicated or lost actual credit");
        }
        {
            var(_,a,_,_)=Setup();var combat=a.Creature.CombatState!;
            Card<Bash>(a).SpendResources().GetAwaiter().GetResult();Power<NoEnergyGainPower>(a.Creature,1);
            ResetEnergyLedger.Refund(combat).GetAwaiter().GetResult();
            Require(a.PlayerCombatState!.Energy==8&&ResetEnergyLedger.Pending(combat).GetValueOrDefault(a.NetId)==2,"Native blocked energy gain consumed refund");
            a.Creature.GetPower<NoEnergyGainPower>()!.RemoveInternal();ResetEnergyLedger.Refund(combat).GetAwaiter().GetResult();
            Require(a.PlayerCombatState.Energy==10&&ResetEnergyLedger.Pending(combat).GetValueOrDefault(a.NetId)==0,"Unblocked native gain did not consume refund");
        }
    }

    private static void ResetAwaitBoundaries(Harmony harmony)
    {
        var method=AccessTools.Method(typeof(Hook),nameof(Hook.AfterModifyingEnergyGain));
        var prefix=AccessTools.Method(typeof(FollowupCardChecks),nameof(PauseFirstRefund));
        harmony.Patch(method,prefix:new HarmonyMethod(prefix));
        try
        {
            foreach(bool turnChanges in new[]{false,true})foreach(bool newPaymentWhilePaused in new[]{false,true})
            {
                var(_,a,b,_)=Setup();var combat=a.Creature.CombatState!;
                Card<DefendIronclad>(a).SpendResources().GetAwaiter().GetResult();
                Card<Bash>(b).SpendResources().GetAwaiter().GetResult();
                var gate=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);refundGate=gate;
                var first=ResetEnergyLedger.Refund(combat);
                Require(!first.IsCompleted,"Refund did not pause at supplied native modifier await");
                if(newPaymentWhilePaused)Card<DefendIronclad>(a).SpendResources().GetAwaiter().GetResult();
                var second=ResetEnergyLedger.Refund(combat);second.GetAwaiter().GetResult();
                Require(a.PlayerCombatState!.Energy==9&&b.PlayerCombatState!.Energy==8,"Concurrent Reset duplicated reserved payments");
                if(turnChanges)Hook.BeforeSideTurnStart(combat,CombatSide.Enemy,combat.Enemies).GetAwaiter().GetResult();
                gate.SetResult();first.GetAwaiter().GetResult();
                Require(a.PlayerCombatState.Energy==(turnChanges?9:10)&&b.PlayerCombatState!.Energy==(turnChanges?8:10),"Refund crossed turn generation or lost reserved own-player grant");
                Require(ResetEnergyLedger.Pending(combat).Values.All(v=>v==0),"Completed refund retained duplicate eligible payment");
            }
        }
        finally{refundGate=null;harmony.Unpatch(method,prefix);}
        Console.WriteLine("Refund concurrency uses a supplied native modifier-await gate, not real simultaneous client actions.");
    }
}
