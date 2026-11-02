using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace LaRata;

[Pool(typeof(SharedRelicPool))]
public sealed class CounterfeitSeal : DecisionRelic
{
    protected override string ArtKey => "counterfeit_seal";
    public override RelicRarity Rarity => RelicRarity.Rare;
    [SavedProperty] public bool TriggeredThisTurn { get; set; }
    protected override void ResetTurn() => TriggeredThisTurn = false;
    public override Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    { ResetTurn(); return Task.CompletedTask; }
    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        if (!InCombat || creator != Owner || card.Owner != Owner || TriggeredThisTurn ||
            card.HasBeenRemovedFromState || card.Pile?.IsCombatPile != true) return Task.CompletedTask;
        // The first actual creation consumes the opportunity, even if that card
        // cannot be upgraded. Draws and pile moves never enter this native hook.
        TriggeredThisTurn = true;
        if (card.IsUpgradable) { Flash(); CardCmd.Upgrade(card); }
        return Task.CompletedTask;
    }
}

[Pool(typeof(SharedRelicPool))]
public sealed class PlagueTestament : DecisionRelic
{
    protected override string ArtKey => "plague_testament";
    public override RelicRarity Rarity => RelicRarity.Rare;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PoisonPower>()];
    private Dictionary<Creature, int> dyingPoison = new(ReferenceEqualityComparer.Instance);
    protected override void DeepCloneFields()
    { base.DeepCloneFields(); dyingPoison = new(ReferenceEqualityComparer.Instance); }
    protected override void ResetCombat() => dyingPoison.Clear();
    public override Task BeforeDeath(Creature creature)
    {
        if (InCombat && creature.IsEnemy)
            dyingPoison[creature] = creature.GetPowerAmount<PoisonPower>();
        return Task.CompletedTask;
    }
    public override async Task AfterDeath(PlayerChoiceContext context, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (!dyingPoison.Remove(creature, out int poison) || wasRemovalPrevented ||
            !creature.IsDead || !InCombat || poison <= 0) return;
        var targets = Owner.Creature.CombatState!.HittableEnemies
            .Where(c => c != creature && c.IsAlive).ToList();
        if (targets.Count == 0) return;
        var target = Owner.RunState.Rng.CombatTargets.NextItem(targets);
        if (target == null) return;
        Flash();
        await PowerCmd.Apply<PoisonPower>(context, target, poison, Owner.Creature, null);
    }
}

[Pool(typeof(SharedRelicPool))]
public sealed class UsurpersTooth : DecisionRelic
{
    protected override string ArtKey => "usurpers_tooth";
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<StrengthPower>(), HoverTipFactory.FromPower<VigorPower>()];
    private Dictionary<PowerModel, int> beforeStrength = new(ReferenceEqualityComparer.Instance);
    protected override void DeepCloneFields()
    { base.DeepCloneFields(); beforeStrength = new(ReferenceEqualityComparer.Instance); }
    protected override void ResetCombat() => beforeStrength.Clear();
    public override Task BeforePowerAmountChanged(PowerModel power, decimal amount, Creature target,
        Creature? applier, CardModel? cardSource)
    {
        if (InCombat && power is StrengthPower && target.IsEnemy)
            beforeStrength[power] = power.Amount;
        return Task.CompletedTask;
    }
    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        if (!beforeStrength.Remove(power, out int before) || !InCombat || power is not StrengthPower ||
            !power.Owner.IsEnemy || power.Amount <= before || (int)amount <= 0) return;
        Flash();
        await PowerCmd.Apply<VigorPower>(context, Owner.Creature, 2, Owner.Creature, null);
    }
}
