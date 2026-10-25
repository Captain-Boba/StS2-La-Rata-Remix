using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace LaRata;

// This is a coverage statement, never an invented zero or a promise that every
// consequence of a relic can be causally assigned to it.
internal static class RelicMetricCoverage
{
    internal static IReadOnlyList<string> Gaps(RelicModel relic)
    {
        var methods = relic.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.DeclaringType != typeof(RelicModel) && m.DeclaringType != typeof(AbstractModel))
            .Select(m => m.Name).ToHashSet(StringComparer.Ordinal);
        var gaps = new List<string>();
        if (relic is MembershipCard or TheCourier) gaps.Add("shop_savings_deferred");
        if (methods.Any(m => m.StartsWith("Modify", StringComparison.Ordinal) || m.StartsWith("Should", StringComparison.Ordinal)))
            gaps.Add("modifier_consequences");
        if (methods.Any(m => m.StartsWith("TryModify", StringComparison.Ordinal))) gaps.Add("offered_vs_obtained");
        if (relic.AddsPet || relic.SpawnsPets) gaps.Add("pet_consequences");
        // Buffs are measured as applied stacks; their later damage/block belongs
        // to a shared power and is deliberately not assigned a second time.
        gaps.Add("downstream_shared_effects");
        return gaps;
    }
}
