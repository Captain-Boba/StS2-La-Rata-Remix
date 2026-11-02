using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

[Pool(typeof(ColorlessCardPool))]
public sealed class RoyalReplication() : ComboCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    internal override string ArtKey => "royal_replication";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!CanContinue) return;
        // Freeze both membership and cloned state before the first generation hook.
        // Statuses and Curses are intentionally included; no run/deck copy is made.
        var originals = PileType.Draw.GetPile(Owner).Cards.ToArray();
        var copies = Enumerable.Range(0, DynamicVars.Cards.IntValue)
            .SelectMany(_ => originals.Select(card => card.CreateClone())).ToArray();
        foreach (var copy in copies)
        {
            if (!CanContinue) { copy.RemoveFromState(); continue; }
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Draw, Owner, CardPilePosition.Random);
        }
    }
    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1);
}

[Pool(typeof(ColorlessCardPool))]
public sealed class DeadLetter() : ComboCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    internal override string ArtKey => "dead_letter";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust), HoverTipFactory.ForEnergy(this)];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!CanContinue) return;
        var card = (await CardSelectCmd.FromCombatPile(ctx, PileType.Exhaust.GetPile(Owner), Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1))).FirstOrDefault();
        if (!CanContinue) return;
        if (card?.Owner == Owner && card.Pile?.Type == PileType.Exhaust)
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
        if (CanContinue) await PowerCmd.Apply<EnergyNextTurnPower>(ctx, Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}

[Pool(typeof(ColorlessCardPool))]
public sealed class PlagueParade() : ComboCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    internal override string ArtKey => "plague_parade";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PoisonPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!CanContinue || play.Target is not { IsAlive: true } target) return;
        var attack = DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(target);
        await attack.Execute(ctx);
        // Read this attack's native receipts, not HP deltas contaminated by other hooks.
        int poison = attack.Results.SelectMany(hit => hit).Where(result => result.Receiver == target)
            .Sum(result => result.UnblockedDamage);
        if (CanContinue && target.IsAlive && poison > 0)
            await PowerCmd.Apply<PoisonPower>(ctx, target, poison, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}

[Pool(typeof(ColorlessCardPool))]
public sealed class AshenOverture() : ComboCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    internal override string ArtKey => "ashen_overture";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(2, ValueProp.Move), new PowerVar<VigorPower>(2), new CardsVar(3)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VigorPower>()];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!CanContinue) return;
        var hand = PileType.Hand.GetPile(Owner).Cards.Where(card => card != this).ToArray();
        var history = CombatManager.Instance.History;
        int offset = history.Entries.Count();
        foreach (var card in hand)
        {
            if (!CanContinue) return;
            // Earlier discard hooks may already have played, exhausted or moved this card.
            if (card.Owner != Owner || card.Pile?.Type != PileType.Hand) continue;
            await CardCmd.Discard(ctx, card);
        }
        // Count actual native discards of the captured hand, including a remaining
        // hand card discarded by an intervening hook, never newly drawn cards.
        int discarded = history.Entries.Skip(offset).OfType<CardDiscardedEntry>()
            .Where(entry => entry.Card.Owner == Owner && hand.Contains(entry.Card))
            .Select(entry => entry.Card).Distinct().Count();
        for (int i = 0; i < discarded && CanContinue; i++)
        {
            // Like native Second Wind, each card grants its own modified Block.
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, play);
            if (CanContinue) await PowerCmd.Apply<VigorPower>(ctx, Owner.Creature, DynamicVars["VigorPower"].BaseValue, Owner.Creature, this);
        }
        if (CanContinue) await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
    }
    protected override void OnUpgrade() { DynamicVars.Block.UpgradeValueBy(1); DynamicVars["VigorPower"].UpgradeValueBy(1); }
}

[Pool(typeof(ColorlessCardPool))]
public sealed class PawnPromotion() : ComboCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    internal override string ArtKey => "pawn_promotion";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];

    // Deliberate pool: unlocked native class rewards plus La Rata player neutrals.
    // No native colorless, prize aliases, boss references or arbitrary third-party cards.
    internal static CardModel[] GenerationPool(Player owner)
    {
        CardPoolModel[] pools = [ModelDb.CardPool<IroncladCardPool>(), ModelDb.CardPool<SilentCardPool>(),
            ModelDb.CardPool<DefectCardPool>(), ModelDb.CardPool<RegentCardPool>(), ModelDb.CardPool<NecrobinderCardPool>()];
        var constraint = owner.RunState.CardMultiplayerConstraint;
        var unlockedPools = owner.UnlockState.CharacterCardPools.Select(pool => pool.Id).ToHashSet();
        var native = pools.Where(pool => unlockedPools.Contains(pool.Id))
            .SelectMany(pool => pool.GetUnlockedCards(owner.UnlockState, constraint))
            .Where(card => card.GetType().Assembly == typeof(CardModel).Assembly);
        return CardFactory.FilterForCombat(native.Concat(LaRataCardCatalog.Neutrals))
            .Where(card => card.Rarity == CardRarity.Rare && card.Type is CardType.Attack or CardType.Skill or CardType.Power)
            .Where(card => !card.Keywords.Contains(CardKeyword.Unplayable))
            // Native free-this-turn modifiers do not replace X resource spending.
            // Keep the same fixed-cost pool for base/+ so the upgrade's 0 is real.
            .Where(card => !card.EnergyCost.CostsX && !card.HasStarCostX)
            .Where(card => constraint != CardMultiplayerConstraint.SingleplayerOnly || card.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly)
            .Where(card => constraint != CardMultiplayerConstraint.MultiplayerOnly || card.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly)
            .DistinctBy(card => card.Id).OrderBy(card => card.Id.Entry, StringComparer.Ordinal).ToArray();
    }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        if (!CanContinue) return;
        var fuel = (await CardSelectCmd.FromHand(ctx, Owner, new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            card => card != this, this)).FirstOrDefault();
        if (!CanContinue || fuel?.Owner != Owner || fuel.Pile?.Type != PileType.Hand) return;
        var history = CombatManager.Instance.History;
        int offset = history.Entries.Count();
        await CardCmd.Exhaust(ctx, fuel);
        if (!CanContinue || !history.Entries.Skip(offset).OfType<CardExhaustedEntry>().Any(entry => entry.Card == fuel)) return;
        var reward = CardFactory.GetDistinctForCombat(Owner, GenerationPool(Owner), 1, Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();
        if (reward == null) return;
        CardCmd.Upgrade(reward);
        if (IsUpgraded) reward.SetToFreeThisTurn();
        if (CanContinue) await CardPileCmd.AddGeneratedCardToCombat(reward, PileType.Hand, Owner);
        else reward.RemoveFromState();
    }
}

[Pool(typeof(ColorlessCardPool))]
public sealed class LastLaugh() : ComboCard(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    internal override string ArtKey => "last_laugh";
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<LastLaughPower>(3), new CardsVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var preview = ModelDb.Power<LastLaughPower>().ToMutable(DynamicVars["LastLaughPower"].IntValue);
            preview.DynamicVars.Cards.BaseValue = DynamicVars.Cards.BaseValue;
            return [HoverTipFactory.FromPower(preview)];
        }
    }
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) => CanContinue
        ? PowerCmd.Apply<LastLaughPower>(ctx, Owner.Creature, DynamicVars["LastLaughPower"].BaseValue, Owner.Creature, this)
        : Task.CompletedTask;
    protected override void OnUpgrade() { DynamicVars["LastLaughPower"].UpgradeValueBy(2); DynamicVars.Cards.UpgradeValueBy(1); }
}
