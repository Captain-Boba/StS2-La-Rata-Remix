using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

internal static class WitheringChecks
{
    private static readonly PlayerChoiceContext Context = new ThrowingPlayerChoiceContext();
    private static int drawnHands;
    private static Player? pausedOwner;
    private static TaskCompletionSource? drawGate;
    private static void PauseBeforeDraw(Player player, ref Task __result)
    {
        if (player == pausedOwner) __result = Wait(__result);
        static async Task Wait(Task original) { await original; await drawGate!.Task; }
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Await(Task task) => task.GetAwaiter().GetResult();

    private static (CombatState Combat, Creature Enemy, Creature[] Targets, MoveState Move) Setup(int players, int act)
    {
        var twist = new EnemyTwist(TwistKind.Withering, 0, 1, act);
        var (combat, enemy, targets) = EncounterVarietyChecks.Setup(ModelDb.Monster<TwoTailedRat>(), players, act, twist);
        foreach (var target in targets)
        {
            target.Player!.PlayerCombatState!.IncrementTurnNumber();
            for (int i = 0; i < 60; i++)
                PileType.Draw.GetPile(target.Player).AddInternal(combat.CreateCard<StrikeIronclad>(target.Player));
        }
        var move = new MoveState("WITHERING_CHECK", _ => Task.CompletedTask);
        HexTelegraphPatch.Hexes.Add(move, new(enemy.Monster!, twist));
        return (combat, enemy, targets, move);
    }

    private static void Apply(MoveState move, IEnumerable<Creature> targets) => Await(move.PerformMove(targets.ToArray()));
    private static void Begin(CombatState combat, Creature[] participants)
    {
        foreach (var owner in participants) owner.BeforeTurnStart(CombatSide.Player);
        Await(Hook.BeforeSideTurnStart(combat, CombatSide.Player, participants));
    }

    // Execute the real SetupPlayerTurn: energy, BeforeHandDraw, ModifyHandDraw,
    // AfterModifyingHandDraw, CardPileCmd.Draw and AfterPlayerTurnStart.
    // Only the surrounding combat loop is driven explicitly, in its native order.
    private static void Draw(CombatState combat, Creature owner, int expected)
    {
        var player = owner.Player!;
        foreach (var card in PileType.Hand.GetPile(player).Cards.ToArray())
        {
            card.RemoveFromCurrentPile();
            PileType.Discard.GetPile(player).AddInternal(card);
        }
        bool expires = owner.GetPower<WitheringPower>()?.AmountOnTurnStart > 0;
        bool hadWithering = owner.HasPower<WitheringPower>();
        var context = new HookPlayerChoiceContext(player, player.NetId, GameActionType.CombatPlayPhaseOnly);
        Await((Task)AccessTools.Method(typeof(CombatManager), "SetupPlayerTurn").Invoke(CombatManager.Instance, [player, context])!);
        Check(PileType.Hand.GetPile(player).Cards.Count == expected, $"Actual hand mismatch for {player.NetId}: expected {expected}, got {PileType.Hand.GetPile(player).Cards.Count}");
        Check(owner.HasPower<WitheringPower>() == (hadWithering && !expires), "Withering cleanup does not match actual owner draw");
        drawnHands++;
    }

    private static void EndSetup(CombatState combat, Creature[] participants) =>
        Await(Hook.AfterSideTurnStart(combat, CombatSide.Player, participants));
    private static void Turn(CombatState combat, Creature[] participants, Func<Creature, int> expected)
    {
        Begin(combat, participants);
        foreach (var owner in participants) Draw(combat, owner, expected(owner));
        EndSetup(combat, participants);
    }

    internal static void Run()
    {
        var previous = LocalContext.NetId;
        LocalContext.NetId = 1201;
        try { RunCases(); }
        finally { LocalContext.NetId = previous; }
    }

    private static void RunCases()
    {
        drawnHands = 0;
        for (int players = 1; players <= 4; players++)
        for (int act = 1; act <= 3; act++)
        {
            var (combat, enemy, targets, move) = Setup(players, act);
            Apply(move, targets); Apply(move, targets);
            Check(targets.All(t => t.GetPowerAmount<WitheringPower>() == 1 && !t.HasPower<MindRotPower>()), "Duplicate/party amplification or permanent MindRot application");
            foreach (var target in targets)
            {
                var power = target.GetPower<WitheringPower>()!;
                Check(power.AmountOnTurnStart == 0 && power.ModifyHandDraw(target.Player!, 5) == 5, "Same-turn application affects hand draw before native snapshot");
                Check(power.Description.GetFormattedText().Contains("next turn") && power.Title.GetFormattedText() == "Withering", "Player status localization missing");
                foreach (var other in targets.Where(t => t != target))
                    Check(power.ModifyHandDraw(other.Player!, 5) == 5, "Foreign owner draw changed");
            }
            // Enemy turn end/start must not consume an effect awaiting its owner.
            foreach (var target in targets)
                Await(target.GetPower<WitheringPower>()!.AfterSideTurnEnd(Context, CombatSide.Enemy, [enemy]));
            Await(Hook.AfterSideTurnStart(combat, CombatSide.Enemy, [enemy]));
            Check(targets.All(t => t.HasPower<WitheringPower>()), "Expired on enemy side");
            Turn(combat, targets, _ => 4);
            Check(targets.All(t => !t.HasPower<WitheringPower>()), "Missing cleanup after affected hand");
            Turn(combat, targets, _ => 5);
            Apply(move, [targets[0]]); Apply(move, [targets[0]]);
            Turn(combat, targets, t => t == targets[0] ? 4 : 5);
            Check(targets.All(t => !t.HasPower<WitheringPower>()), "Reapplication cleanup failed");
            Turn(combat, targets, _ => 5);
        }
        Console.WriteLine("PASS timing: 12 act/party scenarios, native SetupPlayerTurn draws 4 then 5; repeated application capped at -1; later reapplication affects only the next owner hand; no enemy-side or premature cleanup.");

        for (int players = 1; players <= 4; players++)
        {
            var (combat, enemy, targets, move) = Setup(players, 1);
            PowerCmd.Apply<ArtifactPower>(Context, targets[0], 1, null, null).GetAwaiter().GetResult();
            Apply(move, targets);
            Check(!targets[0].HasPower<WitheringPower>() && !targets[0].HasPower<ArtifactPower>(), "Artifact failed to block and consume");
            Turn(combat, targets, t => t == targets[0] ? 5 : 4);
            Apply(move, targets);
            Turn(combat, targets, _ => 4);
            foreach (var target in targets)
            {
                Await(PowerCmd.Apply<MindRotPower>(Context, target, 2, enemy, null));
                Await(PowerCmd.Apply<DrawCardsNextTurnPower>(Context, target, 2, target, null));
            }
            Apply(move, targets);
            Turn(combat, targets, _ => 4); // 5 - 2 permanent + 2 next-turn - 1 Withering
            Check(targets.All(t => t.GetPowerAmount<MindRotPower>() == 2 && !t.HasPower<DrawCardsNextTurnPower>() && !t.HasPower<WitheringPower>()), "Unrelated native draw powers changed");
            Turn(combat, targets, _ => 3);
        }
        Console.WriteLine("PASS boundaries: Artifact independently blocks its owner; native permanent MindRot and positive next-turn draw retain their amounts and lifetimes for 1-4 players.");

        {
            var (combat, _, targets, move) = Setup(4, 1);
            Begin(combat, targets); // Applied after the turn-start snapshot: reserve for next turn.
            Apply(move, targets);
            foreach (var target in targets) Draw(combat, target, 5);
            EndSetup(combat, targets);
            Check(targets.All(t => t.HasPower<WitheringPower>()), "Same-turn application consumed early");
            Turn(combat, [targets[0]], _ => 4);
            Check(!targets[0].HasPower<WitheringPower>() && targets.Skip(1).All(t => t.HasPower<WitheringPower>()), "Another owner's extra turn consumed pending effects");
            Turn(combat, targets.Skip(1).ToArray(), _ => 4);
            Check(targets.All(t => !t.HasPower<WitheringPower>()), "Separated owners did not clean up");
            Apply(move, targets);
            Await(CardPileCmd.Draw(Context, 1, targets[0].Player!));
            Check(PileType.Hand.GetPile(targets[0].Player!).Cards.Count == 5 && targets[0].HasPower<WitheringPower>(), "Ordinary card draw incorrectly consumed/reduced");
            foreach (var target in targets) Await(PowerCmd.Apply<MindRotPower>(Context, target, 5, null, null));
            Turn(combat, targets, _ => 0);
            Check(targets.All(t => !t.HasPower<WitheringPower>() && t.GetPowerAmount<MindRotPower>() == 5), "Zero-hand draw did not expire independently");
        }
        {
            var (combat, _, targets, move) = Setup(2, 1);
            Apply(move, targets); Begin(combat, targets);
            var harmony = new Harmony("LaRata.WitheringChecks.PausedDraw");
            pausedOwner = targets[0].Player!; drawGate = new();
            harmony.Patch(AccessTools.Method(typeof(Hook), nameof(Hook.BeforeHandDraw)), postfix: new(typeof(WitheringChecks), nameof(PauseBeforeDraw)));
            try
            {
                var context = new HookPlayerChoiceContext(pausedOwner, pausedOwner.NetId, GameActionType.CombatPlayPhaseOnly);
                var setup = (Task)AccessTools.Method(typeof(CombatManager), "SetupPlayerTurn").Invoke(CombatManager.Instance, [pausedOwner, context])!;
                Check(!setup.IsCompleted && PileType.Hand.GetPile(pausedOwner).Cards.Count == 0, "Fixture did not pause before drawing");
                Draw(combat, targets[1], 4);
                EndSetup(combat, targets);
                Check(targets[0].HasPower<WitheringPower>() && !targets[1].HasPower<WitheringPower>(), "Side-start consumed the paused owner's pending effect");
                drawGate.SetResult(); Await(setup);
                Check(PileType.Hand.GetPile(pausedOwner).Cards.Count == 4 && !targets[0].HasPower<WitheringPower>(), "Paused draw lost penalty or cleanup");
                drawnHands++;
            }
            finally { drawGate.TrySetResult(); pausedOwner = null; harmony.UnpatchAll(harmony.Id); }
        }
        Console.WriteLine($"PASS owner/edge timing: same-turn reservation, separate participant turns, ordinary draws and zero-card hands; {drawnHands} actual native hands drawn. No rendered UI, network clients, disk reload or game start.");
    }
}
