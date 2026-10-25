using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

public abstract class ComboCard(int cost, CardType type, CardRarity rarity, TargetType target)
    : CustomCardModel(cost, type, rarity, target)
{
    internal abstract string ArtKey { get; }
    public override CardPoolModel Pool => ModelDb.CardPool<ColorlessCardPool>();
    public override Material? CreateCustomFrameMaterial => RoyalCardStyle.Frame();
    public override Material? CreateCustomBannerMaterial => RoyalCardStyle.Banner(Rarity);
    public override string? CustomPortraitPath => $"res://LaRata/images/cards/{ArtKey}.png";
    public override List<(string, string)> Localization => [];
    protected bool CanContinue => Owner.Creature.IsAlive && !CombatManager.Instance.IsOverOrEnding;

    protected async Task DiscardOptional(PlayerChoiceContext ctx)
    {
        var cards = (await CardSelectCmd.FromHandForDiscard(ctx, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 0, 1), null, this)).ToArray();
        if (CanContinue && cards.Length > 0) await CardCmd.Discard(ctx, cards);
    }

    protected async Task<bool> ExhaustFuel(PlayerChoiceContext ctx)
    {
        var chosen = (await CardSelectCmd.FromHand(ctx, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, 1), c => c != this, this)).SingleOrDefault();
        if (!CanContinue || chosen?.Pile?.Type != PileType.Hand) return false;
        var history = CombatManager.Instance.History;
        int historyOffset = history.Entries.Count();
        await CardCmd.Exhaust(ctx, chosen);
        // Native callbacks may already have moved the exhausted card again. Its recorded
        // Exhaust event, not its final pile, is the condition for receiving the benefit.
        return CanContinue && history.Entries.Skip(historyOffset).OfType<CardExhaustedEntry>().Any(e => e.Card == chosen);
    }
}

[Pool(typeof(ColorlessCardPool))]
public sealed class SerKevsThornBuster() : ComboCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
{
    internal override string ArtKey => "ser_kevs_thorn_buster";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(0), new PowerVar<VulnerablePower>(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ThornsPower>(), HoverTipFactory.FromPower<VulnerablePower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        // Do not use HittableEnemies: untargetable living enemies also lose Thorns.
        var stripped = new HashSet<Creature>();
        foreach (var enemy in CombatState!.Enemies.Where(c => c.IsAlive).OrderBy(c => c.CombatId).ToArray())
            foreach (var thorns in enemy.Powers.OfType<ThornsPower>().ToArray())
                if (thorns.Amount > 0 && enemy.Powers.Contains(thorns))
                {
                    await PowerCmd.Remove(thorns);
                    if (!enemy.Powers.Contains(thorns)) stripped.Add(enemy);
                }
        foreach (var enemy in stripped.OrderBy(c => c.CombatId))
            if (CanContinue && enemy.IsAlive)
                await PowerCmd.Apply<VulnerablePower>(ctx, enemy, DynamicVars["VulnerablePower"].BaseValue, Owner.Creature, this);
        if (CanContinue && DynamicVars.Cards.BaseValue > 0)
            await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(2);
}

[Pool(typeof(ColorlessCardPool))]
public sealed class GutterFeint() : ComboCard(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    internal override string ArtKey => "gutter_feint";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4, ValueProp.Move), new BlockVar(3, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(ctx);
        if (CanContinue) await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    }
    protected override void OnUpgrade() { DynamicVars.Damage.UpgradeValueBy(2); DynamicVars.Block.UpgradeValueBy(2); }
}

[Pool(typeof(ColorlessCardPool))]
public sealed class RagpickersGuard() : ComboCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    internal override string ArtKey => "ragpickers_guard";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(9, ValueProp.Move)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DiscardOptional(ctx);
        if (CanContinue) await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
    }
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3);
}

[Pool(typeof(ColorlessCardPool))]
public sealed class CinderReceipt() : ComboCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    internal override string ArtKey => "cinder_receipt";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VigorPower>(10)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VigorPower>(), HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (await ExhaustFuel(ctx))
            await PowerCmd.Apply<VigorPower>(ctx, Owner.Creature, DynamicVars["VigorPower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["VigorPower"].UpgradeValueBy(4);
}

[Pool(typeof(ColorlessCardPool))]
public sealed class VenomousEncore() : ComboCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    internal override string ArtKey => "venomous_encore";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(5, ValueProp.Move), new PowerVar<DoomPower>(4)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PoisonPower>(), HoverTipFactory.FromPower<DoomPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var target = play.Target!;
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target).WithHitCount(2).Execute(ctx);
        if (CanContinue && target.IsAlive && target.GetPowerAmount<PoisonPower>() > 0)
            await PowerCmd.Apply<DoomPower>(ctx, target, DynamicVars["DoomPower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2);
}

[Pool(typeof(ColorlessCardPool))]
public sealed class GraveyardShift() : ComboCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    internal override string ArtKey => "graveyard_shift";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CalculationBaseVar(0), new CalculationExtraVar(6),
        new CalculatedBlockVar(ValueProp.Move).WithMultiplier(static (_, target) =>
            Math.Max(0, target?.GetPowerAmount<VulnerablePower>() ?? 0))
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VulnerablePower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var target = play.Target;
        if (!CanContinue || target is not { IsAlive: true } || target.GetPower<VulnerablePower>() is not { Amount: > 0 } vulnerable) return;
        int stacks = vulnerable.Amount;
        await PowerCmd.Remove(vulnerable);
        if (CanContinue && !target.Powers.Contains(vulnerable))
            await CreatureCmd.GainBlock(Owner.Creature, stacks * DynamicVars.CalculationExtra.BaseValue,
                DynamicVars.CalculatedBlock.Props, play);
    }
    protected override void OnUpgrade() => DynamicVars.CalculationExtra.UpgradeValueBy(3);
}

[Pool(typeof(ColorlessCardPool))]
public sealed class ScrapCoronation() : ComboCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    internal override string ArtKey => "scrap_coronation";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new ForgeVar(8)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromForge();
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (await ExhaustFuel(ctx)) await ForgeCmd.Forge(DynamicVars.Forge.BaseValue, Owner, this);
    }
    protected override void OnUpgrade() => DynamicVars.Forge.UpgradeValueBy(4);
}

[Pool(typeof(ColorlessCardPool))]
public sealed class ColdCircuit() : ComboCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    internal override string ArtKey => "cold_circuit";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.Static(StaticHoverTip.Channeling), HoverTipFactory.FromOrb<FrostOrb>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DiscardOptional(ctx);
        if (!CanContinue) return;
        await OrbCmd.Channel<FrostOrb>(ctx, Owner);
        if (CanContinue) await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[Pool(typeof(ColorlessCardPool))]
public sealed class BorrowedBackbone() : ComboCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    internal override string ArtKey => "borrowed_backbone";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8, ValueProp.Move), new PowerVar<DoomPower>(5)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<DoomPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
        if (CanContinue && play.Target!.IsAlive)
            await PowerCmd.Apply<DoomPower>(ctx, play.Target, DynamicVars["DoomPower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(3); DynamicVars["DoomPower"].UpgradeValueBy(3); }
}

[Pool(typeof(ColorlessCardPool))]
public sealed class SmugglersDividend() : ComboCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    internal override string ArtKey => "smugglers_dividend";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VigorPower>(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VigorPower>(), HoverTipFactory.FromPower<SmugglersDividendPower>()];
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        PowerCmd.Apply<SmugglersDividendPower>(ctx, Owner.Creature, DynamicVars["VigorPower"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["VigorPower"].UpgradeValueBy(2);
}

[Pool(typeof(ColorlessCardPool))]
public sealed class CourtOfCinders() : ComboCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    internal override string ArtKey => "court_of_cinders";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PoisonPower>(5)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PoisonPower>(), HoverTipFactory.FromPower<CourtOfCindersPower>(), HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        PowerCmd.Apply<CourtOfCindersPower>(ctx, Owner.Creature, DynamicVars["PoisonPower"].BaseValue, Owner.Creature, this);
    protected override void OnUpgrade() => DynamicVars["PoisonPower"].UpgradeValueBy(3);
}
