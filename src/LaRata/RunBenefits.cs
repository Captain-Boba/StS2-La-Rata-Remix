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
                // The synchronized Ancient option opens it before granting the
                // chosen relic; no gameplay choices belong in a local fade task.
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

[HarmonyPatch(typeof(EventModel), "RelicOption", [typeof(RelicModel), typeof(Func<Task>), typeof(string)])]
internal static class StartingBoosterChoicePatch
{
    [HarmonyPrefix]
    private static void Prefix(EventModel __instance, ref Func<Task>? onChosen)
    {
        if (__instance is not AncientEventModel || onChosen == null) return;
        var ancient = __instance;
        var original = onChosen;
        onChosen = () => OpenThenChoose(ancient, original);
    }

    internal static async Task OpenThenChoose(EventModel ancient, Func<Task> original)
    {
        // EventSynchronizer executes this owner's option on every peer and
        // tracks its Task. EventRoom.Exit awaits it before combat-state sync.
        // Other players can finish their own packs independently, while later
        // choices from this Ancient's relic cannot overtake this player's pack.
        var owner = ancient.Owner ?? throw new InvalidOperationException("Starting booster requires an active Ancient event owner.");
        foreach (var pack in owner.Relics.OfType<BoosterRelic>()
                     .Where(p => p.StarterPending).ToArray())
            await pack.OpenStarter();
        await original();
    }
}
