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
    public override string PackedIconPath => "res://LaRata/images/relic_native/a_thousand_cuts.png";
    protected override string PackedIconOutlinePath => "res://LaRata/images/relic_native/a_thousand_cuts_outline.png";
    protected override string BigIconPath => "res://LaRata/images/a_thousand_cuts.png";
    public override List<(string, string)> Localization => [];

    internal static bool IsActiveFor(Player player) =>
        player.Relics.Any(r => r is AThousandCuts && !r.IsMelted);
}

// Keep the original selection, payment, synchronized removal and saved price counter.
// Native restrictions (for example another relic disabling removal) still take precedence.
// Do not suppress SetUsed: its underlying native value must survive relic loss.
[HarmonyPatch(typeof(MerchantCardRemovalEntry), nameof(MerchantCardRemovalEntry.Used), MethodType.Getter)]
internal static class RemovalAvailablePatch
{
    [HarmonyPostfix]
    private static void Postfix(Player ____player, ref bool __result)
    {
        if (!AThousandCuts.IsActiveFor(____player) || !Hook.ShouldAllowMerchantCardRemoval(____player.RunState, ____player)) return;
        __result = false;
    }
}

// Native CalcCost at removal index zero is exactly its own BaseCost getter.
// Reuse that getter so Inflation stays native; never mutate the saved counter
// or freeze a potentially increased price when acquiring the relic.
[HarmonyPatch(typeof(MerchantCardRemovalEntry), nameof(MerchantCardRemovalEntry.CalcCost))]
internal static class FirstRemovalBasePricePatch
{
    private static readonly Func<int> NativeFirstCost = AccessTools
        .PropertyGetter(typeof(MerchantCardRemovalEntry), "BaseCost").CreateDelegate<Func<int>>();

    [HarmonyPostfix]
    private static void Postfix(Player ____player, ref int ____cost)
    {
        if (AThousandCuts.IsActiveFor(____player)) ____cost = NativeFirstCost();
    }
}

// Recalculate before display, affordability and the transmitted payment read.
// Also recalculate after relic loss: an entry must not retain its ATC base price.
// Native Cost still applies current discounts and other merchant-price hooks.
[HarmonyPatch(typeof(MerchantEntry), nameof(MerchantEntry.Cost), MethodType.Getter)]
internal static class RepeatRemovalPricePatch
{
    [HarmonyPrefix]
    private static void Prefix(MerchantEntry __instance)
    {
        if (__instance is MerchantCardRemovalEntry)
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
