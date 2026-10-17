using System.Text.Json;
using LaRata;
using MegaCrit.Sts2.Core.Random;

var distribution = new Dictionary<string, int>();
var observedKinds = new HashSet<TwistKind>();
var reinforcementCounts = new int[4];
var normalCounts = new int[4];
int cases = 0;
int largestCoopPlan = 0;
bool mixedReinforcement = false, spread = false;
for (uint seed = 0; seed < 5000; seed++)
for (int players = 1; players <= 4; players++)
for (int tier = 0; tier < 3; tier++)
for (int enemies = 1; enemies <= 4; enemies++)
{
    EncounterPlan Roll()
    {
        var rng = new Rng(seed, $"LaRata/test/{players}/{tier}/{enemies}");
        return EncounterPlan.Roll(n => rng.NextInt(n), players, tier, tier == 0 ? 2 : 0, enemies);
    }
    var first = Roll();
    Require(JsonSerializer.Serialize(first) == JsonSerializer.Serialize(Roll()), "seed replay differs");
    Require(first.Spent > 0 && first.Spent <= first.Budget, "budget exceeded or empty");
    Require(first.Budget == EncounterPlan.BaseBudget(1, tier) * 5 * players, "party budget is not linear");
    Require(first.Spent == first.ReinforcementCount * 20 + first.Twists.Sum(t => EncounterPlan.Cost(t.Kind)), "cost accounting differs");
    Require(first.Affixes.Length <= 3 * players, "affix cap exceeded");
    Require(first.ReinforcementCount <= (players >= 3 ? 2 : 1), "too many reinforcements");
    Require(enemies + first.ReinforcementCount <= 4, "formation exceeds four starting enemies");
    Require(tier == 0 || first.ReinforcementCount == 0, "reinforced elite/boss");
    Require(first.Twists.All(t => t.Target >= 0 && t.Target < enemies + first.ReinforcementCount), "invalid target");
    Require(first.Twists.Select(t => t.Kind).Distinct().Count() == first.Twists.Length, "duplicate twist family");
    Require(first.Twists.Count(t => EncounterPlan.IsOffense(t.Kind)) <= 1, "stacked offense amplifiers");
    Require(first.Twists.Count(t => t.IsHex) <= (players >= 3 ? 2 : 1), "hex pressure exceeded");
    foreach (var group in first.Twists.GroupBy(t => t.Target))
    {
        Require(group.Count(t => t.IsHex) <= 1, "multiple hexes on one monster");
        Require(group.Count(t => EncounterPlan.IsShield(t.Kind)) <= 1, "stacked damage prevention");
        Require(group.Count(t => EncounterPlan.IsBlock(t.Kind)) <= 1, "stacked block engines");
    }
    Require(first.Twists.Where(t => t.Kind == TwistKind.Sturdy).All(t => t.Amount is >= 10 and <= 18), "HP cap exceeded");
    Require(first.Twists.Where(t => t.Kind == TwistKind.Keen).All(t => t.Amount is >= 8 and <= 16), "damage cap exceeded");
    Require(first.Twists.Length == 0 || first.Twists[0].Kind is not (TwistKind.Sturdy or TwistKind.Keen), "stats-only first choice");
    foreach (var twist in first.Twists)
    {
        observedKinds.Add(twist.Kind);
        distribution[twist.Kind.ToString()] = distribution.GetValueOrDefault(twist.Kind.ToString()) + 1;
    }
    if (players == 4)
    {
        largestCoopPlan = Math.Max(largestCoopPlan, first.Affixes.Length);
        mixedReinforcement |= first.ReinforcementCount > 0 && first.Twists.Length > 0;
        spread |= first.Twists.Select(t => t.Target).Distinct().Count() >= 3;
    }
    if (tier == 0 && enemies == 1)
    {
        normalCounts[players - 1]++;
        if (first.ReinforcementCount > 0) reinforcementCounts[players - 1]++;
    }
    cases++;
}
Require(observedKinds.Count == Enum.GetValues<TwistKind>().Length, "unreachable twist family");
Require(largestCoopPlan >= 7 && mixedReinforcement && spread, "co-op diversity/spreading missing");
for (int players = 1; players <= 4; players++)
{
    double rate = (double)reinforcementCounts[players - 1] / normalCounts[players - 1];
    Require(Math.Abs(rate - (.5 + (players - 1) * .15)) < .04, "reinforcement frequency outside tolerance");
    for (uint seed = 0; seed < 100; seed++)
    {
        var rng = new Rng(seed);
        var rat = EncounterPlan.Roll(n => rng.NextInt(n), players, 0, 1, 3, true);
        Require(rat.ReinforcementCount == 1, "forced rat missing or extra slot used");
        Require(players != 1 || rat.Spent == rat.Budget && rat.Twists.Length == 0, "solo reinforcement budget changed");
        var fixedFormation = EncounterPlan.Roll(n => rng.NextInt(n), players, 0, 0, 2, true);
        Require(fixedFormation.ReinforcementCount == 0, "forced reinforcement bypasses formation eligibility");
    }
}
Console.WriteLine($"PASS: {cases} native-Rng policies; replay, exact linear budgets, costs, target indices, formation limits, exclusions, 30-family reachability and co-op diversity.");
Console.WriteLine(JsonSerializer.Serialize(new { distribution, reinforcementCounts, normalCounts, largestCoopPlan }));
var playedCards = new HashSet<int>();
for (uint seed = 0; seed < 1000; seed++)
{
    var aRng = new Rng(seed); var bRng = new Rng(seed);
    var a = new RoyalDeck(n => aRng.NextInt(n)); var b = new RoyalDeck(n => bRng.NextInt(n));
    var drawnCards = new HashSet<int>();
    for (int turn = 0; turn < 40; turn++)
    {
        int energy = turn == 0 ? 4 : turn < 20 ? 3 : 4;
        a.Prepare(energy); b.Prepare(energy);
        Require(a.Snapshot == b.Snapshot, "royal deck replay differs");
        Require(a.Hand.Count == 3 && a.Hand.Distinct().Count() == 3, "invalid draw");
        drawnCards.UnionWith(a.Hand);
        Require(a.Planned.Length > 0 && a.Planned.Sum(i => RoyalDeck.Costs[i]) <= energy, "royal overspend or empty plan");
        Require(a.Hand.Count + a.DrawCount + a.DiscardCount == 10, "royal cards lost or duplicated");
        Require(a.Planned.SkipWhile(i => !RoyalDeck.IsAttack(i)).All(RoyalDeck.IsAttack), "spell scheduled after an attack");
        foreach (int card in a.Planned.ToArray())
        {
            playedCards.Add(card); a.Spend(card); b.Spend(card);
            Require(a.Energy >= 0 && a.Snapshot == b.Snapshot, "invalid card payment");
        }
        a.Finish(); b.Finish();
        Require(a.DrawCount + a.DiscardCount == 10 && a.Hand.Count == 0, "discard invariant failed");
    }
    Require(drawnCards.Count == 10, "royal deck cannot draw every card");
}
Require(playedCards.Count == 10, "royal planning excludes a card");
Console.WriteLine("PASS: 40000 royal turns; deterministic draws, reshuffles, card conservation and energy limits.");
var potionsSeen = new HashSet<int>();
var relicsSeen = new HashSet<int>();
for (uint seed = 0; seed < 1000; seed++)
{
    for (int block = 0; block < 40; block++)
    {
        int offset = new Rng(seed, $"LaRata/goblin/v1/block/{block}").NextInt(3);
        Require(Enumerable.Range(block * 3 + 1, 3).Count(n => ChaosSelections.GoblinVisits(n, offset)) == 1, "not exactly one goblin visit per combat block");
        int ordinal = block * 3 + 1 + offset;
        var a = new Rng(seed, $"LaRata/goblin/v1/potions/{ordinal}");
        var b = new Rng(seed, $"LaRata/goblin/v1/potions/{ordinal}");
        var picks = ChaosSelections.Distinct(4, 12, n => a.NextInt(n));
        Require(picks.Distinct().Count() == 4 && picks.All(i => i >= 0 && i < 12), "duplicate/out-of-range co-op potions");
        Require(picks.SequenceEqual(ChaosSelections.Distinct(4, 12, n => b.NextInt(n))), "goblin reload replay differs");
        potionsSeen.UnionWith(picks);
    }
    var relicRng = new Rng(seed, "royal-relics");
    var relicReplay = new Rng(seed, "royal-relics");
    var relics = ChaosSelections.Distinct(5, 9, n => relicRng.NextInt(n));
    Require(relics.Distinct().Count() == 5 && relics.All(i => i >= 0 && i < 9), "invalid regalia selection");
    Require(relics.SequenceEqual(ChaosSelections.Distinct(5, 9, n => relicReplay.NextInt(n))), "regalia replay differs");
    int mask = relics.Aggregate(0, (bits, i) => bits | (1 << i));
    Require(System.Numerics.BitOperations.PopCount((uint)mask) == 5, "regalia checksum mask loses a relic");
    relicsSeen.UnionWith(relics);
}
Require(potionsSeen.Count == 12 && relicsSeen.Count == 9, "selection pool has unreachable entries");
Console.WriteLine("PASS: 40000 goblin blocks, 160000 distinct co-op potion assignments, 1000 five-relic selections; native-Rng replay and spell-before-attack ordering.");
static void Require(bool condition, string reason) { if (!condition) throw new Exception(reason); }
