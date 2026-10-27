using System.Text.Json;
using HarmonyLib;
using BaseLib.Abstracts;
using LaRata;
using MegaCrit.Sts2.Core.Models;

internal static class AssetPathChecks
{
    internal static void Run(string? projectRoot = null)
    {
        var root = Path.GetFullPath(projectRoot ?? Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var rows = new List<object>();
        void Check(string id, string kind, string? path)
        {
            if (path == null || !path.StartsWith("res://LaRata/", StringComparison.Ordinal)) throw new Exception("Non-mod asset path: " + id);
            string relative = "assets/" + path[6..];
            string local = Path.GetFullPath(Path.Combine(root, relative));
            if (!local.StartsWith(Path.Combine(root, "assets") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(local))
                throw new Exception("Missing/outside asset: " + id + " -> " + relative);
            rows.Add(new { id, kind, path = relative });
        }
        foreach (var entry in LaRataCardCatalog.Entries)
            Check(entry.ModelId.ToString(), "card", ((CustomCardModel)entry.Model).CustomPortraitPath);
        foreach (var type in typeof(ModEntry).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(CustomRelicModel).IsAssignableFrom(t)))
        {
            var relic = ModelDb.GetById<RelicModel>(ModelDb.GetId(type));
            string small = relic.PackedIconPath;
            string outline = (string)AccessTools.PropertyGetter(type, "PackedIconOutlinePath").Invoke(relic, null)!;
            string big = (string)AccessTools.PropertyGetter(type, "BigIconPath").Invoke(relic, null)!;
            Check(relic.Id.ToString(), "relic-small", small);
            Check(relic.Id.ToString(), "relic-outline", outline);
            Check(relic.Id.ToString(), "relic-big", big);
            string stem = Path.GetFileNameWithoutExtension(small);
            string expectedBig = "res://LaRata/images/" + (stem is "a_thousand_cuts" or "well" ? "" : "relics/") + stem + ".png";
            if (small != "res://LaRata/images/relic_native/" + stem + ".png" ||
                outline != "res://LaRata/images/relic_native/" + stem + "_outline.png" || big != expectedBig)
                throw new Exception("Small/outline/original-big contract differs: " + relic.Id);
        }
        string map = ModelDb.Encounter<RatKingEncounter>().BossNodePath;
        Check("RatKing", "map", map + ".png"); Check("RatKing", "map-outline", map + "_outline.png");
        string output = Path.Combine(root, "artifacts/feedback-wave-20261027/asset-model-bindings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        File.WriteAllText(output, JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"PASS: {rows.Count} actual model portrait/relic/map bindings resolve to files inside assets/LaRata. Filesystem only; no Godot import, native resource loading or live UI.");
    }
}
