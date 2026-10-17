using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;

namespace LaRata;

[HarmonyPatch(typeof(Player), nameof(Player.CreateForNewRun), [typeof(CharacterModel), typeof(UnlockState), typeof(ulong)])]
internal static class PotionCapacityPatch
{
    [HarmonyPostfix]
    private static void Postfix(Player __result)
    {
        // This path is never used by FromSerializable. Native save/network fields retain the capacity.
        __result.AddToMaxPotionCount(2);
        ModEntry.Log.Info($"BENEFIT new-player={__result.NetId} potion-slots={__result.MaxPotionCount}");
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyGoldGained))]
internal static class GoldBonusPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref decimal __result)
    {
        // Honor native prevention (e.g. Ectoplasm). Native GainGold truncates to whole coins.
        if (__result > 0) __result *= 1.25m;
    }
}
