using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Runs;

// Existing native-model runner, not a renderer or transport. Controlled native
// MoveStates have no base attack; the real HexExecution and PowerCmd still run.
internal static class HexPresentationChecks
{
    private static bool ending;
    private static bool Ending(ref bool __result) { __result = ending; return false; }
    private static void Require(bool value, string why) { if (!value) throw new Exception(why); }

    internal static (CombatState Combat, Creature Enemy, Creature[] Targets) Setup(EnemyTwist twist, int players)
    {
        var owners = Enumerable.Range(0, players).Select(i => NativeConsoleFixture.Player((ulong)(801 + i))).ToArray();
        var run = RunState.CreateForTest(players: owners, seed: "HEXPRESENTATION");
        var encounter = ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable();
        var combat = new CombatState(encounter, run);
        foreach (var owner in owners)
        {
            var body = new Creature(owner, 50, 50) { CombatState = combat };
            NativeConsoleFixture.Set(owner, nameof(owner.Creature), body);
            owner.ResetCombatState(); combat.AddCreature(body);
        }
        var monster = ModelDb.Monster<TwoTailedRat>().ToMutable();
        EncounterDirector.Rules.Add(encounter, new EncounterRules(new(0, 0, 0, [twist])) { Roster = [monster] });
        var enemy = combat.CreateCreature(monster, CombatSide.Enemy, "hex");
        combat.AddCreature(enemy);
        return (combat, enemy, owners.Select(p => p.Creature).ToArray());
    }

    private static void Start(CombatState combat) =>
        ((Task)AccessTools.Method(typeof(StartingBuffsPatch), "Apply").Invoke(null, [Task.CompletedTask, combat])!).GetAwaiter().GetResult();

    private static PowerModel? Debuff(Creature target, int variant) => variant switch
    {
        2 => target.GetPower<FrailPower>(), 3 => target.GetPower<VulnerablePower>(), _ => target.GetPower<WeakPower>()
    };

    internal static void Run(Harmony harmony)
    {
        ending = false;
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsEnding)), prefix: new(typeof(HexPresentationChecks), nameof(Ending)));
        harmony.CreateClassProcessor(typeof(HexTelegraphPatch)).Patch();
        harmony.CreateClassProcessor(typeof(HexExecutionPatch)).Patch();
        try
        {
            var kinds = EncounterPlan.ActiveKinds.Where(EncounterPlan.IsHex).ToArray();
            Require(kinds.Length == 14 && !kinds.Contains(TwistKind.Doomweaver), "Active hex roster changed");
            foreach (var kind in kinds)
            {
                var state = Setup(new(kind, 0, 1), 1);
                state.Enemy.Monster!.SetUpForCombat();
                var moves = state.Enemy.Monster.MoveStateMachine!.States.Values.OfType<MoveState>().ToArray();
                var marked = moves.Where(m => HexTelegraphPatch.Hexes.TryGetValue(m, out _)).ToArray();
                Require(marked.Length == 1 && HexTelegraphPatch.HasHexMove(state.Enemy.Monster), "Missing/multiple decorated moves: " + kind);
                bool status = kind is TwistKind.Cinders or TwistKind.Disorienting or TwistKind.Lacerating or TwistKind.Leaden or TwistKind.StaticHex;
                Require(marked[0].Intents.Count(i => status ? i is HexStatusIntent : i is HexDebuffIntent) == 1, "Missing concrete native intent: " + kind);
                Require(moves.Where(m => m != marked[0]).All(m => !m.Intents.Any(i => i is HexStatusIntent or HexDebuffIntent)), "Hex description attached to unrelated move: " + kind);
                int hp = state.Enemy.MaxHp;
                Start(state.Combat); Start(state.Combat);
                Require(state.Enemy.Powers.OfType<HexTwistPower>().Count() == 1 && state.Enemy.GetPowerAmount<HexTwistPower>() == (int)kind + 1 && state.Enemy.MaxHp == hp, "Startup duplicated marker or changed HP: " + kind);
            }
            foreach (int players in new[] { 1, 2, 4 })
            foreach (var kind in new[] { TwistKind.Hexed, TwistKind.Echoing })
            foreach (int variant in new[] { 1, 2, 3 })
            foreach (bool lesser in new[] { false, true })
            {
                var twist = new EnemyTwist(kind, 0, variant, Lesser: lesser);
                var state = Setup(twist, players);
                Start(state.Combat);
                var move = new MoveState("HEX_FIXTURE", _ => Task.CompletedTask, new BuffIntent());
                HexTelegraphPatch.Hexes.Add(move, new(state.Enemy.Monster!, twist));
                var unrelated = new MoveState("UNRELATED", _ => Task.CompletedTask, new BuffIntent());
                var target = state.Targets[0];
                Creature[] targets = [target, state.Enemy];
                int first = kind == TwistKind.Echoing || !lesser ? 2 : 1;
                int second = kind == TwistKind.Echoing ? 2 : lesser ? 2 : 3;
                for (int phase = 0; phase < 3; phase++)
                {
                    Debuff(target, variant)?.RemoveInternal();
                    if (phase == 1) state.Enemy.GetPower<HexTwistPower>()!.RemoveInternal();
                    if (phase == 2) ModelDb.Power<HexTwistPower>().ToMutable().ApplyInternal(state.Enemy, (int)kind + 1, true);
                    unrelated.PerformMove(targets).GetAwaiter().GetResult();
                    Require(Debuff(target, variant) == null, "Unrelated move executed hex");
                    move.PerformMove(targets).GetAwaiter().GetResult();
                    Require(Debuff(target, variant)?.Amount == first, $"First dose changed with marker phase {phase}: {kind}/{variant}/{lesser}/{players}");
                    move.PerformMove(targets).GetAwaiter().GetResult();
                    Require(Debuff(target, variant)?.Amount == second, "Repeated hex/cap changed");
                    Require(Debuff(state.Enemy, variant) == null && state.Targets.Skip(1).All(t => Debuff(t, variant) == null), "Hex leaked to foreign targets");
                }
            }
            CheckCompletionAndArtifact();
            Console.WriteLine("PASS: 14 native hex decorations/startup markers; native MoveState + HexExecution/PowerCmd with present/removed/reconstructed markers, Hexed/Echoing variants/Lesser at 1/2/4 modeled players, repeated nonattack moves, target isolation, Artifact and completion guards. Controlled base move/no damage; no attack, renderer, disk or network acceptance.");
        }
        finally { ending = false; }
    }

    private static void CheckCompletionAndArtifact()
    {
        var twist = new EnemyTwist(TwistKind.Hexed, 0, 1);
        var state = Setup(twist, 1); Start(state.Combat);
        var target = state.Targets[0];
        var completion = new TaskCompletionSource<bool>();
        var move = new MoveState("DELAYED_HEX", _ => completion.Task, new BuffIntent());
        HexTelegraphPatch.Hexes.Add(move, new(state.Enemy.Monster!, twist));
        var pending = move.PerformMove([target]);
        Require(!pending.IsCompleted && target.GetPower<WeakPower>() == null, "Hex ran before move completion");
        completion.SetResult(true); pending.GetAwaiter().GetResult();
        Require(target.GetPowerAmount<WeakPower>() == 2, "Completed move lost hex");
        target.GetPower<WeakPower>()!.RemoveInternal();
        ModelDb.Power<ArtifactPower>().ToMutable().ApplyInternal(target, 1, true);
        move.PerformMove([target]).GetAwaiter().GetResult();
        Require(target.GetPowerAmount<WeakPower>() == 0 && target.GetPowerAmount<ArtifactPower>() == 0, "Hex bypassed native Artifact");
        ending = true;
        move.PerformMove([target]).GetAwaiter().GetResult();
        Require(target.GetPower<WeakPower>() == null, "Hex executed while combat ending");
        ending = false;
        state.Enemy.SetCurrentHpInternal(0);
        move.PerformMove([target]).GetAwaiter().GetResult();
        Require(target.GetPower<WeakPower>() == null, "Dead source executed hex");
    }
}
