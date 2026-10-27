using HarmonyLib;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;

namespace LaRata;

// A completed auto-play is distinct from damage dealt by that card. Native card
// callbacks mask the relic while resolving their own effects; history completion
// returns to the originating relic's command, if there is one.
[HarmonyPatchCategory(RelicMetrics.PatchCategory)]
[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardPlayFinished))]
internal static class RelicAutoPlayMetrics
{
    private static void Postfix(CardPlay cardPlay)
    {
        if (cardPlay.IsAutoPlay)
            RelicMetrics.Record(RelicMetricSource.Relic, "auto_plays_completed", 1, "Plays");
    }
}

// This is an actual mutation of blade base damage, not projected attack damage.
[HarmonyPatchCategory(RelicMetrics.PatchCategory)]
[HarmonyPatch(typeof(SovereignBlade), nameof(SovereignBlade.AddDamage))]
internal static class RelicForgeMetrics
{
    private static void Prefix(SovereignBlade __instance, out decimal __state) =>
        __state = __instance.DynamicVars.Damage.BaseValue;
    private static void Postfix(SovereignBlade __instance, decimal __state)
    {
        decimal delta = __instance.DynamicVars.Damage.BaseValue - __state;
        if (delta != 0) RelicMetrics.Record(RelicMetricSource.Relic, "blade_base_damage_adjustment", delta, "ForgeDamage");
    }
}

[HarmonyPatchCategory(RelicMetrics.PatchCategory)]
[HarmonyPatch(typeof(OrbCmd), nameof(OrbCmd.Passive))]
internal static class RelicDirectOrbPassiveMetrics
{
    private static void Prefix(out RelicMetricSource.Target? __state) =>
        __state = CombatManager.Instance.IsOverOrEnding ? null : RelicMetricSource.Attribution;
    private static void Postfix(RelicMetricSource.Target? __state, ref Task __result)
    {
        if (__state != null) __result = Observe(__result, __state);
    }
    private static async Task Observe(Task result, RelicMetricSource.Target source)
    {
        await result;
        RelicMetrics.RecordTarget(source, "orb_passives_completed", 1, "Activations");
    }
}

// Native AfterFlush supplies the cards that actually survived that flush. Cards
// with independent Retain are excluded; a second flush-prevention relic would
// make individual causal attribution ambiguous and is deliberately not split.
[HarmonyPatchCategory(RelicMetrics.PatchCategory)]
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterFlush))]
internal static class RelicPyramidRetentionMetrics
{
    private static void Prefix(Player player, IReadOnlyCollection<CardModel> retainedCards)
    {
        var sources = player.Relics.Where(r => !r.IsMelted && !r.HasBeenRemovedFromState &&
            r.GetType().GetMethod(nameof(AbstractModel.ShouldFlush))?.DeclaringType != typeof(AbstractModel)).ToArray();
        if (sources.Length != 1 || sources[0] is not RunicPyramid source) return;
        int retained = retainedCards.Count(c => !c.ShouldRetainThisTurn && c.Pile?.Type == PileType.Hand);
        RelicMetrics.Record(source, "cards_retained", retained, "Cards");
    }
}
