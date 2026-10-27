using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;

namespace LaRata;

[Pool(typeof(SharedPotionPool))]
public abstract class RoyalPotion : CustomPotionModel
{
    internal abstract string ArtKey { get; }
    public override PotionUsage Usage => PotionUsage.CombatOnly;
    public override string CustomPackedImagePath => $"res://LaRata/images/potions/{ArtKey}.png";
    public override string CustomPackedOutlinePath => CustomPackedImagePath;
    public override string CustomLargeImagePath => CustomPackedImagePath;
    public override List<(string, string)> Localization => [];
    protected bool CanResolve => Owner.Creature.IsAlive && !CombatManager.Instance.IsOverOrEnding;
    protected bool OwnHandCard(CardModel card) => card.Owner == Owner && card.Pile?.Type == PileType.Hand;
}

public sealed class Ashbrand : RoyalPotion
{
    internal override string ArtKey => "ashbrand";
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override TargetType TargetType => TargetType.Self;
    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    internal static bool Eligible(CardModel card) => !card.EnergyCost.CostsX && !card.HasStarCostX &&
        card.EnergyCost.GetWithModifiers(CostModifiers.None) >= 0;

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var card = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), Eligible, this)).FirstOrDefault();
        if (!CanResolve || card == null || !OwnHandCard(card) || !Eligible(card)) return;
        int energy = card.EnergyCost.GetWithModifiers(CostModifiers.None);
        var history = CombatManager.Instance.History;
        int offset = history.Entries.Count();
        await CardCmd.Exhaust(choiceContext, card);
        // Exhaust callbacks may move the same card again; use the native event,
        // not its final pile, to decide whether fuel was actually consumed.
        if (CanResolve && energy > 0 && history.Entries.Skip(offset).OfType<CardExhaustedEntry>().Any(e => e.Card == card))
            await PlayerCmd.GainEnergy(energy, Owner);
    }
}

public sealed class Rustwater : RoyalPotion
{
    internal override string ArtKey => "rustwater";
    public override PotionRarity Rarity => PotionRarity.Uncommon;
    public override TargetType TargetType => TargetType.AnyEnemy;
    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        AssertValidForTargetedPotion(target);
        if (!CanResolve || target!.Side != CombatSide.Enemy || !target.IsAlive) return;
        await CreatureCmd.LoseBlock(target, target.Block);
    }
}

public sealed class CrownLacquer : RoyalPotion
{
    internal override string ArtKey => "crown_lacquer";
    public override PotionRarity Rarity => PotionRarity.Rare;
    public override TargetType TargetType => TargetType.Self;
    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    internal static bool Eligible(CardModel card) => card.GetKeywordsWithSources(KeywordSources.Local).Contains(CardKeyword.Exhaust);
    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var card = (await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), Eligible, this)).FirstOrDefault();
        if (!CanResolve || card == null || !OwnHandCard(card) || !Eligible(card)) return;
        // Native combat cards are separate from their deck originals. Changing
        // the actual hand instance lasts across reshuffles, not into the next fight.
        card.RemoveKeyword(CardKeyword.Exhaust);
    }
}
