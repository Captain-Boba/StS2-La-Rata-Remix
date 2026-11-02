using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

[Pool(typeof(ColorlessCardPool))]
public sealed class StaticTribute() : ComboCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    internal override string ArtKey => "static_tribute";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!CanContinue || play.Target is not { IsAlive: true } target) return;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target).Execute(ctx);
        if (!CanContinue) return;
        var queue = Owner.PlayerCombatState!.OrbQueue;
        foreach (var orb in queue.Orbs.ToArray())
        {
            if (!CanContinue) return;
            if (queue.Orbs.Contains(orb)) await OrbCmd.Passive(ctx, orb, null);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

[Pool(typeof(ColorlessCardPool))]
public sealed class GildedCoffin() : ComboCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    internal override string ArtKey => "gilded_coffin";
    public override TargetType TargetType => IsUpgraded ? TargetType.AllEnemies : TargetType.AnyEnemy;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DoomPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!CanContinue || !IsUpgraded && play.Target is not { IsAlive: true }) return;
        decimal block = Owner.Creature.Block;
        await CreatureCmd.LoseBlock(Owner.Creature, block);
        if (!CanContinue || block <= 0) return;
        var targets = IsUpgraded ? CombatState!.HittableEnemies.OrderBy(c => c.CombatId).ToArray() : [play.Target!];
        foreach (var target in targets)
            if (CanContinue && target.IsAlive)
                await PowerCmd.Apply<DoomPower>(ctx, target, block, Owner.Creature, this);
    }
    // Upgrading changes targeting, not cost, keywords or a numeric amount.
}

[Pool(typeof(ColorlessCardPool))]
public sealed class BonfireSale() : ComboCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    internal override string ArtKey => "bonfire_sale";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!CanContinue) return;
        var attacks = PileType.Discard.GetPile(Owner).Cards.Where(c => c.Type == CardType.Attack).ToArray();
        var history = MegaCrit.Sts2.Core.Combat.CombatManager.Instance.History;
        int offset = history.Entries.Count();
        foreach (var attack in attacks)
        {
            if (!CanContinue) return;
            if (attack.Owner == Owner && attack.Pile?.Type == PileType.Discard)
                await CardCmd.Exhaust(ctx, attack);
        }
        int exhausted = history.Entries.Skip(offset).OfType<CardExhaustedEntry>()
            .Where(e => e.Card.Owner == Owner && attacks.Contains(e.Card)).Select(e => e.Card).Distinct().Count();
        if (CanContinue && exhausted > 0) await PlayerCmd.GainEnergy(exhausted * DynamicVars.Energy.BaseValue, Owner);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[Pool(typeof(ColorlessCardPool))]
public sealed class Bloodsmith() : ComboCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    internal override string ArtKey => "bloodsmith";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new ForgeVar(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromForge();
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => CanContinue
        ? PowerCmd.Apply<BloodsmithPower>(ctx, Owner.Creature, DynamicVars.Forge.BaseValue, Owner.Creature, this)
        : Task.CompletedTask;
    protected override void OnUpgrade() => DynamicVars.Forge.UpgradeValueBy(2);
}

[Pool(typeof(ColorlessCardPool))]
public sealed class UnderTheTable() : ComboCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    internal override string ArtKey => "under_the_table";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new RepeatVar(2)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!CanContinue) return;
        // Native pile index 0 is the top; AddInternal(-1)/Bottom appends.
        var bottom = PileType.Draw.GetPile(Owner).Cards.LastOrDefault();
        if (bottom == null) return;
        int extraReplays = DynamicVars.Repeat.IntValue - 1;
        bottom.BaseReplayCount += extraReplays;
        try
        {
            // One native play sequence captures X once, applies normal replay
            // modifiers and moves a Power/Exhaust card to its result pile once.
            await CardCmd.AutoPlay(ctx, bottom, null);
        }
        finally { bottom.BaseReplayCount -= extraReplays; }
    }
    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1);
}
