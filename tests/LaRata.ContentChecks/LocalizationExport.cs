using System.Text.Json;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using BaseLib.Patches.Localization;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.TestSupport;

internal static class LocalizationExport
{
    internal static void Run(string directory)
    {
        if (Directory.Exists(directory)) throw new InvalidOperationException("Export directory must be new: " + directory);
        TestMode.IsOn = true;
        var harmony = new Harmony("LaRata.ContentChecks.LocalizationExport");
        NativeConsoleFixture.PatchDesktop(harmony);
        // Use the installed BaseLib implementation, including CustomID and namespace prefixes.
        harmony.CreateClassProcessor(typeof(PrefixIdPatch)).Patch();
        try
        {
            var models = typeof(AbstractModel).Assembly.GetTypes().Concat(typeof(ModEntry).Assembly.GetTypes())
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(AbstractModel)) && t.GetConstructor(Type.EmptyTypes) != null).ToArray();
            foreach (var type in models) ModelDb.Inject(type);
            var map = (Dictionary<string, string?>)AccessTools.Field(typeof(ILocalizationProvider).Assembly.GetType("BaseLib.Patches.Localization.ModelLocPatch")!, "CategoryToLocTable").GetValue(null)!;
            var tables = new SortedDictionary<string, SortedDictionary<string, string>>(StringComparer.Ordinal);
            int providers = 0;
            foreach (var type in models.Where(t => t.Assembly == typeof(ModEntry).Assembly))
            {
                var model = ModelDb.GetById<AbstractModel>(ModelDb.GetId(type));
                if (model is not ILocalizationProvider provider || provider.Localization is not { Count: > 0 } pairs) continue;
                string table = provider.LocTable ?? map.GetValueOrDefault(model.Id.Category) ?? throw new Exception("Missing table: " + model.Id);
                if (!tables.TryGetValue(table, out var entries)) tables[table] = entries = new(StringComparer.Ordinal);
                foreach (var (suffix, value) in pairs)
                {
                    string key = model.Id.Entry + "." + suffix;
                    if (!entries.TryAdd(key, SimpleLoc.TrySimplify(value))) throw new Exception("Duplicate localization key: " + key);
                }
                providers++;
            }
            Directory.CreateDirectory(directory);
            foreach (var (table, entries) in tables)
                File.WriteAllText(Path.Combine(directory, table + ".json"), JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"EXPORTED: {providers} actual model providers, {tables.Sum(t => t.Value.Count)} entries in {tables.Count} tables; native ModelDb + installed BaseLib ID prefix/table mapping/SimpleLoc. Engine and mod initialization not run.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }
}
