using System.Text.Json;
using BaseLib.Abstracts;
using LaRata;
using MegaCrit.Sts2.Core.Models;

internal static class AssetPathChecks
{
    internal static void Run()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
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
            Check(relic.Id.ToString(), "relic", relic.PackedIconPath);
        }
        string map = ModelDb.Encounter<RatKingEncounter>().BossNodePath;
        Check("RatKing", "map", map + ".png"); Check("RatKing", "map-outline", map + "_outline.png");
        File.WriteAllText(Path.Combine(root, "artifacts/dev-wave-20261025/asset-model-bindings.json"), JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"PASS: {rows.Count} actual model portrait/relic/map bindings resolve to files inside assets/LaRata. Filesystem only; no Godot import, native resource loading or live UI.");
    }
}
