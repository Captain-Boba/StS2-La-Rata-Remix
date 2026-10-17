using System.Text.Json;
using LaRata;
using MegaCrit.Sts2.Core.Random;

var distribution = new Dictionary<string, int>();
int cases = 0;
for (uint seed = 0; seed < 10000; seed++)
for (int players = 1; players <= 4; players++)
for (int tier = 0; tier < 3; tier++)
{
    EncounterPlan Roll()
    {
        var rng = new Rng(seed, $"LaRata/test/{players}/{tier}");
        return EncounterPlan.Roll(n => rng.NextInt(n), players, tier, tier == 0);
    }
    var first = Roll();
    Require(JsonSerializer.Serialize(first) == JsonSerializer.Serialize(Roll()), "seed replay differs");
    Require(first.Spent > 0 && first.Spent <= first.Budget, "budget exceeded or empty");
    Require(first.Affixes.Length is >= 1 and <= 3, "affix count outside limits");
    Require(first.HpPercent <= 12 && first.DamagePercent <= 12, "stat cap exceeded");
    Require(first.DamagePercent == 0 || first.BuffBonus == 0, "stacked offense amplifiers");
    Require(first.HexAmount <= 2 && first.DebuffBonus <= 1 && first.Artifact <= 1, "power cap exceeded");
    Require(!first.Reinforcement || (first.Affixes.Length == 1 && first.HpPercent == 0 && first.DamagePercent == 0 && first.Hex == 0), "reinforcement stacked with other modifiers");
    foreach (var name in first.Affixes.Select(x => x.Split(':')[0])) distribution[name] = distribution.GetValueOrDefault(name) + 1;
    cases++;
}
var forcedRng = new Rng(42);
var rat = EncounterPlan.Roll(n => forcedRng.NextInt(n), 4, 0, true, true);
Require(rat.Reinforcement && rat.Spent == rat.Budget, "rat consumes full budget");
Console.WriteLine($"PASS: {cases} seeded policies checked with the game's native Rng; replay, budgets, caps, and offensive exclusions.");
Console.WriteLine(JsonSerializer.Serialize(distribution));
static void Require(bool condition, string reason) { if (!condition) throw new Exception(reason); }
