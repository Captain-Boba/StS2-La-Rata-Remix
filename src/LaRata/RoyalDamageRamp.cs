using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

internal static class RoyalDamageRamp
{
    internal static decimal Factor(int round) => (40 + 10 * Math.Clamp(round - 1, 0, 6)) / 100m;

    internal static decimal For(Creature owner, Creature? dealer, ValueProp props)
    {
        if (!ReferenceEquals(owner, dealer) || owner.Side != CombatSide.Enemy ||
            owner.CombatState?.Encounter is not RatKingEncounter ||
            !props.HasFlag(ValueProp.Move) || props.HasFlag(ValueProp.Unpowered)) return 1m;
        return Factor(owner.CombatState.RoundNumber);
    }
}
