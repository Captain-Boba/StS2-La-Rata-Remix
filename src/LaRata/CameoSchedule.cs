using MegaCrit.Sts2.Core.Random;

namespace LaRata;

// Separate native RNG streams: never advances the game's combat/card RNG.
internal static class CameoSchedule
{
    internal static int? GoblinRound(uint seed, int combatOrdinal)
    {
        if (combatOrdinal < 1) return null;
        int offset = new Rng(seed, $"LaRata/goblin/v1/block/{(combatOrdinal - 1) / 3}").NextInt(3);
        return ChaosSelections.GoblinVisits(combatOrdinal, offset)
            ? 3 + new Rng(seed, $"LaRata/goblin/v1/arrival/{combatOrdinal}").NextInt(2) : null;
    }

    internal static int? RoyalRound(uint seed, int eligibleOrdinal, int combatOrdinal)
    {
        if (eligibleOrdinal < 1) return null;
        int offset = new Rng(seed, $"LaRata/royal-cameo/v1/block/{(eligibleOrdinal - 1) / 3}").NextInt(3);
        if ((eligibleOrdinal - 1) % 3 != offset) return null;
        int round = 2 + new Rng(seed, $"LaRata/royal-cameo/v1/arrival/{eligibleOrdinal}").NextInt(2);
        return round == GoblinRound(seed, combatOrdinal) ? 2 : round;
    }

    // No surprise direct damage, Strength before already-announced attacks or Doom.
    // These are actual cards from La Rata's deck with their full printed effects.
    internal static readonly int[] RoyalPool = [3, 5, 6, 8];
    internal static int Encode(int round, int card) => round * 16 + card;
    internal static int Card(int value) => value % 16;
    internal static int Round(int value) => value / 16;
}
