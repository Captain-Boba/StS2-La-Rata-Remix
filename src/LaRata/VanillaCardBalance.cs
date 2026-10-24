using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace LaRata;

// Keep the native card identity, pool, effects, upgrades and save format. The
// same rarity is used by reward/shop selection and by the card's visual banner.
[HarmonyPatch(typeof(CardModel), nameof(CardModel.Rarity), MethodType.Getter)]
internal static class BarricadeRarityPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref CardRarity __result)
    {
        if (__instance is Barricade) __result = CardRarity.Uncommon;
    }
}
