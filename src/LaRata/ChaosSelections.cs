namespace LaRata;

// Policies shared by the native-Rng runtime adapters and deterministic checks.
internal static class ChaosSelections
{
    internal static int[] Distinct(int count, int poolSize, Func<int, int> next)
    {
        if (count < 0 || count > poolSize) throw new ArgumentOutOfRangeException(nameof(count));
        var pool = Enumerable.Range(0, poolSize).ToList();
        var result = new int[count];
        for (int i = 0; i < count; i++)
        {
            int slot = next(pool.Count);
            result[i] = pool[slot];
            pool.RemoveAt(slot);
        }
        return result;
    }

    internal static bool GoblinVisits(int combatOrdinal, int blockOffset) =>
        combatOrdinal > 0 && (combatOrdinal - 1) % 3 == blockOffset;
}
