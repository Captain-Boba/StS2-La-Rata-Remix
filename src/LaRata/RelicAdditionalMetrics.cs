using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Orbs;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;

namespace LaRata;

internal static class RelicGeneratedCardMetrics
{
    private static readonly ConditionalWeakTable<CardModel, RelicModel> Pending = new();
    internal static void Added(CardModel card)
    {
        if (!Pending.TryGetValue(card, out var source)) return;
        Pending.Remove(card);
        RelicMetrics.Record(source, "cards_generated", 1, "Cards");
    }
    [HarmonyPatchCategory(RelicMetrics.PatchCategory)]
    [HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardGenerated))]
    private static class Generated
    {
        private static void Postfix(CardModel card)
        {
            // Native history logs generation BEFORE Add and can include failed
            // additions. Mark the card; count only a subsequent actual pile add.
            if (RelicMetricSource.Relic is not { } source) return;
            Pending.Remove(card); Pending.Add(card, source);
        }
    }
}

[HarmonyPatchCategory(RelicMetrics.PatchCategory)]
[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.OrbChanneled))]
internal static class RelicOrbChannelMetrics
{
    private static void Postfix() => RelicMetrics.Record(RelicMetricSource.Relic, "orbs_channeled", 1, "Orbs");
}

[HarmonyPatchCategory(RelicMetrics.PatchCategory)]
[HarmonyPatch(typeof(OrbQueue), nameof(OrbQueue.AddCapacity))]
internal static class RelicOrbCapacityMetrics
{
    private static void Prefix(OrbQueue __instance, out int __state) => __state = __instance.Capacity;
    private static void Postfix(OrbQueue __instance, int __state)
    {
        int delta = __instance.Capacity - __state;
        if (delta > 0) RelicMetrics.Record(RelicMetricSource.Relic, "orb_slots_gained", delta, "OrbSlots");
    }
}

[HarmonyPatchCategory(RelicMetrics.PatchCategory)]
[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.Summoned))]
internal static class RelicSummonMetrics
{
    private static void Postfix(int amount) => RelicMetrics.Record(RelicMetricSource.Relic, "osty_hp_summoned", amount, "HP");
}

[HarmonyPatchCategory(RelicMetrics.PatchCategory)]
[HarmonyPatch(typeof(CardModel), nameof(CardModel.CurrentUpgradeLevel), MethodType.Setter)]
internal static class RelicUpgradeMetrics
{
    private static void Prefix(CardModel __instance, out int __state) => __state = __instance.CurrentUpgradeLevel;
    private static void Postfix(CardModel __instance, int __state)
    {
        // CardCmd.Upgrade is for real upgrades. Direct preview UpgradeInternal
        // calls are outside that boundary and cannot inflate a run counter.
        if (RelicResolution.In("upgrade") && __instance.CurrentUpgradeLevel > __state)
            RelicMetrics.Record(RelicMetricSource.Relic, "card_upgrades", __instance.CurrentUpgradeLevel - __state, "Upgrades");
    }
}

[HarmonyPatchCategory(RelicMetrics.PatchCategory)]
[HarmonyPatch(typeof(CardCmd), nameof(CardCmd.Enchant), [typeof(EnchantmentModel), typeof(CardModel), typeof(decimal)])]
internal static class RelicEnchantmentMetrics
{
    private static void Prefix(CardModel card, out (ModelId? Id, int? Amount) __state) =>
        __state = (card.Enchantment?.Id, card.Enchantment?.Amount);
    private static void Postfix(CardModel card, EnchantmentModel? __result, (ModelId? Id, int? Amount) __state)
    {
        // Offered reward/shop candidates do not yet belong to a pile. Their
        // enchantment is not claimed as an acquired benefit.
        if (__result != null && card.Pile != null &&
            (__result.Id != __state.Id || __result.Amount != __state.Amount))
            RelicMetrics.Record(RelicMetricSource.Relic, "cards_enchanted", 1, "Cards");
    }
}

[HarmonyPatchCategory(RelicMetrics.PatchCategory)]
[HarmonyPatch(typeof(Player), nameof(Player.AddPotionInternal))]
internal static class RelicPotionMetrics
{
    private static void Postfix(PotionProcureResult __result)
    { if (__result.success) RelicMetrics.Record(RelicMetricSource.Relic, "potions_added", 1, "Potions"); }
}

[HarmonyPatchCategory(RelicMetrics.PatchCategory)]
[HarmonyPatch(typeof(Player), "SetMaxPotionCountInternal")]
internal static class RelicPotionCapacityMetrics
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var previous = generator.DeclareLocal(typeof(int));
        var get = AccessTools.PropertyGetter(typeof(Player), nameof(Player.MaxPotionCount));
        yield return new(OpCodes.Ldarg_0);
        yield return new(OpCodes.Call, get);
        yield return new(OpCodes.Stloc, previous);
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldfld && instruction.operand is FieldInfo field && field.Name == "MaxPotionCountChanged")
            {
                // Stack already holds the Player for the event-field read.
                var first = new CodeInstruction(OpCodes.Dup);
                first.labels.AddRange(instruction.labels); instruction.labels.Clear();
                yield return first;
                yield return new(OpCodes.Ldloc, previous);
                yield return new(OpCodes.Ldarg_0);
                yield return new(OpCodes.Call, get);
                yield return CodeInstruction.Call(typeof(RelicPotionCapacityMetrics), nameof(Observe));
            }
            yield return instruction;
        }
    }
    internal static void Observe(Player player, int before, int after)
    {
        int delta = after - before;
        if (delta != 0) RelicMetrics.Record(RelicMetricSource.Relic,
            delta > 0 ? "potion_slots_gained" : "potion_slots_lost", Math.Abs((decimal)delta), "PotionSlots");
    }
}

// Preserve the unchanged native purchase/counter path. This only establishes
// source attribution on every peer, including the native remote completion path.
[HarmonyPatchCategory(RelicMetrics.PatchCategory)]
[HarmonyPatch(typeof(OneOffSynchronizer), "DoMerchantCardRemoval", MethodType.Async)]
internal static class ThousandCutsMetricContext
{
    private static void Prefix(object __instance, out RelicMetricSource.Target? __state)
    {
        var player = __instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => f.FieldType == typeof(Player)).Select(f => f.GetValue(__instance) as Player).FirstOrDefault();
        var source = player?.Relics.OfType<AThousandCuts>().FirstOrDefault(r => !r.IsMelted);
        __state = RelicMetricSource.Enter(source);
    }
    private static Exception? Finalizer(Exception? __exception, RelicMetricSource.Target? __state)
    { RelicMetricSource.Restore(__state); return __exception; }
}

[HarmonyPatchCategory(RelicMetrics.PatchCategory)]
[HarmonyPatch(typeof(ExtraPlayerFields), nameof(ExtraPlayerFields.CardShopRemovalsUsed), MethodType.Setter)]
internal static class ThousandCutsCompletedMetrics
{
    private static void Prefix(ExtraPlayerFields __instance, out int __state) => __state = __instance.CardShopRemovalsUsed;
    private static void Postfix(ExtraPlayerFields __instance, int __state)
    {
        if (RelicMetricSource.Relic is not AThousandCuts source || !ReferenceEquals(source.Owner.ExtraFields, __instance)) return;
        int delta = __instance.CardShopRemovalsUsed - __state;
        if (delta > 0) RelicMetrics.Record(source, "paid_card_removals", delta, "Cards");
    }
}
