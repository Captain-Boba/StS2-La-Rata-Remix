using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

public abstract class DecisionRelic : CustomRelicModel
{
    protected abstract string ArtKey { get; }
    public override string PackedIconPath => $"res://LaRata/images/relics/{ArtKey}.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    public override List<(string, string)> Localization => [];

    protected bool InCombat => !IsCanonical && !HasBeenRemovedFromState && !IsMelted &&
        Owner?.Creature is { IsAlive: true } && CombatManager.Instance.IsInProgress;
    protected bool OwnTurn => InCombat && !CombatManager.Instance.IsEnding &&
        !CombatManager.Instance.EndingPlayerTurnPhaseOne && !CombatManager.Instance.EndingPlayerTurnPhaseTwo &&
        CombatManager.Instance.IsPartOfPlayerTurn(Owner);

    protected virtual void ResetTurn() { }
    protected virtual void ResetCombat() => ResetTurn();
    public override Task BeforeCombatStart() { ResetCombat(); return Task.CompletedTask; }
    public override Task BeforeSideTurnStart(PlayerChoiceContext context, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature)) ResetTurn();
        return Task.CompletedTask;
    }
    public override Task AfterCombatEnd(CombatRoom room) { ResetCombat(); return Task.CompletedTask; }
}

[Pool(typeof(SharedRelicPool))]
public sealed class SootboundTicket : DecisionRelic
{
    protected override string ArtKey => "sootbound_ticket";
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    [SavedProperty] public bool TriggeredThisTurn { get; set; }
    protected override void ResetTurn() => TriggeredThisTurn = false;

    public override async Task AfterCardExhausted(PlayerChoiceContext context, CardModel card, bool causedByEthereal)
    {
        if (!OwnTurn || card.Owner != Owner || TriggeredThisTurn || causedByEthereal ||
            card.EnergyCost.CostsX || card.EnergyCost.GetWithModifiers(CostModifiers.None) < 2 ||
            PileType.Hand.GetPile(Owner).Cards.Count >= CardPile.MaxCardsInHand) return;
        var candidates = PileType.Discard.GetPile(Owner).Cards.Where(c => c.Owner == Owner).ToList();
        if (candidates.Count == 0) return;
        var recovered = Owner.RunState.Rng.CombatCardSelection.NextItem(candidates);
        if (recovered == null) return;
        TriggeredThisTurn = true; // Reserve before pile hooks can re-enter.
        Flash();
        var result = await CardPileCmd.Add(recovered, PileType.Hand);
        if (result.success) RelicMetrics.Record(this, "cards_recovered", 1, "Cards");
    }
}

[Pool(typeof(SharedRelicPool))]
public sealed class StairwellKey : DecisionRelic
{
    protected override string ArtKey => "stairwell_key";
    public override RelicRarity Rarity => RelicRarity.Rare;
    [SavedProperty] public int LastEnergySpent { get; set; } = -1;
    [SavedProperty] public int DescendingCount { get; set; }
    [SavedProperty] public bool TriggeredThisTurn { get; set; }
    protected override void ResetTurn()
    { LastEnergySpent = -1; DescendingCount = 0; TriggeredThisTurn = false; }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (!OwnTurn || play.Card.Owner != Owner || play.IsAutoPlay || !play.IsFirstInSeries || TriggeredThisTurn) return;
        int spent = play.Resources.EnergySpent;
        DescendingCount = DescendingCount > 0 && spent < LastEnergySpent ? DescendingCount + 1 : 1;
        LastEnergySpent = spent;
        if (DescendingCount < 3) return;
        TriggeredThisTurn = true;
        Flash();
        await PlayerCmd.GainEnergy(1, Owner);
        if (OwnTurn) await CardPileCmd.Draw(context, 2, Owner);
    }
}

[Pool(typeof(SharedRelicPool))]
public sealed class MockingBell : DecisionRelic
{
    protected override string ArtKey => "mocking_bell";
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>()];
    [SavedProperty] public bool TriggeredThisTurn { get; set; }
    protected override void ResetTurn() => TriggeredThisTurn = false;

    public override async Task AfterPowerAmountChanged(PlayerChoiceContext context, PowerModel power, decimal amount,
        Creature? applier, CardModel? cardSource)
    {
        if (!OwnTurn || TriggeredThisTurn || (int)amount == 0 || !power.Owner.IsEnemy ||
            (applier != Owner.Creature && applier?.PetOwner != Owner) ||
            power.GetTypeForAmount(amount) != PowerType.Debuff || !power.Owner.Powers.Contains(power)) return;
        var candidates = Owner.Creature.CombatState?.HittableEnemies.Where(c => c != power.Owner).ToList();
        if (candidates == null || candidates.Count == 0) return;
        var target = Owner.RunState.Rng.CombatTargets.NextItem(candidates);
        if (target == null) return;
        TriggeredThisTurn = true; // Weak itself can invoke AfterPowerAmountChanged.
        Flash();
        await PowerCmd.Apply<WeakPower>(context, target, 1, Owner.Creature, null);
    }
}

[Pool(typeof(SharedRelicPool))]
public sealed class BrassScavenger : DecisionRelic
{
    protected override string ArtKey => "brass_scavenger";
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    [SavedProperty] public bool UsedThisCombat { get; set; }
    protected override void ResetCombat() => UsedThisCombat = false;

    public override async Task AfterDamageGiven(PlayerChoiceContext context, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (!InCombat || UsedThisCombat || !target.IsEnemy || !result.WasBlockBroken || result.BlockedDamage <= 0 ||
            cardSource is not { Type: CardType.Attack } || cardSource.Owner != Owner ||
            (dealer != Owner.Creature && dealer?.PetOwner != Owner)) return;
        UsedThisCombat = true;
        Flash();
        await PlayerCmd.GainGold(Math.Min(12, result.BlockedDamage), Owner);
    }
}

[Pool(typeof(SharedRelicPool))]
public sealed class MoonlitStopper : DecisionRelic
{
    protected override string ArtKey => "moonlit_stopper";
    public override RelicRarity Rarity => RelicRarity.Rare;
    [SavedProperty] public bool UsedThisCombat { get; set; }
    private bool armed;
    [SavedProperty]
    public bool Armed
    {
        get => armed;
        set
        {
            if (armed == value) return;
            armed = value;
            // Global cost hooks do not mutate CardEnergyCost. Tell the native
            // tracker to recompute visible Energy AND Stars when availability
            // changes; no UI node or second cost modifier is needed.
            NotifyHandCosts();
        }
    }
    private void NotifyHandCosts()
    {
        if (IsCanonical || Owner?.PlayerCombatState is not { } state) return;
        foreach (var card in state.Hand.Cards.ToArray())
            if (card.Owner == Owner && card.Pile?.Type == PileType.Hand &&
                !card.EnergyCost.CostsX && !card.HasStarCostX)
                card.InvokeEnergyCostChanged();
    }
    protected override void ResetTurn() => Armed = false;
    protected override void ResetCombat() { UsedThisCombat = false; Armed = false; }

    public override Task AfterPotionDiscarded(PotionModel potion)
    {
        if (potion.Owner != Owner || !OwnTurn || UsedThisCombat) return Task.CompletedTask;
        UsedThisCombat = true;
        Armed = true;
        Flash();
        return Task.CompletedTask;
    }
    private bool DiscountApplies(CardModel card) => Armed && OwnTurn && card.Owner == Owner &&
        !card.EnergyCost.CostsX && !card.HasStarCostX && card.Pile?.Type is PileType.Hand or PileType.Play;
    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = DiscountApplies(card) ? 0 : originalCost;
        return modifiedCost != originalCost;
    }
    public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = DiscountApplies(card) ? 0 : originalCost;
        return modifiedCost != originalCost;
    }
    public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay play)
    {
        if (play.Card.Owner == Owner && !play.IsAutoPlay && play.IsFirstInSeries &&
            !play.Card.EnergyCost.CostsX && !play.Card.HasStarCostX) Armed = false;
        return Task.CompletedTask;
    }
    public override Task BeforeSideTurnEnd(PlayerChoiceContext context, CombatSide side, IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner.Creature)) Armed = false;
        return Task.CompletedTask;
    }
    public override Task AfterRemoved()
    {
        Armed = false;
        return base.AfterRemoved();
    }
}
