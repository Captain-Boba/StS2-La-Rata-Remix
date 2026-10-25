using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

internal static class NightTwistChecks
{
    private static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }
    private static bool Active(ref bool __result) { __result=false; return false; }
    private static void Apply(CombatState combat) =>
        ((Task)AccessTools.Method(typeof(StartingBuffsPatch), "Apply").Invoke(null,[Task.CompletedTask,combat])!).GetAwaiter().GetResult();
    private static (RunState run, CombatState combat, Creature[] enemies, EncounterRules? rules) Setup(int count, bool markers, bool scout)
    {
        var players=Enumerable.Range(0,count).Select(i=>NativeConsoleFixture.Player((ulong)(601+i))).ToArray();
        var run=RunState.CreateForTest(players:players,seed:"NIGHTTWISTS");
        var encounter=ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable();
        var combat=new CombatState(encounter,run);
        foreach(var p in players)
        {
            var c=new Creature(p,50,50){CombatState=combat};
            NativeConsoleFixture.Set(p,nameof(p.Creature),c);p.ResetCombatState();combat.AddCreature(c);
        }
        MonsterModel[] monsters=[ModelDb.Monster<TwoTailedRat>().ToMutable(),ModelDb.Monster<LeafSlimeS>().ToMutable(),ModelDb.Monster<Inklet>().ToMutable()];
        EncounterRules? rules=null;
        if(markers)
        {
            var plan=new EncounterPlan(0,0,2,[new(TwistKind.Sturdy,0,18),new(TwistKind.Keen,0,12),new(TwistKind.Empowered,0,1)]){ScoutReinforcement=scout};
            rules=new EncounterRules(plan){Roster=monsters};rules.Reinforcements.AddRange(monsters.Skip(1));
            EncounterDirector.Rules.Add(encounter,rules);
        }
        var enemies=monsters.Select((m,i)=>combat.CreateCreature(m,CombatSide.Enemy,i.ToString())).ToArray();
        foreach(var c in enemies)combat.AddCreature(c);
        return(run,combat,enemies,rules);
    }
    internal static void Run()
    {
        var harmony=new Harmony("LaRata.NightTwistChecks");
        NativeConsoleFixture.PatchDesktop(harmony);
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager),nameof(CombatManager.IsOverOrEnding)),prefix:new(typeof(NightTwistChecks),nameof(Active)));
        harmony.CreateClassProcessor(typeof(CreatureHpPatch)).Patch();
        harmony.CreateClassProcessor(typeof(DamagePatch)).Patch();
        harmony.CreateClassProcessor(typeof(PowerPotencyPatch)).Patch();
        try
        {
            foreach(int players in new[]{1,2,4})
            foreach(bool scout in new[]{false,true})
            {
                var baseline=Setup(players,false,scout);var candidate=Setup(players,true,scout);
                int[] initial=candidate.enemies.Select(c=>c.MaxHp).ToArray();
                Require(initial[0]==decimal.Ceiling(baseline.enemies[0].MaxHp*1.18m),"Sturdy actual HP differs");
                foreach(int i in new[]{1,2})Require(initial[i]==Math.Max(1,decimal.Ceiling(baseline.enemies[i].MaxHp*(scout?.35m:.60m))),"Reinforcement actual HP differs");
                Apply(candidate.combat);Apply(candidate.combat);
                var main=candidate.enemies[0];
                Require(main.GetPowerAmount<SturdyTwistPower>()==18&&main.GetPowerAmount<KeenTwistPower>()==12&&main.GetPowerAmount<EmpoweredTwistPower>()==1,"Missing/wrong stat markers");
                Require(main.GetPowerAmount<VigorPower>()==2,"Empowered Vigor duplicated");
                Require(candidate.enemies.Select(c=>c.MaxHp).SequenceEqual(initial),"Marker application mutates HP again");
                foreach(var c in candidate.enemies.Skip(1))
                {
                    Require(c.GetPowerAmount<ReinforcementTwistPower>()==(scout?35:60),"Wrong reinforcement marker");
                    Require(!c.IsSecondaryEnemy,"Reused Minion icon changed fatal behavior");
                }
                main.GetPower<VigorPower>()!.RemoveInternal();
                decimal Damage(int i,CardPreviewMode preview)=>Hook.ModifyDamage(candidate.run,candidate.combat,candidate.run.Players[0].Creature,candidate.enemies[i],10,ValueProp.Move,null,ModifyDamageHookType.Multiplicative,preview,out _);
                for(int i=0;i<3;i++)
                {
                    decimal expected=i==0?11.2m:i==2?10m:scout?5m:6.5m;
                    Require(Damage(i,CardPreviewMode.None)==expected&&Damage(i,CardPreviewMode.Normal)==expected,"Actual/preview damage multiplier changed");
                }
                // Reconstructing a status instance from canonical amount must remain passive.
                main.GetPower<SturdyTwistPower>()!.RemoveInternal();
                ModelDb.Power<SturdyTwistPower>().ToMutable().ApplyInternal(main,18,true);
                Require(main.MaxHp==initial[0]&&main.GetPowerAmount<SturdyTwistPower>()==18,"Marker reconstruction reapplies HP");
                Require(main.Powers.OfType<TwistStatusPower>().All(p=>!p.ShouldScaleInMultiplayer&&!p.OwnerIsSecondaryEnemy),"Passive marker changes party/fatal behavior");
            }
            Console.WriteLine("PASS: native CreateCreature HP scaling and PowerCmd marker application at 1/2/4 modeled players, 35/60% reinforcements, 18% Sturdy, 12% Keen, Empowered+Vigor, startup idempotence, passive marker reconstruction, native damage multipliers and Inklet exception. Desktop/combat guard substituted; no disk save, rendering or network evidence.");
        }
        finally{harmony.UnpatchAll(harmony.Id);}
    }
}
