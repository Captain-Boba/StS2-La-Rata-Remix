using LaRata;
using MegaCrit.Sts2.Core.Random;

internal static class RoyalDeckWaveChecks
{
    private static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }
    private static int Count(RoyalDeck deck) => deck.DrawCount + deck.DiscardCount + deck.Hand.Count + deck.PlayCount + deck.ExhaustCount + deck.PersistentCount;
    internal static void Run()
    {
        foreach (decimal raw in new decimal[] { 0, 1, 3, 5, 12, 18, 25 })
        {
            Require(RoyalBalance.CardBlock(raw, false) == raw && RoyalBalance.CardBlock(raw, true) == raw / 2,
                "Block scope must reduce raw boss-card base only, before native rounding");
        }
        foreach (var (raw, expected) in new (decimal, decimal)[] { (0, 0), (1, 1), (3, 1), (4, 1), (7, 1), (8, 2), (13, 3), (25, 6) })
            Require(RoyalBalance.ReducedHealing(raw) == expected, "Boss healing floor/minimum differs");
        foreach (var (hp, quarter, tenth) in new (int, int, int)[] { (1500, 93, 37), (3900, 243, 97), (5850, 365, 146), (7800, 487, 195) })
        {
            Require(RoyalBalance.GuardHealing(hp, .25m, true) == quarter && RoyalBalance.GuardHealing(hp, .10m, true) == tenth,
                "Knight-to-La-Rata healing differs at party-scaled HP");
            Require(RoyalBalance.GuardHealing(hp, .25m, false) == Math.Ceiling(hp * .25m) &&
                RoyalBalance.GuardHealing(hp, .10m, false) == Math.Ceiling(hp * .10m), "Knight-to-knight healing was reduced");
        }
        Require(RoyalDeck.Costs.Length == 48 && RoyalDeck.ActiveCards.Length == 46 &&
            !RoyalDeck.ActiveCards.Contains(3) && !RoyalDeck.ActiveCards.Contains(21), "Boss catalog/exclusions differ");
        ResetPaymentBoundaries();
        var observed = new HashSet<int>();
        for (uint seed = 0; seed < 16; seed++)
        {
            var ar = new Rng(seed); var br = new Rng(seed);
            var a = new RoyalDeck(n => ar.NextInt(n)); var b = new RoyalDeck(n => br.NextInt(n));
            Require(a.ReserveNext(10, 10) == (3, 2) && b.ReserveNext(10, 10) == (3, 2), "Card resource caps differ");
            Require(a.ReserveNext(1, 1) == (0, 0) && b.ReserveNext(1, 1) == (0, 0), "Card resource caps can be bypassed");
            for (int turn = 0; turn < 12; turn++)
            {
                a.Prepare(8, turn == 0 ? 9 : 5); b.Prepare(8, turn == 0 ? 9 : 5);
                if (turn == 0) Require(a.Capacity == 10 && a.Hand.Count == 10, "Next-hand bonuses or hand limit differ");
                Require(a.Snapshot == b.Snapshot && ar.Counter == br.Counter, "Hidden deck/plan/RNG replay differs");
                Require(Count(a) == 46 && a.Hand.Distinct().Count() == a.Hand.Count && a.Planned.Length <= 8, "Card conservation/hand/play bound broken");
                Require(a.Planned.Sum(i => RoyalDeck.Costs[i]) <= a.Capacity, "Plan overspends energy");
                Require(a.Planned.SkipWhile(i => !RoyalDeck.IsAttack(i)).All(RoyalDeck.IsAttack), "Skill/power scheduled after attack");
                if (a.Planned.Contains(47)) Require(a.Planned.SkipWhile(i => i != 47).Skip(1).All(RoyalDeck.IsAttack), "Reset must follow other support cards and precede attacks");
                foreach (int card in a.Planned.ToArray())
                {
                    observed.Add(card);
                    a.Spend(card); b.Spend(card);
                    Require(a.PlayCount == 1 && !a.Hand.Contains(card) && Count(a) == 46, "Resolving card lost or prematurely discarded");
                    if (a.HasMaterial(card))
                    {
                        var victim = a.UseMaterial(card, card is 39 or 42);
                        Require(victim == b.UseMaterial(card, card is 39 or 42) && victim != card && !a.Planned.Contains(victim!.Value), "Material consumes an announced play or differs in replay");
                    }
                    int currentEnergy = a.Energy, currentHand = a.Hand.Count;
                    if (card == 47)
                    {
                        Require(a.ReserveReset() == b.ReserveReset(), "Reset replay differs");
                        Require(a.Energy == currentEnergy && a.Hand.Count == currentHand, "Reset changes current hand resources");
                    }
                    if (card is 26 or 30 or 31 or 32 or 33 or 36 or 41 or 43)
                    {
                        a.ReserveNext(2, 1); b.ReserveNext(2, 1);
                        Require(a.Energy == currentEnergy && a.Hand.Count == currentHand, "Next-hand resources affect current hand");
                    }
                    a.Complete(card); b.Complete(card);
                    Require(Count(a) == 46 && a.Snapshot == b.Snapshot, "Completion loses or duplicates a card");
                    if (RoyalDeck.IsPersistent(card)) Require(a.Persistent.Contains(card), "Power returns to cycling piles");
                    else if (RoyalDeck.IsExhaust(card)) Require(a.Exhaust.Contains(card), "Exhaust card returns to cycling piles");
                }
                a.Finish(); b.Finish();
                Require(Count(a) == 46 && a.Hand.Count == 0 && a.PlayCount == 0 && a.Snapshot == b.Snapshot, "Finish/replay invariant broken");
            }
        }
        Require(observed.Contains(36) && observed.Any(RoyalDeck.IsPersistent) && observed.Any(RoyalDeck.RequiresMaterial), "Directed seeds miss key new deck mechanisms");
        Console.WriteLine("PASS: 192 directed royal-deck turns preserve all 46 instances including resolving/Exhaust/persistent piles, exact hidden-state/RNG replay, material safety, next-hand caps and bounded planning. Policy simulation only; no live monster execution.");
    }

    private static void ResetPaymentBoundaries()
    {
        int cases = 0;
        foreach (int paid in new[] { 0, 1, 2, 4 })
        foreach (int bank in new[] { 0, 1, 2 })
        foreach (int relicBank in new[] { 0, 3 })
        {
            RoyalDeck? deck = null; int[] payments = [];
            // Find a real deterministic hand with a suitable subset; no private-state injection.
            for (uint seed = 0; seed < 256 && deck == null; seed++)
            {
                var rng = new Rng(seed); var candidate = new RoyalDeck(n => rng.NextInt(n));
                candidate.Prepare(30, 10);
                if (!candidate.Planned.Contains(47)) continue;
                var available = candidate.Planned.Where(i => i != 47).ToArray();
                for (int mask = 0; mask < (1 << available.Length); mask++)
                {
                    var chosen = available.Where((_, i) => (mask & (1 << i)) != 0).ToArray();
                    if (chosen.Sum(i => RoyalDeck.Costs[i]) != paid) continue;
                    deck = candidate; payments = chosen; break;
                }
            }
            Require(deck != null, "Directed seeds could not construct Reset payment boundary");
            var d = deck!;
            foreach (int card in payments) { d.Spend(card); d.Complete(card); }
            d.ReserveNext(0, bank); d.ReserveNext(0, relicBank, relic: true);
            int energyBefore = d.Energy; d.Spend(47);
            int grant = Math.Min(paid, 2 - bank);
            Require(d.ReserveReset() == (paid, grant, paid - grant), "Reset eligibility or shared card cap differs");
            Require(d.ReserveReset() == (paid - grant, 0, paid - grant), "Reset double-refunds or loses capped eligibility");
            Require(d.Energy == energyBefore - 2 && d.NextEnergy == bank + relicBank + grant, "Reset refunds itself or crosses relic bank/current energy");
            Require(d.Snapshot.Contains($"resetLedger={paid}/{grant};"), "Reset snapshot does not preserve actual granted amount");
            bool replayRejected = false;
            try { d.Spend(47); } catch (InvalidOperationException) { replayRejected = true; }
            Require(replayRejected, "Duplicate Reset payment accepted");
            d.Complete(47); d.Finish();
            Require(d.Exhaust.Contains(47), "Boss Reset is not exhausted");
            d.Prepare(8);
            Require(d.Capacity == 8 + bank + relicBank + grant && d.NextEnergy == 0 && d.Snapshot.Contains("resetLedger=0/0;"), "Reset next-hand consume/reset differs");
            cases++;
        }
        Console.WriteLine($"PASS: {cases} Reset payment-boundary cases, real generated hands, actual Spend/Reserve/Complete transitions; shared card cap, separate relic bank, repeat prevention and next-hand reset. Policy only, not native effect/network/save execution.");
    }
}
