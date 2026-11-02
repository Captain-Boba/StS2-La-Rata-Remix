using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

public sealed class LastLaughPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // Each played copy contributes its own Block and replacement draw, including
    // mixed base/upgraded copies, using native power-instance semantics.
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
    public override string? CustomPackedIconPath => ModelDb.Power<DrawCardsNextTurnPower>().PackedIconPath;
    public override string? CustomBigIconPath => CustomPackedIconPath;
    public override List<(string, string)> Localization => [];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    public override LocString Description
    {
        get { var text = base.Description; DynamicVars.AddTo(text); return text; }
    }
    public override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        // Native per-instance dynamic data survives power cloning and is rebuilt by replay.
        DynamicVars.Cards.BaseValue = cardSource is LastLaugh ? cardSource.DynamicVars.Cards.BaseValue : 1;
        return Task.CompletedTask;
    }

    // Transient per-power work queue. Native combat replay rebuilds it; no static
    // owner state or recursion through a long run of Status/Curse draws.
    private int pending;
    private bool resolving;
    private bool Active => Owner.Player != null && Owner.IsAlive && Owner.Powers.Contains(this) &&
        CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsOverOrEnding;

    public override async Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        if (!Active || card.Owner != Owner.Player || card.Type is not (CardType.Status or CardType.Curse)) return;
        pending++;
        if (resolving) return;
        resolving = true;
        try
        {
            while (pending > 0 && Active)
            {
                pending--;
                Flash();
                await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
                if (Active) await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner.Player!);
            }
        }
        finally { pending = 0; resolving = false; }
    }
}
