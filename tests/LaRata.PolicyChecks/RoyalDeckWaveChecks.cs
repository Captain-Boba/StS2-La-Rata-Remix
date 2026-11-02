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
        Require(RoyalDeck.Costs.Length == 48 && RoyalDeck.ActiveCards.Length == 41 &&
            RoyalDeck.ActiveCards.SequenceEqual(Enumerable.Range(0, 48).Except(new[] { 3, 20, 21, 25, 26, 28, 47 })), "Boss catalog/exclusions differ");
        Require(RoyalDeck.ResetCard == 47 && !RoyalDeck.ActiveCards.Contains(RoyalDeck.ResetCard), "Reset identity moved or reentered the active deck");
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
                Require(Count(a) == 41 && a.Hand.Distinct().Count() == a.Hand.Count && a.Planned.Length <= 8, "Card conservation/hand/play bound broken");
                Require(a.Hand.All(RoyalDeck.ActiveCards.Contains), "Excluded card returned on draw or reshuffle");
                Require(a.Planned.Sum(i => RoyalDeck.Costs[i]) <= a.Capacity, "Plan overspends energy");
                Require(a.Planned.SkipWhile(i => !RoyalDeck.IsAttack(i)).All(RoyalDeck.IsAttack), "Skill/power scheduled after attack");
                Require(!a.Planned.Contains(RoyalDeck.ResetCard), "Retired Reset entered the active plan");
                foreach (int card in a.Planned.ToArray())
                {
                    observed.Add(card);
                    a.Spend(card); b.Spend(card);
                    Require(a.PlayCount == 1 && !a.Hand.Contains(card) && Count(a) == 41, "Resolving card lost or prematurely discarded");
                    if (a.HasMaterial(card))
                    {
                        var victim = a.UseMaterial(card, card is 39 or 42);
                        Require(victim == b.UseMaterial(card, card is 39 or 42) && victim != card && !a.Planned.Contains(victim!.Value), "Material consumes an announced play or differs in replay");
                    }
                    int currentEnergy = a.Energy, currentHand = a.Hand.Count;
                    if (card is 26 or 30 or 31 or 32 or 33 or 36 or 41 or 43)
                    {
                        a.ReserveNext(2, 1); b.ReserveNext(2, 1);
                        Require(a.Energy == currentEnergy && a.Hand.Count == currentHand, "Next-hand resources affect current hand");
                    }
                    a.Complete(card); b.Complete(card);
                    Require(Count(a) == 41 && a.Snapshot == b.Snapshot, "Completion loses or duplicates a card");
                    if (RoyalDeck.IsPersistent(card)) Require(a.Persistent.Contains(card), "Power returns to cycling piles");
                    else if (RoyalDeck.IsExhaust(card)) Require(a.Exhaust.Contains(card), "Exhaust card returns to cycling piles");
                }
                a.Finish(); b.Finish();
                Require(Count(a) == 41 && a.Hand.Count == 0 && a.PlayCount == 0 && a.Snapshot == b.Snapshot, "Finish/replay invariant broken");
            }
        }
        Require(observed.Contains(36) && observed.Any(RoyalDeck.IsPersistent) && observed.Any(RoyalDeck.RequiresMaterial), "Directed seeds miss key new deck mechanisms");
        Console.WriteLine("PASS: 192 directed royal-deck turns preserve all 41 instances including resolving/Exhaust/persistent piles, exact hidden-state/RNG replay, material safety, next-hand caps and bounded planning. Policy simulation only; no live monster execution.");
    }

    // Reset retains its stable identity/adapter, but cannot be drawn from the active deck.
}
