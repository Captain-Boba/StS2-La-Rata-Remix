using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Entities.Cards;

internal static class LocalizationWaveChecks
{
    private static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }
    private static Dictionary<string, string> Read(string path)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(path, new System.Text.UTF8Encoding(false, true)));
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in json.RootElement.EnumerateObject())
        {
            Require(entry.Value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(entry.Value.GetString()), "Empty/non-string: " + path + "/" + entry.Name);
            Require(result.TryAdd(entry.Name, entry.Value.GetString()!), "Duplicate JSON key: " + path + "/" + entry.Name);
        }
        return result;
    }
    private static HashSet<string> Variables(string text) => Regex.Matches(text, @"\{([A-Za-z_][A-Za-z_0-9]*|\d+)(?=[:}])").Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);
    private static bool CompatibleVariables(string key, string source, string localized, string language)
    {
        var expected = Variables(source);
        var actual = Variables(localized);
        // Chinese has no singular/plural inflection. Only the dedicated numeric
        // grammar operand may disappear; displayed values and power names must remain.
        if (language == "zhs" && key.StartsWith("stats.result.", StringComparison.Ordinal) &&
            !actual.Contains("1") && Regex.IsMatch(source, @"\{1:plural:") &&
            !Regex.IsMatch(source, @"\{1(?=[:}])(?!:plural:)")) expected.Remove("1");
        return expected.SetEquals(actual);
    }
    internal static void Run(string root)
    {
        TestMode.IsOn = true;
        var harmony = new Harmony("LaRata.ContentChecks.LocalizationWave");
        NativeConsoleFixture.PatchDesktop(harmony);
        harmony.CreateClassProcessor(typeof(PrefixIdPatch)).Patch();
        var oldManager = LocManager.Instance;
        var oldSmart = SmartFormat.Smart.Default;
        var oldFormatter = AccessTools.Field(typeof(LocManager), "_smartFormatter").GetValue(null);
        try
        {
            var types = typeof(AbstractModel).Assembly.GetTypes().Concat(typeof(ModEntry).Assembly.GetTypes())
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(AbstractModel)) && t.GetConstructor(Type.EmptyTypes) != null).ToArray();
            foreach (var type in types) ModelDb.Inject(type);
            var modModels = types.Where(t => t.Assembly == typeof(ModEntry).Assembly).Select(t => ModelDb.GetById<AbstractModel>(ModelDb.GetId(t))).ToArray();
            Require(modModels.OfType<ILocalizationProvider>().All(p => p.Localization == null || p.Localization.Count == 0), "English model provider still overwrites translated JSON at ModelDb.Init");
            var manager = (LocManager)RuntimeHelpers.GetUninitializedObject(typeof(LocManager));
            AccessTools.PropertySetter(typeof(LocManager), nameof(LocManager.Instance)).Invoke(null, [manager]);
            AccessTools.Method(typeof(LocManager), "LoadLocFormatters").Invoke(manager, null);
            var en = Directory.GetFiles(Path.Combine(root, "eng"), "*.json").ToDictionary(p => Path.GetFileNameWithoutExtension(p), Read);
            var owner = NativeConsoleFixture.Player(91);
            _ = RunState.CreateForTest(players: [owner], seed: "LOCALIZATION");
            var measured = NativeConsoleFixture.Relic<OldCoin>(owner); RelicMetrics.Obtained(measured);
            var contributions = (System.Collections.IDictionary)AccessTools.Field(typeof(ModEntry).Assembly.GetType("LaRata.RelicNumericContribution")!, "Supported").GetValue(null)!;
            foreach (System.Collections.DictionaryEntry item in contributions)
            {
                var spec = ((string Boundary, string Key, string Unit))item.Value!;
                RelicMetrics.Record(measured, spec.Key, 1.25m, spec.Unit, "Contribution");
            }
            foreach (var (key, unit) in new (string, string)[]
            {
                ("block_gained","Block"),("block_removed","Block"),("hp_restored","HP"),("hp_lost","HP"),
                ("max_hp_gained","HP"),("max_hp_lost","HP"),("gold_gained","Gold"),("gold_lost","Gold"),
                ("energy_gained","Energy"),("energy_lost","Energy"),("stars_gained","Stars"),("stars_lost","Stars"),
                ("power_applied","PowerStacks:" + ModelDb.Power<StrengthPower>().Id),("power_removed","PowerStacks:" + ModelDb.Power<StrengthPower>().Id),
                ("relics_removed","Relics"),("relics_gained","Relics"),("cards_drawn","Cards"),("cards_added","Cards"),
                ("cards_removed","Cards"),("cards_generated","Cards"),("cards_exhausted","Cards"),("cards_moved_to_discard","Cards"),
                ("card_upgrades","Upgrades"),("cards_enchanted","Cards"),("potions_added","Potions"),("potion_slots_gained","PotionSlots"),
                ("potion_slots_lost","PotionSlots"),("paid_card_removals","Cards"),("orbs_channeled","Orbs"),("osty_hp_summoned","HP"),
                ("rest_actions_completed","Activations"),("auto_plays_completed","Plays"),("blade_base_damage_adjustment","ForgeDamage"),
                ("cards_retained","Cards"),("orb_passives_completed","Activations"),
                ("energy_saved","Energy"),("stars_saved","Stars"),("energy_surcharge_paid","Energy"),("stars_surcharge_paid","Stars")
            }) RelicMetrics.Record(measured, key, 2, unit);
            RelicMetrics.Record(measured, "energy_cost_adjustment_used", -1, "EnergyValue", "Contribution");
            RelicMetrics.Record(measured, "stars_cost_adjustment_used", -2, "StarValue", "Contribution");
            var origin = RelicMetricSource.Enter(measured);
            RelicMetrics.MarkDeferred(RelicMetricSource.Attribution, "complex_payments_deferred");
            RelicMetricSource.Restore(origin);
            var deferredShop = NativeConsoleFixture.Relic<MembershipCard>(owner); RelicMetrics.Obtained(deferredShop);
            RelicMetrics.Record(measured, "hp_restored:other", 1, "HP");
            RelicMetrics.Record(measured, "cards_recovered", 2, "Cards");
            RelicMetrics.Record(measured, "block_gained", 999, "WrongUnit");
            RelicMetrics.Record(measured, "unknown_test_metric", 999, "Block");
            harmony.CreateClassProcessor(typeof(RelicStatsPresentation).GetNestedType("NativeRunResults", System.Reflection.BindingFlags.NonPublic)!).Patch();
            int formatted = 0;
            foreach (var (language, culture) in new[] { ("eng", "en-US"), ("deu", "de-DE"), ("zhs", "zh-CN") })
            {
                var localized = Directory.GetFiles(Path.Combine(root, language), "*.json").ToDictionary(p => Path.GetFileNameWithoutExtension(p), Read);
                Require(en.Keys.ToHashSet().SetEquals(localized.Keys), "Table parity: " + language);
                var tables = new Dictionary<string, LocTable>();
                foreach (var (name, source) in en)
                {
                    Require(source.Keys.ToHashSet().SetEquals(localized[name].Keys), $"Key parity {language}/{name}; missing={string.Join(',', source.Keys.Except(localized[name].Keys))}; extra={string.Join(',', localized[name].Keys.Except(source.Keys))}");
                    tables[name!] = new LocTable(name!, localized[name], new LocTable(name!, source));
                    foreach (var (key, text) in localized[name])
                    {
                        Require(CompatibleVariables(key, source[key], text, language), $"Placeholder parity: {language}/{name}/{key}");
                        _ = SmartFormat.Smart.Default.Parser.ParseFormat(text);
                    }
                }
                AccessTools.Field(typeof(LocManager), "_tables").SetValue(manager, tables);
                foreach (string nativeTable in new[] { "card_keywords", "static_hover_tips" })
                {
                    string path = Path.GetFullPath(Path.Combine(root, "../../../.research/native-reference-0.107.1-20261025/localization", language, nativeTable + ".json"));
                    var native = Read(path);
                    if (tables.TryGetValue(nativeTable, out var table)) table.MergeWith(native);
                    else tables[nativeTable] = new LocTable(nativeTable, native);
                }
                NativeConsoleFixture.Set(manager, nameof(LocManager.CultureInfo), CultureInfo.GetCultureInfo(culture));
                NativeConsoleFixture.Set(manager, nameof(LocManager.Language), language);
                foreach (var canonical in LaRataCardCatalog.Entries.Select(e => e.Model))
                foreach (bool upgraded in new[] { false, true })
                {
                    if (upgraded && canonical.MaxUpgradeLevel == 0) continue;
                    var card = canonical.ToMutable();
                    if (upgraded) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
                    string key = card.Id.Entry + ".description";
                    // Native descriptions supply IfUpgraded, InCombat and other
                    // contextual variables as well as the numeric DynamicVars.
                    string rendered = card.GetDescriptionForPile(PileType.None); formatted++;
                    Require(!rendered.Contains('{') && !rendered.Contains('}'), $"Unresolved card placeholder: {language}/{key}");
                    string nativeExhaust = (string)AccessTools.Method(typeof(CardModel).Assembly.GetType("MegaCrit.Sts2.Core.Entities.Cards.CardKeywordExtensions"), "GetCardText").Invoke(null, [CardKeyword.Exhaust])!;
                    if (card.Keywords.Contains(CardKeyword.Exhaust))
                        Require(Regex.Matches(rendered, Regex.Escape(nativeExhaust)).Count == 1 && rendered.EndsWith(nativeExhaust), $"Missing/duplicated native Exhaust: {language}/{key}: {rendered}");
                    if (card.Id.Entry == "LARATA-LORD_TIBOS_RESET")
                    {
                        Require(card.Keywords.Contains(CardKeyword.Exhaust) != upgraded && (upgraded ? !rendered.EndsWith(nativeExhaust) : rendered.EndsWith(nativeExhaust)), $"Reset upgrade Exhaust mismatch: {language}: {rendered}");
                        Console.WriteLine($"RESET {language}/{(upgraded ? "upgraded" : "base")}: {rendered.Replace('\n', '|')}");
                    }
                    if (card is EmeraldWard or GraveSpark)
                        Require(Regex.Matches(rendered, "energy_icon.png").Count == card.DynamicVars.Energy.IntValue, $"Missing native energy icons: {language}/{key}");
                    if (card is WrathOfTheBigO or TheKikilyst or SerKevsThornBuster)
                    {
                        string full = rendered;
                        string exhaust = (string)AccessTools.Method(typeof(CardModel).Assembly.GetType("MegaCrit.Sts2.Core.Entities.Cards.CardKeywordExtensions"), "GetCardText").Invoke(null, [CardKeyword.Exhaust])!;
                        Require(Regex.Matches(full, Regex.Escape(exhaust)).Count == 1 && full.EndsWith(exhaust), $"Missing/duplicated native Exhaust: {language}/{key}: {full}");
                        string plain = Regex.Replace(full, @"\[[^\]]*\]", "");
                        if (card is TheKikilyst) Require(Regex.IsMatch(plain, (upgraded ? "50" : "25") + @"\s*%"), $"Wrong percentage in full native description: {language}: {full}");
                        if (card is SerKevsThornBuster)
                        {
                            Require(plain.Contains('3'), $"Thorn Vulnerable amount missing: {language}: {full}");
                            Require(upgraded ? plain.Contains('2') : !Regex.IsMatch(plain, "Draw|Ziehe|抽", RegexOptions.IgnoreCase),
                                $"Thorn base/upgrade draw clause differs: {language}: {full}");
                        }
                        Console.WriteLine($"TEXT {language}/{card.Id.Entry}/{(upgraded ? "upgraded" : "base")}: {full.Replace('\n', '|')}");
                    }
                }
                foreach (var (key, text) in localized["larata_ui"])
                {
                    var names = Variables(text);
                    if (!names.All(n => int.TryParse(n, out _))) continue;
                    int count = names.Count == 0 ? 0 : names.Select(int.Parse).Max() + 1;
                    _ = UiText.Format(key, Enumerable.Range(0, count).Select(i => (object)(123 + i)).ToArray()); formatted++;
                }
                // Native power names are required by the production PowerStacks tooltip path.
                var nativePowerPath = Path.Combine(root, "../../../.research/native-reference-0.107.1-20261025/localization", language, "powers.json");
                tables["powers"].MergeWith(JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.GetFullPath(nativePowerPath)))!);
                string[] resultLines = RelicStatsPresentation.ResultLines(measured).ToArray();
                string measuredTip = string.Join('\n', resultLines);
                Require(resultLines.Length == 12 && resultLines.All(line => !line.Contains('{') && !line.Contains("999")), "Minimal results include unsupported/incorrect metrics or unresolved variables");
                Require(resultLines.Contains(UiText.Format("stats.result.block_gained", "2", 2m)) &&
                    !measuredTip.Contains(UiText.Get("stats.gap.complex_payments_deferred")) &&
                    !measuredTip.Contains(UiText.Get("stats.coverage.Complete")) &&
                    RelicMetrics.Read(measured).Gaps.Contains("complex_payments_deferred"), "Minimal tooltip lost exact result or exposed internal diagnostic"); formatted++;
                Require(!RelicStatsPresentation.ResultLines(deferredShop).Any() && RelicMetrics.Read(deferredShop).Metrics.Count == 0 &&
                    RelicMetrics.Read(deferredShop).Gaps.Contains("shop_savings_deferred"), "Deferred shop displayed fabricated zero/diagnostic"); formatted++;
                var nativeRelicsPath = Path.Combine(root, "../../../.research/native-reference-0.107.1-20261025/localization", language, "relics.json");
                tables["relics"].MergeWith(Read(Path.GetFullPath(nativeRelicsPath)));
                var integrated = measured.HoverTip;
                Require(integrated.Description == measured.DynamicDescription.GetFormattedText() + "\n\n" + measuredTip &&
                    integrated.CanonicalModel == measured.CanonicalInstance, "Native relic description or tooltip metadata replaced/duplicated");
                Require(!RelicStatsPresentation.ResultLines(ModelDb.Relic<OldCoin>()).Any(), "Canonical relic shows run values");
                foreach (var relic in modModels.OfType<DecisionRelic>())
                {
                    string description = relic.DynamicDescription.GetFormattedText();
                    Require(!description.Contains('{') && !description.Contains('}'), "Unresolved new relic text: " + relic.Id);
                    Console.WriteLine($"RELIC {language}/{relic.Id.Entry}: {description}"); formatted++;
                }
                foreach (var power in modModels.OfType<TwistStatusPower>())
                {
                    var description = power.Description; description.Add("Amount", 35);
                    string rendered = description.GetFormattedText();
                    Require(!rendered.Contains('{') && !rendered.Contains('}'), "Unresolved canonical twist text: " + power.Id);
                    Console.WriteLine($"TWIST {language}/{power.Id.Entry}: {rendered}"); formatted++;
                }
                foreach (bool inklet in new[] { false, true })
                foreach (int hp in new[] { 35, 60 })
                {
                    MonsterModel monster = inklet ? ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.Inklet>().ToMutable() : ModelDb.Monster<MegaCrit.Sts2.Core.Models.Monsters.LeafSlimeS>().ToMutable();
                    var creature = new MegaCrit.Sts2.Core.Entities.Creatures.Creature(monster, MegaCrit.Sts2.Core.Combat.CombatSide.Enemy, "0");
                    var marker = ModelDb.Power<ReinforcementTwistPower>().ToMutable(); marker.ApplyInternal(creature, hp, true);
                    var description = marker.Description; description.Add("Amount", hp);
                    string rendered = description.GetFormattedText();
                    int attack = inklet ? 100 : hp == 35 ? 50 : 65;
                    Require(rendered.Contains(hp.ToString()) && rendered.Contains(attack.ToString()), "Bound reinforcement HP/attack description differs: " + rendered); formatted++;
                }
                foreach (int amount in new[] { 1, 2, 12 })
                {
                    string line = UiText.Format("stats.result.cards_recovered", amount.ToString(CultureInfo.GetCultureInfo(culture)), (decimal)amount);
                    string word = language == "eng" ? (amount == 1 ? "card recovered" : "cards recovered") : language == "deu" ? (amount == 1 ? "Karte" : "Karten") : "牌";
                    Require(line.Contains(word) && line.Contains(amount.ToString()) && !line.Contains('{'), "Native result singular/plural format failed: " + line);
                    Console.WriteLine($"RESULT {language}/{amount}: {line}"); formatted++;
                }
                var fallback = new LocTable("probe", [], new LocTable("probe", new() { ["key"] = "English fallback" }));
                Require(fallback.GetRawText("key") == "English fallback", "Native LocTable fallback failed");
            }
            Console.WriteLine($"PASS: strict UTF-8/JSON duplicate checks, EN/DE/ZHS table/key/placeholder parity, empty model providers, {formatted} actual native formatter calls for card bases/upgrades and indexed UI, native English table fallback. Tables supplied from workspace files; engine/BaseLib asset discovery, fonts/layout and PCK loading not tested.");
        }
        finally
        {
            AccessTools.PropertySetter(typeof(LocManager), nameof(LocManager.Instance)).Invoke(null, [oldManager]);
            AccessTools.Field(typeof(LocManager), "_smartFormatter").SetValue(null, oldFormatter);
            SmartFormat.Smart.Default = oldSmart;
            harmony.UnpatchAll(harmony.Id);
        }
    }
}
