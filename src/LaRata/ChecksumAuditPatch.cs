using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Runs;

namespace LaRata;

// Observe the game's own checksums; never replace its serializer, hash or network validation.
[HarmonyPatch(typeof(ChecksumTracker), nameof(ChecksumTracker.GenerateChecksum), [typeof(string), typeof(GameAction)])]
internal static class ChecksumAuditPatch
{
    [HarmonyPostfix]
    private static void Postfix(ChecksumTracker __instance, IRunState ____runState, string context, NetChecksumData __result)
    {
        if (!ModEntry.AuditChecksums || !__instance.IsEnabled) return;
        ModEntry.Log.Info($"AUDIT checksum id={__result.id} value={__result.checksum} context={context}");

        // Match NetFullCombatState.FromRun's combat source, including out-of-combat
        // checksums. Include every run player and explicit zeroes for peer comparison.
        var combat = ____runState.Players.FirstOrDefault()?.Creature.CombatState;
        var pending = combat == null ? null : ResetEnergyLedger.Pending(combat);
        var owners = string.Join(",", ____runState.Players.OrderBy(player => player.NetId)
            .Select(player => FormattableString.Invariant($"{player.NetId}:{pending?.GetValueOrDefault(player.NetId) ?? 0}")));
        ModEntry.Log.Info(FormattableString.Invariant(
            $"AUDIT reset id={__result.id} combat={(combat == null ? 0 : 1)} round={combat?.RoundNumber ?? 0} side={combat?.CurrentSide.ToString() ?? "None"} owners=[{owners}]"));
    }
}
