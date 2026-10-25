using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace LaRata;

[Pool(typeof(IroncladCardPool))]
public sealed class WrathOfTheBigO() : ComboCard(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    internal override string ArtKey => "wrath_of_the_big_o";
    public override CardPoolModel Pool => ModelDb.CardPool<IroncladCardPool>();
    public override Material? CreateCustomFrameMaterial => null;
    public override Material? CreateCustomBannerMaterial => null;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!CanContinue) return;
        int strength = Owner.Creature.GetPowerAmount<StrengthPower>();
        // Preserve signed Strength and the native power/Artifact modifier path.
        if (strength != 0)
            await PowerCmd.Apply<StrengthPower>(ctx, Owner.Creature, strength, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[Pool(typeof(SilentCardPool))]
public sealed class TheKikilyst() : ComboCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    internal override string ArtKey => "the_kikilyst";
    public override CardPoolModel Pool => ModelDb.CardPool<SilentCardPool>();
    public override Material? CreateCustomFrameMaterial => null;
    public override Material? CreateCustomBannerMaterial => null;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Spread", 25)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PoisonPower>()];

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var target = play.Target;
        if (!CanContinue || target is not { IsAlive: true }) return;
        int poison = target.GetPowerAmount<PoisonPower>();
        if (poison > 0)
            await PowerCmd.Apply<PoisonPower>(ctx, target, poison, Owner.Creature, this);

        if (!CanContinue || CombatState is not { } combat) return;
        // Read after all native callbacks: Artifact or another modifier may have
        // prevented/changed the doubling. Never spread an assumed precomputed total.
        int resultingPoison = Math.Max(0, target.GetPowerAmount<PoisonPower>());
        decimal spread = decimal.Floor(resultingPoison * DynamicVars["Spread"].BaseValue / 100m);
        if (spread <= 0) return;
        var others = combat.Enemies.Where(c => c != target && c.IsAlive).OrderBy(c => c.CombatId).ToArray();
        foreach (var enemy in others)
        {
            if (!CanContinue) break;
            if (enemy.IsAlive)
                await PowerCmd.Apply<PoisonPower>(ctx, enemy, spread, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars["Spread"].UpgradeValueBy(25);
}
