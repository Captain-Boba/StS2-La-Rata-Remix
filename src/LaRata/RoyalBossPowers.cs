using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

// Explicit royal events: player AfterCardPlayed/AfterCardDiscarded hooks do not
// fire for monster-owned cards. Amount also carries the once-per-turn latch.
public sealed class RoyalSecondShadowPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string? CustomPackedIconPath => ModelDb.Power<AfterimagePower>().IconPath;
    public override string? CustomBigIconPath => CustomPackedIconPath;
    public override List<(string, string)> Localization => [];
    internal async Task AfterRoyalCard()
    {
        if (Owner.IsAlive && !CombatManager.Instance.IsEnding)
            await CreatureCmd.GainBlock(Owner, 1, ValueProp.Unpowered, null, fast: true);
    }
}

public abstract class RoyalOncePerTurnPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => Amount == 1 ? 1 : 0;
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == CombatSide.Enemy && participants.Contains(Owner) && Amount != 1)
            await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, 1 - Amount, Owner, null);
    }
    protected async Task<bool> Consume()
    {
        if (Amount != 1 || !Owner.IsAlive || CombatManager.Instance.IsEnding) return false;
        await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, 1, Owner, null);
        return true;
    }
}

public sealed class RoyalSmugglersDividendPower : RoyalOncePerTurnPower
{
    public override string? CustomPackedIconPath => ModelDb.Power<VigorPower>().IconPath;
    public override string? CustomBigIconPath => CustomPackedIconPath;
    public override List<(string, string)> Localization => [];
    internal async Task AfterRoyalDiscard()
    {
        if (await Consume())
            await PowerCmd.Apply<VigorPower>(new ThrowingPlayerChoiceContext(), Owner, 3, Owner, null);
    }
}

public sealed class RoyalCourtOfCindersPower : RoyalOncePerTurnPower
{
    public override string? CustomPackedIconPath => ModelDb.Power<PoisonPower>().IconPath;
    public override string? CustomBigIconPath => CustomPackedIconPath;
    public override List<(string, string)> Localization => [];
    internal async Task AfterRoyalExhaust(RatKing king)
    {
        if (!await Consume()) return;
        await PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(), RoyalBossEffects.Players(king), 5, Owner, null);
    }
}
