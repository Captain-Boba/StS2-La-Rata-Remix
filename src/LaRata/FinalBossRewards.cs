using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace LaRata;

// Native final-act rewards are empty, but our regular finale precedes an optional
// bonus act. Keep native reward rolls, hooks, ownership and synchronization.
[HarmonyPatch(typeof(RewardsSet), nameof(RewardsSet.WithRewardsFromRoom))]
internal static class FinalBossRewardsPatch
{
    private static readonly System.Reflection.MethodInfo Generate =
        AccessTools.Method(typeof(RewardsSet), "GenerateRewardsFor", [typeof(Player), typeof(AbstractRoom)]);

    [HarmonyPrefix]
    private static bool Prefix(RewardsSet __instance, AbstractRoom room, ref RewardsSet __result)
    {
        var run = __instance.Player.RunState;
        if (room is not CombatRoom combat || run.Act is RatDomain ||
            combat.Encounter is RatKingEncounter || combat.Encounter.IsDebugEncounter ||
            !combat.Encounter.ShouldGiveRewards || !EncounterDirector.IsFinalBoss(run, combat.Encounter))
            return true;

        __instance.EmptyForRoom(room);
        AddNativeBatch(__instance, combat);
        var rules = EncounterDirector.Get(combat.Encounter);
        if (rules is { BossDuo: true, BossSource: not null })
        {
            // Metadata-only room: do not enter it, generate enemies or advance the run.
            // Use the supporting boss's own gold range and the native boss reward rules.
            AddNativeBatch(__instance, new CombatRoom(rules.BossSource.ToMutable(), run));
        }
        if (combat.ExtraRewards.TryGetValue(__instance.Player, out var extras))
            __instance.Rewards.AddRange(extras);
        __result = __instance;
        return false;
    }

    private static void AddNativeBatch(RewardsSet rewards, CombatRoom room) =>
        rewards.Rewards.AddRange((List<Reward>)Generate.Invoke(rewards, [rewards.Player, room])!);
}
