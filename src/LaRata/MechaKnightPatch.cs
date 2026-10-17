using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace LaRata;

[HarmonyPatch(typeof(MechaKnight), "GenerateMoveStateMachine")]
internal static class MechaKnightPatch
{
    internal const string SuppressionId = "LA_RATA_SUPPRESSION_MOVE";

    [HarmonyPostfix]
    private static void Postfix(MechaKnight __instance, MonsterMoveStateMachine __result)
    {
        if (!__result.States.TryGetValue("FLAMETHROWER_MOVE", out var original) ||
            original is not MoveState flamethrower || flamethrower.FollowUpState?.Id != "WINDUP_MOVE" ||
            __result.States.ContainsKey(SuppressionId))
            throw new InvalidOperationException("LaRata: unexpected Mecha Knight graph; incompatible monster patch/version.");

        var suppression = new MoveState(SuppressionId,
            targets => Suppress(__instance, targets), new DebuffIntent(strong: true))
        {
            FollowUpState = flamethrower.FollowUpState
        };
        __result.States.Add(suppression.Id, suppression);
        flamethrower.FollowUpState = suppression;
    }

    private static async Task Suppress(MechaKnight monster, IReadOnlyList<Creature> targets)
    {
        // Targets come from the game's deterministic player order. Await all gameplay commands;
        // no async-void hooks, host-only mutations, local-player selection or wall-clock state.
        var livingTargets = targets.Where(target => target.IsAlive).ToArray();
        var context = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<WeakPower>(context, livingTargets, 3m, monster.Creature, null);
        await PowerCmd.Apply<FrailPower>(context, livingTargets, 3m, monster.Creature, null);
        ModEntry.Log.Info($"Suppression resolved for {livingTargets.Length} living players (3 Weak + 3 Frail before native duration ticks).");
    }
}
