using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace LaRata;

public sealed class BrassPolish : RoyalPotion
{
    internal override string ArtKey => "brass_polish";
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override TargetType TargetType => TargetType.Self;
    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VigorPower>()];
    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (!CanResolve) return;
        int block = Owner.Creature.Block;
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, block, Owner.Creature, null);
    }
}

public sealed class AmberHourglass : RoyalPotion
{
    internal override string ArtKey => "amber_hourglass";
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override TargetType TargetType => TargetType.Self;
    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<BlurPower>()];
    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        if (CanResolve)
            await PowerCmd.Apply<BlurPower>(choiceContext, Owner.Creature, 2, Owner.Creature, null);
    }
}

public sealed class GraveDistillate : RoyalPotion
{
    internal override string ArtKey => "grave_distillate";
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override TargetType TargetType => TargetType.AnyEnemy;
    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<PoisonPower>(), HoverTipFactory.FromPower<DoomPower>()];
    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        if (!CanResolveOnEnemy(target) || !target.CanReceivePowers) return;
        int poison = target.GetPowerAmount<PoisonPower>();
        int doom = target.GetPowerAmount<DoomPower>();
        if (poison == doom) return;

        // Snapshot BOTH amounts before any removal/await. Reapplications use
        // native modifiers in the approved fixed order: Poison, then Doom.
        // Artifact/immunity can block them; no bypass or compensating writes.
        await PowerCmd.Remove<PoisonPower>(target);
        if (!CanResolveOnEnemy(target)) return;
        await PowerCmd.Remove<DoomPower>(target);
        if (!CanResolveOnEnemy(target)) return;
        if (doom > 0) await PowerCmd.Apply<PoisonPower>(choiceContext, target, doom, Owner.Creature, null);
        if (!CanResolveOnEnemy(target)) return;
        if (poison > 0) await PowerCmd.Apply<DoomPower>(choiceContext, target, poison, Owner.Creature, null);
    }
}
