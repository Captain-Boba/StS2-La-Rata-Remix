using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;

namespace LaRata;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.FinalizeStartingRelics))]
internal static class StartingRelicsPatch
{
    [HarmonyPostfix]
    private static void Postfix(RunManager __instance, ref Task __result) => __result = Grant(__result, __instance);

    private static async Task Grant(Task original, RunManager manager)
    {
        await original;
        // Native new-run finalization is not called when loading or reconnecting.
        // Obtain owns inventory, history, grab-bag removal and native relic callbacks.
        var run = manager.DebugOnlyGetState();
        if (run == null) return;
        foreach (var player in run.Players.OrderBy(p => p.NetId).ToArray())
        {
            if (player.GetRelic<BowlerHat>() == null) await RelicCmd.Obtain<BowlerHat>(player);
            if (player.GetRelic<PotionBelt>() == null) await RelicCmd.Obtain<PotionBelt>(player);
            if (player.GetRelic<WhiteBeastStatue>() == null) await RelicCmd.Obtain<WhiteBeastStatue>(player);
            ModEntry.Log.Info($"BENEFIT starting-relics player={player.NetId} potion-slots={player.MaxPotionCount}");
        }
    }
}
