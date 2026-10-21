using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
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
            var booster = ClassBooster(player.Character);
            if (player.Relics.All(relic => relic.Id != booster.Id))
            {
                var starter = (BoosterRelic)booster.ToMutable();
                // NRun and its overlays do not exist during FinalizeStartingRelics.
                // Keep the deferred opening in the native relic save properties.
                starter.StarterPending = true;
                await RelicCmd.Obtain(starter, player);
            }
            ModEntry.Log.Info($"BENEFIT starting-relics player={player.NetId} potion-slots={player.MaxPotionCount}");
        }
    }

    internal static BoosterRelic ClassBooster(CharacterModel character) => character switch
    {
        Ironclad => ModelDb.Relic<IroncladBooster>(),
        Silent => ModelDb.Relic<SilentBooster>(),
        Defect => ModelDb.Relic<DefectBooster>(),
        Regent => ModelDb.Relic<RegentBooster>(),
        Necrobinder => ModelDb.Relic<NecrobinderBooster>(),
        // Third-party characters have no dedicated pack among our six models.
        _ => ModelDb.Relic<RoyalBooster>()
    };
}

[HarmonyPatch(typeof(RunManager), "FadeIn")]
internal static class StartingBoosterOpeningPatch
{
    [HarmonyPostfix]
    private static void Postfix(RunManager __instance, ref Task __result) => __result = Open(__result, __instance);

    private static async Task Open(Task original, RunManager manager)
    {
        await original;
        // AfterRoomEntered runs beneath the opaque room transition. Wait until
        // the native fade has finished and location-targeted choices can arrive.
        var run = manager.DebugOnlyGetState();
        if (run == null) return;
        foreach (var player in run.Players.OrderBy(p => p.NetId).ToArray())
            foreach (var pack in player.Relics.OfType<BoosterRelic>().Where(p => p.StarterPending && !p.Opened).ToArray())
                await pack.OpenStarter();
    }
}
