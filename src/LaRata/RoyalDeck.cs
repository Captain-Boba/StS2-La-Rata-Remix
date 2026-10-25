namespace LaRata;

// One combat instance per family. Legacy slots never move; prizes are aliases.
// Native combat restart/replay reconstructs this state from encounter RNG.
internal sealed class RoyalDeck(Func<int, int> next)
{
    internal static readonly int[] Costs = [1, 1, 3, 2, 2, 2, 2, 0, 0, 2, 2, 2, 1, 3, 2, 1, 2, 3, 2, 1, 2, 3, 2, 3, 3,
        2, 0, 1, 1, 2, 1, 0, 1, 2, 2, 2, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2];
    internal static readonly int[] ActiveCards = Enumerable.Range(0, Costs.Length).Where(card => card is not (3 or 21)).ToArray();
    internal const int HandLimit = 10;
    internal const int PlayLimit = 8;
    internal const int ResetCard = 47;
    private readonly List<int> draw = ActiveCards.ToList();
    private readonly List<int> discard = [];
    private readonly List<int> hand = [];
    private readonly List<int> exhaust = [];
    private readonly List<int> persistent = [];
    private readonly Dictionary<int, int> sacrifices = [];
    private readonly Dictionary<int, string> skipped = [];
    private bool shuffled;
    private int? playing;
    private int nextCardDraw, nextCardEnergy, nextRelicDraw, nextRelicEnergy;
    private int refundableEnergySpent, resetEnergyRefunded;
    internal int Energy { get; private set; }
    internal int Capacity { get; private set; }
    internal int DrawnThisTurn { get; private set; }
    internal int NextDraw => nextCardDraw + nextRelicDraw;
    internal int NextEnergy => nextCardEnergy + nextRelicEnergy;
    internal int DrawCount => draw.Count;
    internal int DiscardCount => discard.Count;
    internal int ExhaustCount => exhaust.Count;
    internal int PersistentCount => persistent.Count;
    internal int PlayCount => playing.HasValue ? 1 : 0;
    internal IReadOnlyList<int> Hand => hand;
    internal IReadOnlyList<int> Exhaust => exhaust;
    internal IReadOnlyList<int> Persistent => persistent;
    internal IReadOnlyDictionary<int, string> Skipped => skipped;
    internal bool LastHandHadAttack { get; private set; }
    internal int[] Planned { get; private set; } = [];

    internal void Prepare(int energy, int cardsToDraw = 5, Func<int, string?>? unavailable = null)
    {
        if (hand.Count != 0 || playing != null) throw new InvalidOperationException("Finish the royal turn before drawing again.");
        if (!shuffled) { Shuffle(); shuffled = true; }
        Energy = Capacity = energy + NextEnergy;
        int requested = Math.Min(HandLimit, cardsToDraw + NextDraw);
        nextCardDraw = nextCardEnergy = nextRelicDraw = nextRelicEnergy = 0;
        refundableEnergySpent = resetEnergyRefunded = 0;
        DrawnThisTurn = 0;
        LastHandHadAttack = false;
        sacrifices.Clear(); skipped.Clear();
        for (int i = 0; i < requested; i++)
        {
            if (draw.Count == 0) { draw.AddRange(discard); discard.Clear(); Shuffle(); }
            if (draw.Count == 0) break;
            hand.Add(draw[^1]); draw.RemoveAt(draw.Count - 1); DrawnThisTurn++;
        }
        int available = Energy;
        var planned = new List<int>();
        foreach (int card in hand.OrderByDescending(card => Costs[card]))
        {
            string? reason = unavailable?.Invoke(card);
            if (reason != null) { skipped[card] = reason; continue; }
            if (Costs[card] > available || planned.Count == PlayLimit) continue;
            available -= Costs[card]; planned.Add(card);
        }
        // Reserve material BEFORE publishing intents. Never discard an announced play.
        foreach (int card in planned.Where(RequiresMaterial).ToArray())
        {
            int? victim = FindMaterial(card, planned);
            if (victim == null)
                victim = planned.AsEnumerable().Reverse().Where(i => i != card && !RequiresMaterial(i) && !sacrifices.ContainsValue(i)).Select(i => (int?)i).FirstOrDefault();
            if (victim == null) { planned.Remove(card); skipped[card] = "no_material"; continue; }
            planned.Remove(victim.Value); sacrifices[card] = victim.Value;
        }
        foreach (int card in planned.Where(i => i is 38 or 43))
        {
            int? victim = FindMaterial(card, planned);
            if (victim != null) sacrifices[card] = victim.Value;
        }
        // Cleanse before granting stats, so removing negative Strength cannot erase
        // a newly granted buff and the native intent projection remains unambiguous.
        // Reset follows other Skills/Powers, but never anticipates later attack
        // payments. The established support-then-attack phases remain intact.
        Planned = planned.OrderBy(card => IsAttack(card) ? 3 : card == ResetCard ? 2 : card is 25 or 19 ? 0 : 1).ToArray();
    }

    private int? FindMaterial(int card, List<int> planned) => hand.Where(i => i != card && !planned.Contains(i) && !sacrifices.ContainsValue(i)).Select(i => (int?)i).FirstOrDefault();
    internal bool HasMaterial(int card) => sacrifices.TryGetValue(card, out int victim) && hand.Contains(victim);
    internal int? UseMaterial(int card, bool isExhaust)
    {
        if (!sacrifices.Remove(card, out int victim) || !hand.Remove(victim)) return null;
        (isExhaust ? exhaust : discard).Add(victim);
        return victim;
    }

    internal (int Draw, int Energy) ReserveNext(int cards, int energy, bool relic = false)
    {
        if (cards < 0 || energy < 0) throw new ArgumentOutOfRangeException(nameof(cards));
        if (relic) { nextRelicDraw += cards; nextRelicEnergy += energy; return (cards, energy); }
        int gainedDraw = Math.Min(cards, 3 - nextCardDraw), gainedEnergy = Math.Min(energy, 2 - nextCardEnergy);
        nextCardDraw += gainedDraw; nextCardEnergy += gainedEnergy;
        return (gainedDraw, gainedEnergy);
    }

    internal void Spend(int card)
    {
        if (playing != null || !hand.Contains(card) || !Planned.Contains(card) || Energy < Costs[card])
            throw new InvalidOperationException("Invalid royal card play.");
        int before = Energy;
        Energy -= Costs[card]; hand.Remove(card); playing = card;
        if (card != ResetCard) refundableEnergySpent += before - Energy;
        LastHandHadAttack |= IsAttack(card);
    }

    // Reserve and claim synchronously before any presentation/command await.
    // Repeated resolution cannot refund a payment twice. Only the actual grant
    // counts as refunded; all Reset costs are excluded at the payment boundary.
    internal (int Eligible, int Gained, int Remaining) ReserveReset()
    {
        if (playing != ResetCard) throw new InvalidOperationException("Reset must be the resolving royal card.");
        int eligible = refundableEnergySpent - resetEnergyRefunded;
        int gained = ReserveNext(0, eligible).Energy;
        resetEnergyRefunded += gained;
        return (eligible, gained, eligible - gained);
    }

    internal void Complete(int card)
    {
        if (playing != card) throw new InvalidOperationException("Wrong royal play completion.");
        playing = null;
        (IsPersistent(card) ? persistent : IsExhaust(card) ? exhaust : discard).Add(card);
    }
    internal void Finish()
    {
        if (playing != null) throw new InvalidOperationException("A royal card is still resolving.");
        discard.AddRange(hand); hand.Clear(); Planned = []; sacrifices.Clear();
    }
    internal string Snapshot => $"energy={Energy}/{Capacity};next={NextDraw}/{NextEnergy};cardBank={nextCardDraw}/{nextCardEnergy};relicBank={nextRelicDraw}/{nextRelicEnergy};resetLedger={refundableEnergySpent}/{resetEnergyRefunded};draw={string.Join(',', draw)};hand={string.Join(',', hand)};play={playing};discard={string.Join(',', discard)};exhaust={string.Join(',', exhaust)};powers={string.Join(',', persistent)};planned={string.Join(',', Planned)};material={string.Join(',', sacrifices.OrderBy(p => p.Key).Select(p => $"{p.Key}:{p.Value}"))}";
    internal static bool IsAttack(int card) => card is 0 or 1 or 2 or 9 or 10 or 11 or 17 or 18 or 23 or 29 or 37 or 40;
    internal static bool IsPersistent(int card) => card is 34 or 45 or 46;
    internal static bool IsPower(int card) => IsPersistent(card) || card is 6 or 7 or 24;
    internal static bool IsExhaust(int card) => card is 25 or 26 or 30 or 31 or 35 or 36 or 41 or 42 or ResetCard;
    internal static bool RequiresMaterial(int card) => card is 32 or 39 or 42;
    private void Shuffle()
    {
        for (int i = draw.Count - 1; i > 0; i--)
        {
            int j = next(i + 1);
            (draw[i], draw[j]) = (draw[j], draw[i]);
        }
    }
}
