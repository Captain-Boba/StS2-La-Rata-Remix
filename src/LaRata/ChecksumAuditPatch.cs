using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.GameActions;

namespace LaRata;

// Observe the game's own checksums; never replace its serializer, hash or network validation.
[HarmonyPatch(typeof(ChecksumTracker), nameof(ChecksumTracker.GenerateChecksum), [typeof(string), typeof(GameAction)])]
internal static class ChecksumAuditPatch
{
    [HarmonyPostfix]
    private static void Postfix(ChecksumTracker __instance, string context, NetChecksumData __result)
    {
        if (ModEntry.AuditChecksums && __instance.IsEnabled)
            ModEntry.Log.Info($"AUDIT checksum id={__result.id} value={__result.checksum} context={context}");
    }
}
