namespace LaRata;

// Apply fractions to raw bases, not already-rounded or multiplayer-scaled grants.
// Native GainBlock performs additive/multiplicative hooks and final truncation.
internal static class RoyalBalance
{
    internal static decimal CardBlock(decimal basis, bool ratKing) => ratKing ? basis * .5m : basis;
    internal static decimal ReducedHealing(decimal basis) => basis <= 0 ? 0 : Math.Max(1, Math.Floor(basis * .25m));
    // Grave Levy is a final percentage of the current, already-scaled maximum HP.
    // It must not pass through the general quarter-healing rule a second time.
    internal static decimal GraveLevyHealing(decimal maxHp) => maxHp <= 0 ? 0 : Math.Max(1, Math.Floor(maxHp * .01m));
    internal static decimal GuardHealing(decimal maxHp, decimal rate, bool targetIsRatKing) =>
        targetIsRatKing ? ReducedHealing(maxHp * rate) : Math.Ceiling(maxHp * rate);
}
