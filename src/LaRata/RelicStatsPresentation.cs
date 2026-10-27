using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace LaRata;

// Presentation only: one existing relic description plus short, reliable results.
// Coverage, source attribution and deferred effects remain internal to the ledger.
internal static class RelicStatsPresentation
{
    private static readonly IReadOnlyDictionary<string, string> ResultUnits = new Dictionary<string, string>
    {
        ["block_gained"] = "Block", ["hp_restored"] = "HP", ["max_hp_gained"] = "HP",
        ["gold_gained"] = "Gold", ["energy_gained"] = "Energy", ["stars_gained"] = "Stars",
        ["cards_drawn"] = "Cards", ["cards_removed"] = "Cards", ["cards_recovered"] = "Cards",
        ["energy_saved"] = "Energy", ["stars_saved"] = "Stars"
    };

    internal static IEnumerable<string> ResultLines(RelicModel relic)
    {
        if (!RelicMetrics.Enabled) yield break;
        if (!relic.IsMutable || relic.Owner == null) yield break;
        var safe = new Dictionary<(string Key, string Unit), decimal>();
        foreach (var metric in RelicMetrics.Read(relic).Metrics)
        {
            if (metric.Attribution != "Exact" || metric.Value <= 0) continue;
            bool power = metric.Key is "power_applied" or "power_applied:other";
            if (power)
            {
                var model = SafePower(metric.Key, metric.Unit);
                if (model == null) continue;
            }
            else if (!ResultUnits.TryGetValue(metric.Key, out string? expectedUnit) || metric.Unit != expectedUnit) continue;
            var id = (metric.Key, metric.Unit);
            safe[id] = safe.GetValueOrDefault(id) + metric.Value;
        }
        foreach (var pair in safe.OrderBy(p => p.Key.Key, StringComparer.Ordinal).ThenBy(p => p.Key.Unit, StringComparer.Ordinal))
        {
            decimal value = pair.Value;
            string number = value.ToString("#,0.###", LocManager.Instance.CultureInfo);
            if (pair.Key.Key is "power_applied" or "power_applied:other")
            {
                var power = SafePower(pair.Key.Key, pair.Key.Unit);
                if (power != null) yield return UiText.Format("stats.result.power", number, value, power.Title.GetFormattedText());
            }
            else yield return UiText.Format("stats.result." + pair.Key.Key, number, value);
        }
    }

    private static PowerModel? SafePower(string key, string unit)
    {
        if (!unit.StartsWith("PowerStacks:", StringComparison.Ordinal)) return null;
        string id = unit[12..];
        var parts = id.Split('.');
        if (parts.Length != 2 || parts.Any(string.IsNullOrWhiteSpace)) return null;
        var power = ModelDb.GetByIdOrNull<PowerModel>(new ModelId(parts[0], parts[1]));
        return power?.Type == (key == "power_applied:other" ? PowerType.Debuff : PowerType.Buff) ? power : null;
    }

    [HarmonyPatch(typeof(RelicModel), nameof(RelicModel.HoverTip), MethodType.Getter)]
    [HarmonyPatchCategory(RelicMetrics.PatchCategory)]
    private static class NativeRunResults
    {
        [HarmonyPostfix]
        private static void Postfix(RelicModel __instance, ref HoverTip __result)
        {
            string[] lines = ResultLines(__instance).ToArray();
            if (lines.Length == 0) return;
            var original = __result;
            __result = new HoverTip(__instance.Title, original.Description + "\n\n" + string.Join("\n", lines), original.Icon)
            {
                Id = original.Id, IsSmart = original.IsSmart, IsDebuff = original.IsDebuff,
                IsInstanced = original.IsInstanced, ShouldOverrideTextOverflow = original.ShouldOverrideTextOverflow
            };
            if (original.CanonicalModel != null) __result.SetCanonicalModel(original.CanonicalModel);
        }
    }

    [HarmonyPatch(typeof(NRelicInventory), "OnRelicClicked")]
    private static class ReopenRemoval
    {
        [HarmonyPrefix] private static bool Prefix(RelicModel model)
        {
            if (model is not RelicRemover remover || !RelicRemover.CanReopen(remover)) return true;
            RelicRemover.RequestOpen(remover);
            return false;
        }
    }
}
