using System.Text.Json;
using LaRata;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.TestSupport;
using System.Reflection;
using Cleanse = LaRata.Cleanse;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
static SavedProperties Roundtrip(AbstractModel model) => JsonSerializer.Deserialize(
    JsonSerializer.Serialize(SavedProperties.From(model)!, JsonSerializationUtility.GetTypeInfo<SavedProperties>()),
    JsonSerializationUtility.GetTypeInfo<SavedProperties>())!;
var types = typeof(ModEntry).Assembly.GetTypes();
// Exercise the real formatter: without (), SmartFormat treats energyIcons as
// a numeric format and silently prints a number without the energy symbol.
var formatter = SmartFormat.Smart.CreateDefaultSmartFormat();
formatter.AddExtensions(new MegaCrit.Sts2.Core.Localization.Formatters.EnergyIconsFormatter());
foreach (var energyCard in new RemixCard[] { new EmeraldWard(), new GraveSpark() })
{
    var description = energyCard.Localization.Single(l => l.Item1 == "description").Item2;
    var token = System.Text.RegularExpressions.Regex.Match(description, @"\{Energy:[^}]+\}").Value;
    foreach (var amount in new[] { 1, 2 })
    {
        var energy = new MegaCrit.Sts2.Core.Localization.DynamicVars.EnergyVar(amount) { ColorPrefix = "ironclad" };
        var rendered = formatter.Format(token, new { Energy = energy });
        Require(System.Text.RegularExpressions.Regex.Matches(rendered, "ironclad_energy_icon.png").Count == amount,
            $"Energy symbols missing from {energyCard.GetType().Name}: {rendered}");
    }
}
Console.WriteLine("PASS: Emerald Ward and Grave Spark use native energy symbols for base and upgraded values.");
TestMode.IsOn = true;
// Native test registration without launching the game's mod manager or UI.
// BaseLib's runtime ID-prefix patch is not installed in this model-only runner;
// leave the native namesake Cleanse out so the custom card can be registered.
foreach (var type in typeof(AbstractModel).Assembly.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(AbstractModel)) && t.GetConstructor(Type.EmptyTypes) != null && t != typeof(MegaCrit.Sts2.Core.Models.Cards.Cleanse)))
    ModelDb.Inject(type);
var cards = types.Where(t => !t.IsAbstract && (t.IsSubclassOf(typeof(RoyalCard)) || t.IsSubclassOf(typeof(RemixCard)) || t == typeof(Cleanse))).OrderBy(t => t.Name).ToArray();
Require(cards.Length == 36, "Expected 20 boss cards, Cleanse, 10 neutrals and 5 playable boss prizes.");
var models = new List<CardModel>();
foreach (var type in cards)
{
    ModelDb.Inject(type);
    var canonical = (CustomCardModel)ModelDb.GetById<CardModel>(ModelDb.GetId(type));
    var card = canonical.ToMutable();
    Require(card.EnergyCost.Canonical is >= 0 and <= 3, $"Invalid energy: {type.Name}");
    Require(canonical.Localization!.Any(l => l.Item1 == "title" && l.Item2.Length > 0), $"Missing title: {type.Name}");
    Require(canonical.CustomPortraitPath is not null, $"Missing portrait: {type.Name}");
    var save = card.ToSerializable();
    Require(save.Id == canonical.Id && save.CurrentUpgradeLevel == 0, $"Save identity: {type.Name}");
    if (canonical is RemixCard or Cleanse)
    {
        var before = canonical.DynamicVars.ToDictionary(v => v.Key, v => v.Value.BaseValue);
        if (card is EmeraldWard)
            Require(card.EnergyCost.Canonical == 2 && before["Block"] == 5 && before["Energy"] == 1, "Emerald Ward base: 2 cost, 5 Block, 1 Energy.");
        if (card is RoyalScepterPrize)
            Require(card.EnergyCost.Canonical == 2 && before["Damage"] == 10 && before["WeakPower"] == 1 && before["VulnerablePower"] == 1, "Scepter Storm base values.");
        card.UpgradeInternal();
        card.FinalizeUpgradeInternal();
        Require(card.DynamicVars.Any(v => v.Value.BaseValue != before[v.Key]) || card.EnergyCost.GetWithModifiers(CostModifiers.None) < canonical.EnergyCost.Canonical, $"Upgrade has no effect: {type.Name}");
        if (card is BorrowedBreath)
            Require(card.DynamicVars["Heal"].BaseValue == 6 && card.DynamicVars.Cards.BaseValue == 3, "Borrowed Breath+ must heal 6 and draw 3.");
        if (card is EmeraldWard)
            Require(card.EnergyCost.GetWithModifiers(CostModifiers.None) == 2 && card.DynamicVars.Block.BaseValue == 10 && card.DynamicVars.Energy.BaseValue == 2, "Emerald Ward+: 2 cost, 10 Block, 2 Energy.");
        if (card is RoyalScepterPrize)
            Require(card.EnergyCost.GetWithModifiers(CostModifiers.None) == 2 && card.DynamicVars.Damage.BaseValue == 20 && card.DynamicVars["WeakPower"].BaseValue == 2 && card.DynamicVars["VulnerablePower"].BaseValue == 2, "Scepter Storm+ values.");
        Require(canonical.DynamicVars.All(v => v.Value.BaseValue == before[v.Key]), $"Canonical values were modified: {type.Name}");
        var restored = CardModel.FromSerializable(JsonSerializer.Deserialize(
            JsonSerializer.Serialize(card.ToSerializable(), JsonSerializationUtility.GetTypeInfo<SerializableCard>()),
            JsonSerializationUtility.GetTypeInfo<SerializableCard>())!);
        Require(restored.CurrentUpgradeLevel == 1 && restored.DynamicVars.All(v => v.Value.BaseValue == card.DynamicVars[v.Key].BaseValue), $"Upgrade save roundtrip failed: {type.Name}");
        Require(restored.EnergyCost.GetWithModifiers(CostModifiers.None) == card.EnergyCost.GetWithModifiers(CostModifiers.None), $"Upgrade cost reload failed: {type.Name}");
    }
    models.Add(card);
}
var relics = types.Where(t => !t.IsAbstract && (t.IsSubclassOf(typeof(BoosterRelic)) || t.IsSubclassOf(typeof(ComboRelic)))).ToArray();
Require(relics.Length == 11, "Expected six packs and five combo relics.");
foreach (var type in relics)
{
    ModelDb.Inject(type);
    SavedPropertiesTypeCache.InjectTypeIntoCache(type);
    var canonical = ModelDb.GetById<RelicModel>(ModelDb.GetId(type));
    var relic = canonical.ToMutable();
    if (relic is BoosterRelic pack)
    {
        pack.Contents = models.Take(5).Select(c => c.ToSerializable()).ToList();
        var saved = Roundtrip(pack);
        var restored = (BoosterRelic)canonical.ToMutable();
        saved!.Fill(restored);
        Require(!restored.Opened && restored.Contents.Count == 5 && restored.Contents[0].Id == pack.Contents[0].Id, "Pending pack save roundtrip failed.");
        pack.Opened = true;
        pack.Contents = [];
        Roundtrip(pack).Fill(restored);
        Require(restored.Opened && restored.Contents.Count == 0, "Opened memento save roundtrip failed.");
    }
    if (relic is ComboRelic combo)
    {
        combo.Triggered = true; combo.SeenTypes = 3;
        var restored = (ComboRelic)canonical.ToMutable();
        Roundtrip(combo).Fill(restored);
        Require(restored.Triggered && restored.SeenTypes == 3, "Combo state save roundtrip failed.");
    }
}
Console.WriteLine("PASS: 36 card constructors/clones serialize using native model IDs; all 16 player upgrades change effects/costs, preserve canonical values and reload through native card JSON; Borrowed Breath+ heals 6 and draws 3; 11 relic constructors and native SavedProperties JSON roundtrips for pending packs, opened mementos and combo counters. No full game registration, gameplay, UI or network test.");

var catalog = types.Single(t => t.Name == "GoblinPotionCatalog");
var supported = (Type[])catalog.GetField("Supported", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
var excluded = (Type[])catalog.GetField("Excluded", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
var nativePotions = typeof(PotionModel).Assembly.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(PotionModel)) && t.Namespace == typeof(StrengthPotion).Namespace).ToArray();
Require(supported.Length == 58 && excluded.Length == 6, "Expected 58 splashable potions and 6 explicit exclusions.");
Require(supported.Concat(excluded).Distinct().Count() == 64 && nativePotions.ToHashSet().SetEquals(supported.Concat(excluded)), "Native potion coverage changed; audit new effects before admitting them: " + string.Join(", ", nativePotions.Except(supported.Concat(excluded)).Select(t => t.FullName)));
foreach (var type in supported)
{
    var potion = ModelDb.GetById<PotionModel>(ModelDb.GetId(type));
    Require(potion.ToMutable().Id == potion.Id, "Potion identity: " + type.Name);
    Require(type.GetMethod("OnUse", BindingFlags.NonPublic | BindingFlags.Instance) != null, "Missing native potion effect.");
}
Console.WriteLine("PASS: all 64 native potion models classified (58 supported, 6 excluded); native clones and effect entrypoints resolve.");

foreach (var (type, expected) in new (Type, (string Move, decimal Damage, int Hits)[])[]
{
    (typeof(CrownBulwark), [("STRIKE", 18, 2), ("RENEW", 36, 1)]),
    (typeof(CrownHeadsman), [("STRIKE", 20, 2), ("RENEW", 36, 1)]),
    (typeof(CrownArmorer), [("STRIKE", 16, 1)]),
    (typeof(CrownHerald), [("STRIKE", 10, 2)])
})
{
    ModelDb.Inject(type);
    var knight = ModelDb.GetById<MonsterModel>(ModelDb.GetId(type)).ToMutable();
    knight.SetUpForCombat();
    var states = knight.MoveStateMachine!.States;
    Require(states.Count == 3, "Each guard keeps its three-turn cycle.");
    foreach (var (move, damage, hits) in expected)
    {
        var intent = ((MoveState)states[move]).Intents.OfType<AttackIntent>().Single();
        Require(intent.DamageCalc!() == damage && intent.Repeats == hits, "Kingsguard damage announcement mismatch: " + type.Name + "/" + move);
    }
}
Console.WriteLine("PASS: six Kingsguard attack intents announce doubled damage with unchanged hit counts; native three-turn state machines construct.");

