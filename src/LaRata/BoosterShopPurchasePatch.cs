using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;

namespace LaRata;

// The native shop transmits RewardObtainedMessage only after RelicCmd.Obtain
// finishes. Resolving a booster inside that Obtain sends Opened=true, causing
// remote peers to skip the choice and leave their choice-ID counter behind.
// Keep the native payment, inventory mutation and single reward transmission;
// await the booster after transmission, but before the purchase can complete.
[HarmonyPatch(typeof(MerchantRelicEntry), "OnTryPurchase")]
internal static class BoosterShopPurchasePatch
{
    [HarmonyPrefix]
    private static void Prefix(MerchantRelicEntry __instance, out BoosterRelic? __state)
    {
        __state = __instance.Model as BoosterRelic;
        if (__state != null) __state.ShopOpeningDeferred = true;
    }

    [HarmonyPostfix]
    private static void Postfix(ref Task<(bool, int)> __result, BoosterRelic? __state)
    {
        if (__state != null) __result = CompletePurchase(__result, __state);
    }

    private static async Task<(bool, int)> CompletePurchase(Task<(bool, int)> purchase, BoosterRelic booster)
    {
        try
        {
            var result = await purchase;
            booster.ShopOpeningDeferred = false;
            // Use the same hook as a remote native Obtain, including its relic
            // attribution scope. StarterPending and Opened retain their guards.
            if (result.Item1) await booster.AfterObtained();
            return result;
        }
        finally { booster.ShopOpeningDeferred = false; }
    }

    // Async faults are handled above; preserve and clean up any synchronous
    // failure from the native method or another patch as well.
    [HarmonyFinalizer]
    private static Exception? Finalizer(Exception? __exception, BoosterRelic? __state)
    {
        if (__exception != null && __state != null) __state.ShopOpeningDeferred = false;
        return __exception;
    }
}
