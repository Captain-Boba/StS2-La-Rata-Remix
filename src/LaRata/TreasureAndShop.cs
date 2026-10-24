using BaseLib.Abstracts;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using Godot;

namespace LaRata;

// The native treasure UI already supports several offers and one vote per player.
[HarmonyPatch(typeof(TreasureRoomRelicSynchronizer), nameof(TreasureRoomRelicSynchronizer.BeginRelicPicking))]
internal static class SoloTreasureChoicesPatch
{
    [HarmonyPostfix]
    private static void Postfix(IPlayerCollection ____playerCollection, List<RelicModel>? ____currentRelics,
        RelicGrabBag ____sharedGrabBag, Rng ____rng)
    {
        if (____playerCollection.Players.Count != 1 || ____currentRelics == null || ____currentRelics.Count == 0) return;
        var run = ____playerCollection.Players[0].RunState;
        while (____currentRelics.Count < 3)
        {
            var relic = ____sharedGrabBag.PullFromFront(RelicFactory.RollRarity(____rng), run) ?? RelicFactory.FallbackRelic;
            ____currentRelics.Add(relic);
        }
        ModEntry.Log.Info($"TREASURE solo offers={string.Join(',', ____currentRelics.Select(r => r.Id.Entry))}");
    }
}

[Pool(typeof(SharedRelicPool))]
public sealed class AThousandCuts : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override string PackedIconPath => "res://LaRata/images/a_thousand_cuts.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    public override List<(string, string)> Localization =>
    [
        ("title", "A Thousand Cuts"),
        ("description", "You may remove cards at the Merchant any number of times. Pay the normal cost each time. The removal price still increases after every purchase."),
        ("flavor", "The perfect deck is hiding inside your deck. Keep cutting.")
    ];
}

// Keep the original selection, payment, synchronized removal and saved price counter.
// Native restrictions (for example another relic disabling removal) still take precedence.
[HarmonyPatch(typeof(MerchantCardRemovalEntry), nameof(MerchantCardRemovalEntry.SetUsed))]
internal static class RepeatRemovalPatch
{
    [HarmonyPrefix]
    private static bool Prefix(Player ____player)
    {
        if (!____player.Relics.Any(r => r is AThousandCuts) || !Hook.ShouldAllowMerchantCardRemoval(____player.RunState, ____player)) return true;
        return false;
    }
}

[HarmonyPatch(typeof(MerchantCardRemovalEntry), nameof(MerchantCardRemovalEntry.Used), MethodType.Getter)]
internal static class RemovalAvailablePatch
{
    [HarmonyPostfix]
    private static void Postfix(Player ____player, ref bool __result)
    {
        if (!____player.Relics.Any(r => r is AThousandCuts) || !Hook.ShouldAllowMerchantCardRemoval(____player.RunState, ____player)) return;
        __result = false;
    }
}

// The native entry caches its base price for a single purchase. Recalculate at
// the price boundary, before affordability, display and payment read it, rather
// than as a side effect of querying Used. Native synchronization owns the saved
// removal count; native Cost still applies discounts and other price hooks.
[HarmonyPatch(typeof(MerchantEntry), nameof(MerchantEntry.Cost), MethodType.Getter)]
internal static class RepeatRemovalPricePatch
{
    [HarmonyPrefix]
    private static void Prefix(MerchantEntry __instance, Player ____player)
    {
        if (__instance is MerchantCardRemovalEntry && ____player.Relics.Any(r => r is AThousandCuts))
            __instance.CalcCost();
    }
}

// Buying the relic after already using removal in this very shop reopens the native slot.
[HarmonyPatch(typeof(NMerchantCardRemoval), "UpdateVisual")]
internal static class ReopenRemovalVisualPatch
{
    [HarmonyPrefix]
    private static void Prefix(MerchantCardRemovalEntry ____removalEntry, ref bool ____isUnavailable,
        AnimationPlayer ____animator, Control ____hitbox)
    {
        if (!____isUnavailable || ____removalEntry.Used) return;
        ____isUnavailable = false;
        ____hitbox.MouseFilter = Control.MouseFilterEnum.Stop;
        if (____animator.HasAnimation("RESET")) ____animator.Play("RESET");
    }
}
