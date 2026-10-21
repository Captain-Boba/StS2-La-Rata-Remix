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
for (uint seed = 0; seed < 2000; seed++)
for (int act = 0; act < 4; act++)
for (int players = 1; players <= 4; players++)
for (int tier = 0; tier < 3; tier++)
for (int enemies = 1; enemies <= 4; enemies++)
{
    EncounterPlan Roll()
    {
        var rng = new Rng(seed, $"LaRata/test/{players}/{tier}/{enemies}");
        return EncounterPlan.Roll(n => rng.NextInt(n), players, tier, tier == 0 ? 3 : 0, enemies, actIndex: act);
    }
    var first = Roll();
    Require(JsonSerializer.Serialize(first) == JsonSerializer.Serialize(Roll()), "seed replay differs");
    Require(first.Spent >= 0 && first.Spent <= first.Budget, "budget exceeded or empty");
    Require(first.Budget == 3 * EncounterPlan.BaseBudget(players, tier) * (5 + 2 * act), "budget is not exactly 75% of the old quarter-unit budget");
    Require(first.Twists.All(t => EncounterPlan.ActiveKinds.Contains(t.Kind)), "retired twist rolled");
    Require(first.Spent > 0, "unexpected empty encounter plan");
    Require(first.Budget == EncounterPlan.StageBudget(1, tier, act) * players, "party budget is not linear");
    Require(act == 0 || first.Budget > EncounterPlan.StageBudget(players, tier, act - 1), "act budget did not grow");
    Require(first.Spent == first.ReinforcementCount * first.HelperCost + first.Twists.Sum(EncounterPlan.Cost), "cost accounting differs");
    Require(first.Twists.All(t => !t.Lesser || (first.Budget < 96 && EncounterPlan.HasLesser(t.Kind))), "lesser variant outside its budget/family");
    Require(!first.ScoutReinforcement || (first.Budget < 64 && tier == 0 && first.ReinforcementCount == 1), "scout outside small ordinary encounter");
    if (players == 4) Require(!first.ScoutReinforcement && first.Twists.All(t => !t.Lesser), "four-player pricing changed");
    if (players == 1 && tier == 0 && act == 0)
        Require(first.ReinforcementCount + first.Twists.Count(t => t.Kind is not (TwistKind.Sturdy or TwistKind.Keen)) >= 2, "solo opener lacks two tactical changes");
    Require(first.Affixes.Length <= 3 * players + act, "affix cap exceeded");
    Require(first.ReinforcementCount <= EncounterPlan.ReinforcementLimit(players, act), "too many reinforcements");
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
        Require(group.Count(t => EncounterPlan.IsRetaliation(t.Kind)) <= 1, "stacked retaliation");
    }
    Require(first.Twists.Where(t => t.Kind == TwistKind.Sturdy).All(t => t.Amount >= 10 + 4 * act && t.Amount <= 18 + 4 * act), "HP cap exceeded");
    Require(first.Twists.Where(t => t.Kind == TwistKind.Keen).All(t => t.Amount >= 8 + 2 * act && t.Amount <= 16 + 2 * act), "damage cap exceeded");
    Require(first.Twists.All(t => t.ActIndex == act), "twist stage was lost");
    Require(!first.Twists.Any(t => t.Kind == TwistKind.BloodFeud) || enemies + first.ReinforcementCount > 1, "blood feud without an ally");
    Require(!first.Twists.Any(t => t.Kind == TwistKind.SkillHunter) || act > 0 && tier == 0, "skill hunter on early/elite/boss encounter");
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
    if (tier == 0 && enemies == 1 && act == 0)
    {
        normalCounts[players - 1]++;
        if (first.ReinforcementCount > 0) reinforcementCounts[players - 1]++;
    }
    cases++;
}
Require(observedKinds.SetEquals(EncounterPlan.ActiveKinds), "unreachable twist family");
Require(largestCoopPlan >= 7 && mixedReinforcement && spread, "co-op diversity/spreading missing");
for (int players = 1; players <= 4; players++)
{
    double rate = (double)reinforcementCounts[players - 1] / normalCounts[players - 1];
    Require(Math.Abs(rate - EncounterPlan.ReinforcementChance(players, 0) / 100.0) < .04, "reinforcement frequency outside tolerance");
    for (uint seed = 0; seed < 100; seed++)
    {
        var rng = new Rng(seed);
        var rat = EncounterPlan.Roll(n => rng.NextInt(n), players, 0, 1, 3, true);
        Require(rat.ReinforcementCount == 1, "forced rat missing or extra slot used");
        Require(rat.Spent <= rat.Budget, "forced reinforcement overspent");
        var fixedFormation = EncounterPlan.Roll(n => rng.NextInt(n), players, 0, 0, 2, true);
        Require(fixedFormation.ReinforcementCount == 0, "forced reinforcement bypasses formation eligibility");
    }
}
Require(new EnemyTwist(TwistKind.Fortified, 0, 4, 0).Defense(4) == 4 && new EnemyTwist(TwistKind.Fortified, 0, 4, 1).Defense(4) == 6 && new EnemyTwist(TwistKind.Fortified, 0, 4, 2).Defense(4) == 8, "Block progression differs");
Console.WriteLine($"PASS: {cases} native-Rng policies across four acts; replay, party-linear/stage-growing budgets, costs, target indices, formations, exclusions, {observedKinds.Count}-family reachability and co-op diversity.");
Console.WriteLine(JsonSerializer.Serialize(new { distribution, reinforcementCounts, normalCounts, largestCoopPlan }));
var playedCards = new HashSet<int>();
for (uint seed = 0; seed < 1000; seed++)
{
    var aRng = new Rng(seed); var bRng = new Rng(seed);
    var a = new RoyalDeck(n => aRng.NextInt(n)); var b = new RoyalDeck(n => bRng.NextInt(n));
    var drawnCards = new HashSet<int>();
    for (int turn = 0; turn < 40; turn++)
    {
        int energy = (turn < 20 ? 6 : 8) + (turn % 3 == 2 || turn == 0 ? 1 : 0);
        int draw = turn == 0 ? 7 : 5;
        a.Prepare(energy, draw); b.Prepare(energy, draw);
        Require(a.Snapshot == b.Snapshot, "royal deck replay differs");
        Require(a.Hand.Count == draw && a.Hand.Distinct().Count() == draw, "invalid draw");
        drawnCards.UnionWith(a.Hand);
        Require(a.Planned.Length > 0 && a.Planned.Sum(i => RoyalDeck.Costs[i]) <= energy, "royal overspend or empty plan");
        Require(a.Hand.Count + a.DrawCount + a.DiscardCount == RoyalDeck.Costs.Length, "royal cards lost or duplicated");
        Require(a.Planned.SkipWhile(i => !RoyalDeck.IsAttack(i)).All(RoyalDeck.IsAttack), "spell scheduled after an attack");
        foreach (int card in a.Planned.ToArray())
        {
            playedCards.Add(card); a.Spend(card); b.Spend(card);
            Require(a.Energy >= 0 && a.Snapshot == b.Snapshot, "invalid card payment");
        }
        a.Finish(); b.Finish();
        Require(a.DrawCount + a.DiscardCount == RoyalDeck.Costs.Length && a.Hand.Count == 0, "discard invariant failed");
    }
    Require(drawnCards.Count == RoyalDeck.Costs.Length, "royal deck cannot draw every card");
}
Require(playedCards.Count == RoyalDeck.Costs.Length, "royal planning excludes a card");
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
    var relics = ChaosSelections.Distinct(10, 14, n => relicRng.NextInt(n));
    Require(relics.Distinct().Count() == 10 && relics.All(i => i >= 0 && i < 14), "invalid regalia selection");
    Require(relics.SequenceEqual(ChaosSelections.Distinct(10, 14, n => relicReplay.NextInt(n))), "regalia replay differs");
    int mask = relics.Aggregate(0, (bits, i) => bits | (1 << i));
    Require(System.Numerics.BitOperations.PopCount((uint)mask) == 10, "regalia checksum mask loses a relic");
    relicsSeen.UnionWith(relics);
}
Require(potionsSeen.Count == 12 && relicsSeen.Count == 14, "selection pool has unreachable entries");
Console.WriteLine("PASS: 40000 goblin blocks, 160000 distinct co-op potion assignments, 1000 ten-relic selections; native-Rng replay and spell-before-attack ordering.");
int royalVisits = 0;
for (uint seed = 0; seed < 1000; seed++)
{
    for (int block = 0; block < 40; block++)
    {
        int visits = 0;
        for (int offset = 1; offset <= 3; offset++)
        {
            int eligible = block * 3 + offset;
            // Simulate bosses/event fights interspersed between eligible rooms.
            int ordinal = eligible + eligible / 7 + (int)(seed % 4);
            int? round = CameoSchedule.RoyalRound(seed, eligible, ordinal);
            Require(round == CameoSchedule.RoyalRound(seed, eligible, ordinal), "royal cameo reload differs");
            Require(round == null || round is 2 or 3, "royal cameo outside its warning window");
            if (round != null)
            {
                visits++; royalVisits++;
                Require(round != CameoSchedule.GoblinRound(seed, ordinal), "royal cameo collides with goblin");
                foreach (int card in CameoSchedule.RoyalPool)
                {
                    int value = CameoSchedule.Encode(round.Value, card);
                    Require(CameoSchedule.Card(value) == card && CameoSchedule.Round(value) == round, "hidden power encoding loses state");
                    Require(!RoyalDeck.IsAttack(card) && card != 4 && card != 7, "surprise damage/Doom/Strength in intervention pool");
                }
            }
            int originalOffset = new Rng(seed, $"LaRata/goblin/v1/block/{(ordinal - 1) / 3}").NextInt(3);
            int? originalRound = ChaosSelections.GoblinVisits(ordinal, originalOffset)
                ? 3 + new Rng(seed, $"LaRata/goblin/v1/arrival/{ordinal}").NextInt(2) : null;
            Require(CameoSchedule.GoblinRound(seed, ordinal) == originalRound, "existing goblin schedule changed");
        }
        Require(visits == 1, "royal cameo frequency not exactly one scheduled visit per eligible block");
    }
}
Require(CameoSchedule.RoyalRound(1, 0, 0) == null && CameoSchedule.GoblinRound(1, 0) == null, "invalid combat scheduled");
Console.WriteLine($"PASS: 120000 cameo schedules, {royalVisits} royal visits; no goblin overlap, unchanged goblin timing, hidden state round-trips and safe card pool. These are policy checks, not network/gameplay tests.");
static void Require(bool condition, string reason) { if (!condition) throw new Exception(reason); }
