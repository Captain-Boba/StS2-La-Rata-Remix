using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

internal static class RoyalFollowupChecks
{
    private static void Require(bool ok,string why){if(!ok)throw new Exception(why);}
    private static void Power<T>(Creature c,int n) where T:PowerModel,new(){if(n>0)ModelDb.Power<T>().ToMutable().ApplyInternal(c,n,true);}
    private static (RunState run,CombatState combat,RatKing king,Player[] players) Setup(int count)
    {
        var players=Enumerable.Range(0,count).Select(i=>NativeConsoleFixture.Player((ulong)(901+i))).ToArray();
        var run=RunState.CreateForTest(players:players,seed:"ROYALDELTA");
        run.CurrentActIndex=2; // Native final-act boss scaling, also used by the bonus act.
        NativeConsoleFixture.Set(RunManager.Instance,"State",run);
        var combat=new CombatState(ModelDb.Encounter<RatKingEncounter>().ToMutable(),run,multiplayerScalingModel:run.MultiplayerScalingModel);
        run.MultiplayerScalingModel.OnCombatEntered(combat);
        AccessTools.Field(typeof(CombatManager),"_state").SetValue(CombatManager.Instance,combat);
        foreach(var p in players)
        {
            NativeConsoleFixture.Set(p,nameof(p.Creature),new Creature(p,100,100){CombatState=combat});
            p.ResetCombatState();combat.AddCreature(p.Creature);
        }
        var king=(RatKing)ModelDb.Monster<RatKing>().ToMutable();
        combat.AddCreature(new Creature(king,CombatSide.Enemy,"king"){CombatState=combat});
        AccessTools.Field(typeof(RatKing),"deck").SetValue(king,new RoyalDeck(_=>0));
        return(run,combat,king,players);
    }
    private static void Target(RatKing king,int index,Player p)=>((Dictionary<int,ulong>)AccessTools.Field(typeof(RatKing),"targetIds").GetValue(king)!)[index]=p.NetId;
    private static void Resolve(RatKing king,int index,Player[] players)=>RoyalBossEffects.Resolve(king,index,players.Select(p=>p.Creature).ToArray()).GetAwaiter().GetResult();
    internal static void Run()
    {
        using var localization=new NativeFixtureLocalization();
        RoyalBossEffects.ValidateCatalog();
        Require(RoyalDeck.ResetCard == 47 && !RoyalDeck.ActiveCards.Contains(47) &&
            !LaRataCardCatalog.GetBossEntry(47).BossEligible && LaRataCardCatalog.GetBossModel(47) is LordTibosReset &&
            LaRataCardCatalog.Neutrals.Any(card => card is LordTibosReset),
            "Reset must retain ID 47 and its player booster model while excluded from the boss deck");
        Require(!RoyalDeck.ActiveCards.Contains(25) && !LaRataCardCatalog.GetBossEntry(25).BossEligible &&
            LaRataCardCatalog.Neutrals.Any(card=>card is LaRata.Cleanse),
            "Royal Cleansing must remain a player booster card while excluded from the boss deck");
        Require(RoyalBossEffects.Description(8)=="Gain 25 base Block and 2 Vulnerable." &&
            RoyalBossEffects.EnglishDescription(8)==RoyalBossEffects.Description(8), "Exposed Crown boss preview/fallback differs");
        Require(RoyalDeck.Costs[36]==1&&RoyalDeck.Costs[41]==1&&RoyalDeck.Costs[46]==1,"Royal cost delta missing");
        Require(RoyalDeck.IsExhaust(36)&&RoyalDeck.IsExhaust(41)&&!RoyalDeck.ActiveCards.Contains(3),"Royal Exhaust or retired Alchemy changed");
        foreach(int count in new[]{1,2,3,4})
        {
            {
                var(_,_,king,players)=Setup(count);
                Resolve(king,8,players);
                int expectedBlock=count switch{1=>25,2=>65,3=>97,4=>130,_=>throw new InvalidOperationException()};
                Require(king.Creature.Block==expectedBlock && king.Creature.GetPowerAmount<VulnerablePower>()==2,
                    $"Exposed Crown native boss Block/Vulnerable count={count}: expected {expectedBlock}/2, got {king.Creature.Block}/{king.Creature.GetPowerAmount<VulnerablePower>()}");
                Require(RoyalBossEffects.Intents(king,8).Any(i=>i is MegaCrit.Sts2.Core.MonsterMoves.Intents.DefendIntent),
                    "Exposed Crown lost its native defensive intent");
            }
            foreach(int artifact in new[]{0,1,2,3})
            {
                var(_,_,king,players)=Setup(count);
                for(int i=0;i<count;i++){Power<ThornsPower>(players[i].Creature,i%2==0?3:0);Power<ArtifactPower>(players[i].Creature,artifact);}
                string deck=king.Deck!.Snapshot;Resolve(king,36,players);
                for(int i=0;i<count;i++)
                {
                    var c=players[i].Creature;bool stripped=i%2==0;
                    Require(c.GetPowerAmount<ThornsPower>()==0&&c.GetPowerAmount<VulnerablePower>()==(stripped&&artifact==0?3:0),"Royal Thorn recipient/Artifact");
                    Require(c.GetPowerAmount<ArtifactPower>()==Math.Max(0,artifact-(stripped?1:0)),"Royal Thorn consumes Artifact on wrong player");
                }
                Require(king.Deck.Snapshot==deck,"Royal Thorn still reserves future draw");
            }
            foreach(string state in new[]{"present","lost","dead"})
            {
                var(_,_,king,players)=Setup(count);Target(king,41,players[0]);
                foreach(var p in players)Power<VulnerablePower>(p.Creature,3);
                if(state=="lost")players[0].Creature.GetPower<VulnerablePower>()!.RemoveInternal();
                if(state=="dead")players[0].Creature.SetCurrentHpInternal(0);
                string deck=king.Deck!.Snapshot;int hp=king.Creature.CurrentHp;
                Resolve(king,41,players);
                // Native final-act boss Block uses count*1.3 for multiple players;
                // it differs from the separate manual Plating amount convention.
                int expectedBlock=state=="present"?(int)(9*(count==1?1m:count*1.3m)):0;
                Require(king.Creature.Block==expectedBlock,$"Royal Graveyard count={count} state={state}: expected {expectedBlock}, got {king.Creature.Block}");
                Require(players.Skip(1).All(p=>p.Creature.GetPowerAmount<VulnerablePower>()==3),"Royal Graveyard retargeted another player");
                Require(king.Creature.CurrentHp==hp&&king.Deck.Snapshot==deck,"Royal Graveyard old HP/draw effect remains");
            }
            {
                var(_,combat,king,players)=Setup(count);Resolve(king,46,players);
                var power=king.Creature.GetPower<RoyalCourtOfCindersPower>()!;string before=king.Deck!.Snapshot;
                power.AfterRoyalExhaust(king).GetAwaiter().GetResult();power.AfterRoyalExhaust(king).GetAwaiter().GetResult();
                Require(players.All(p=>p.Creature.GetPowerAmount<PoisonPower>()==5)&&king.Deck.Snapshot==before,"Royal Court cap or old future Energy");
                power.AfterSideTurnStart(CombatSide.Enemy,[king.Creature],combat).GetAwaiter().GetResult();
                power.AfterRoyalExhaust(king).GetAwaiter().GetResult();
                Require(players.All(p=>p.Creature.GetPowerAmount<PoisonPower>()==10),"Royal Court turn reset");
            }
        }
        int forecasts=0;
        foreach(int[] plan in new[]{new[]{36,41,1},new[]{41,35,1},new[]{36,1},new[]{41,1}})
        foreach(int artifact in new[]{0,1,2,3})foreach(bool vulnerable in new[]{false,true})foreach(bool thorns in new[]{false,true})
        {
            var(run,combat,king,players)=Setup(2);var target=players[0].Creature;
            Target(king,41,players[0]);NativeConsoleFixture.Set(king.Deck!,nameof(RoyalDeck.Planned),plan);
            Power<VulnerablePower>(target,vulnerable?3:0);Power<ThornsPower>(target,thorns?2:0);Power<ArtifactPower>(target,artifact);
            string before=king.Deck!.Snapshot;
            int forecast=RoyalForecastAttackIntent.Forecast(king,king.Creature,target,12,1,false);
            int guardForecast=RoyalForecastAttackIntent.Forecast(king,king.Creature,target,12,-1,true);
            Require(king.Deck.Snapshot==before&&target.GetPowerAmount<ArtifactPower>()==artifact&&target.GetPowerAmount<VulnerablePower>()==(vulnerable?3:0),"Forecast mutates live state");
            foreach(int index in plan.TakeWhile(i=>!RoyalDeck.IsAttack(i)))Resolve(king,index,players);
            int actual=(int)Hook.ModifyDamage(run,combat,target,king.Creature,12,ValueProp.Move,null,ModifyDamageHookType.All,CardPreviewMode.None,out _);
            Require(forecast==actual&&guardForecast==actual,$"Royal forecast vs native resolved powers differs plan={string.Join(',',plan)} artifact={artifact} vuln={vulnerable} thorns={thorns}: {forecast}/{guardForecast}!={actual}");
            forecasts++;
        }
        foreach(var (maxHp,expected) in new[]{(50,1),(199,1),(1500,15),(3900,39),(5850,58),(7800,78)})
        {
            var(_,_,king,players)=Setup(1);
            AccessTools.PropertySetter(typeof(Creature),nameof(Creature.MaxHp)).Invoke(king.Creature,[maxHp]);
            king.Creature.SetCurrentHpInternal(maxHp-20);
            decimal amount=RoyalBossEffects.GraveLevyHealing(king);
            Require(amount==expected,"Final Grave Levy percentage/rounding/quartering");
            ModelDb.Card<GraveLevy>().Resolve(king,players.Select(p=>p.Creature).ToArray()).GetAwaiter().GetResult();
            Require(king.Creature.CurrentHp==Math.Min(maxHp,maxHp-20+expected),"Native Grave Levy heal/cap");
            // Existing 0.9.5 boss ramp applies even when Resolve is invoked
            // directly: the initial round deals floor(18 * 40%) = 7 damage.
            Require(players[0].Creature.CurrentHp==93,"Grave Levy initial-round attack did not apply the existing 40% boss ramp");
        }
        Require(ModelDb.Card<RoyalForeclosurePrize>().DynamicVars["Heal"].BaseValue==6,"Unrequested playable healing changed");
        {
            var(_,combat,king,players)=Setup(1);
            Power<DexterityPower>(king.Creature,3);
            Resolve(king,8,players);
            Require(king.Creature.Block==28,"Exposed Crown must apply native Dexterity after its 25 Block boss basis");
            var caster=ModelDb.Monster<CrownHeadsman>().ToMutable();
            combat.AddCreature(new Creature(caster,CombatSide.Enemy,"intervention"){CombatState=combat});
            ModelDb.Card<ExposedCrown>().Resolve(caster,players.Select(p=>p.Creature).ToArray()).GetAwaiter().GetResult();
            Require(caster.Creature.Block==16 && caster.Creature.GetPowerAmount<VulnerablePower>()==2,
                "Exposed Crown changed the normal intervention caster");
            Require(RoyalCard.Descriptions[8].Contains("16 base Block"),"Normal intervention description changed");
        }
        Console.WriteLine("PASS Exposed Crown: native Block/Vulnerable for 1/2/3/4 modeled owners (25/65/97/130 Block), native Dexterity after the boss basis, 16 Block intervention, boss preview/fallback and defensive intent. No rendered UI/live multiplayer claim.");
        Console.WriteLine($"PASS royal delta: native effect commands for 1/2/3/4 modeled owners, {forecasts} supplied-plan forecasts compared to resolved native power hooks, final 1% healing and caps. Target announcements supplied; no live network, rendered intents or full boss-hand lifecycle.");
    }
}
