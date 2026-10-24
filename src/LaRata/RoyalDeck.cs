namespace LaRata;

// Enemy-owned deck state; no fake player, player RNG draws, or unsynchronized UI choices.
// Native saves restart combat, so this is reconstructed from the encounter seed on load.
internal sealed class RoyalDeck(Func<int, int> next)
{
    internal static readonly int[] Costs = [1, 1, 3, 2, 2, 2, 2, 0, 0, 2, 2, 2, 1, 3, 2, 1, 2, 3, 2, 1, 2, 3, 2, 3, 3];
    // Preserve catalog IDs for existing card models, history and player prizes.
    // Sewer Alchemy (3) and Plague of Crowns (21) no longer enter the boss deck.
    internal static readonly int[] ActiveCards = Enumerable.Range(0, Costs.Length).Where(card => card is not (3 or 21)).ToArray();
    private readonly List<int> draw = ActiveCards.ToList();
    private readonly List<int> discard = [];
    private readonly List<int> hand = [];
    private bool shuffled;
    internal int Energy { get; private set; }
    internal int Capacity { get; private set; }
    internal int DrawCount => draw.Count;
    internal int DiscardCount => discard.Count;
    internal IReadOnlyList<int> Hand => hand;
    internal int[] Planned { get; private set; } = [];

    internal void Prepare(int energy, int cardsToDraw = 5)
    {
        if (hand.Count != 0) throw new InvalidOperationException("Finish the royal turn before drawing again.");
        if (!shuffled) { Shuffle(); shuffled = true; }
        Energy = Capacity = energy;
        for (int i = 0; i < cardsToDraw; i++)
        {
            if (draw.Count == 0) { draw.AddRange(discard); discard.Clear(); Shuffle(); }
            if (draw.Count == 0) break;
            hand.Add(draw[^1]);
            draw.RemoveAt(draw.Count - 1);
        }
        int available = energy;
        var planned = new List<int>();
        foreach (int card in hand.OrderByDescending(card => Costs[card]))
        {
            if (Costs[card] > available) continue;
            available -= Costs[card];
            planned.Add(card);
        }
        // Cast skills/powers first, then execute ordinary monster attacks.
        Planned = planned.OrderBy(card => IsAttack(card) ? 1 : 0).ToArray();
    }

    internal void Spend(int card)
    {
        if (!hand.Contains(card) || !Planned.Contains(card) || Energy < Costs[card])
            throw new InvalidOperationException("Invalid royal card play.");
        Energy -= Costs[card];
        hand.Remove(card);
        discard.Add(card);
    }

    internal void Finish() { discard.AddRange(hand); hand.Clear(); Planned = []; }
    internal string Snapshot => $"energy={Energy}/{Capacity};draw={string.Join(',', draw)};hand={string.Join(',', hand)};discard={string.Join(',', discard)};planned={string.Join(',', Planned)}";
    internal static bool IsAttack(int card) => card is 0 or 1 or 2 or 9 or 10 or 11 or 17 or 18 or 23;
    private void Shuffle()
    {
        for (int i = draw.Count - 1; i > 0; i--)
        {
            int j = next(i + 1);
            (draw[i], draw[j]) = (draw[j], draw[i]);
        }
    }
}
