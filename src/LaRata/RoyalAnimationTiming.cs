using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace LaRata;

// BaseLib routes the native creature triggers to this AnimationPlayer. Match
// CreatureCmd.TriggerAnim's native Fast/Instant waits without changing its
// commands or any other creature's animation policy.
internal static class RoyalAnimationTiming
{
    private static AnimationPlayer? Player(NCreature creature) =>
        creature.Entity?.Monster is RatKing
            ? creature.Visuals?.GetNodeOrNull<AnimationPlayer>("Visuals/AnimationPlayer") : null;

    [HarmonyPatch(typeof(NCreature), nameof(NCreature.SetAnimationTrigger))]
    private static class Trigger
    {
        [HarmonyPrefix, HarmonyPriority(Priority.First)]
        private static void Prefix(NCreature __instance, string trigger)
        {
            if (Player(__instance) is not { } player) return;
            player.SpeedScale = (trigger is "Attack" or "Cast" or "Hit") &&
                SaveManager.Instance.PrefsSave.FastMode == FastModeType.Fast ? 2 : 1;
        }

        [HarmonyPostfix]
        private static void Postfix(NCreature __instance, string trigger)
        {
            if (trigger is not ("Attack" or "Cast" or "Hit") ||
                SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant ||
                Player(__instance) is not { } player) return;
            player.Play("Idle");
            player.Advance(0);
        }
    }

    [HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
    private static class Death
    {
        [HarmonyPrefix, HarmonyPriority(Priority.First)]
        private static void Prefix(NCreature __instance)
        {
            // Native death uses Cmd.Wait rather than CustomScaledWait: Fast mode
            // still waits the complete DeathAnimLengthOverride.
            if (Player(__instance) is { } player) player.SpeedScale = 1;
        }

        [HarmonyPostfix]
        private static void Postfix(NCreature __instance)
        {
            if (SaveManager.Instance.PrefsSave.FastMode != FastModeType.Instant ||
                Player(__instance) is not { } player || !GodotObject.IsInstanceValid(player) ||
                !player.HasAnimation("Dead")) return;
            player.Play("Dead");
            player.Seek(player.GetAnimation("Dead").Length, update: true);
            player.Pause();
        }
    }
}
