using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace LaRata;

public abstract class BoosterRelic : CustomRelicModel
{
    protected abstract int Pack { get; }
    internal static readonly string[] Labels = ["Ironclad", "Silent", "Defect", "Regent", "Necrobinder", "La Rata"];
    internal static readonly string[] Art = ["ironclad_pack", "silent_pack", "defect_pack", "regent_pack", "necrobinder_pack", "royal_pack"];
    public override RelicRarity Rarity => RelicRarity.Common;
    public override bool HasUponPickupEffect => true;
    [SavedProperty] public bool Opened { get; set; }
    [SavedProperty] public bool StarterPending { get; set; }
    [SavedProperty] public List<SerializableCard> Contents { get; set; } = [];
    private bool opening;
    public override string PackedIconPath => $"res://LaRata/images/relics/{Art[Pack]}.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    public override List<(string, string)> Localization =>
    [
        ("title", $"{Labels[Pack]} Booster"),
        ("description", $"Upon pickup, open 5 different {Labels[Pack]} cards plus 2 neutral cards from the regular and La Rata pools. Keep any number of them." + (Pack == 5 ? " A 20% chance replaces one of the first five cards with a playable royal boss card." : " Any character can use them.")),
        ("flavor", "A little cardboard. A lot of possibilities."),
        ("selection", "Choose any cards to keep. Unselected cards are discarded. You may keep all or none.")
    ];
    public override Task AfterObtained() => StarterPending ? Task.CompletedTask : OpenOnce();
    internal async Task OpenStarter()
    {
        await OpenOnce();
        if (Opened) StarterPending = false;
    }
    private async Task OpenOnce()
    {
        if (Opened || opening) return;
        opening = true;
        try { await Open(); }
        finally { opening = false; }
    }
    public override Task AfterRoomEntered(AbstractRoom room) => Opened || Contents.Count == 0 ? Task.CompletedTask : AfterObtained();
    private async Task Open()
    {
        CardPoolModel[] pools = [ModelDb.CardPool<IroncladCardPool>(), ModelDb.CardPool<SilentCardPool>(), ModelDb.CardPool<DefectCardPool>(), ModelDb.CardPool<RegentCardPool>(), ModelDb.CardPool<NecrobinderCardPool>()];
        List<CardCreationResult> cards;
        if (Contents.Count != 0)
            cards = Contents.Select(c => new CardCreationResult(Owner.RunState.LoadCard(c, Owner))).ToList();
        else
        {
            // Regular reward RNG/rarity odds can be local-only. Use a native isolated RNG
            // and Uniform odds so all peers generate the same choices before syncing indexes.
            var rng = new Rng(Owner.RunState.Rng.Seed, $"LaRata/booster/v1/{Owner.NetId}/{Id.Entry}/{FloorAddedToDeck}");
            var pool = (Pack == 5 ? RemixCard.Neutrals : pools[Pack].GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint))
                .Where(c => c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)
                .OrderBy(c => c.Id.Entry, StringComparer.Ordinal).ToArray();
            var options = new CardCreationOptions(pool, CardCreationSource.Other, CardRarityOddsType.Uniform)
                .WithRngOverride(rng).WithFlags(CardCreationFlags.NoUpgradeRoll | CardCreationFlags.NoModifyHooks | CardCreationFlags.NoCardPoolModifications | CardCreationFlags.NoCardModelModifications);
            cards = CardFactory.CreateForReward(Owner, 5, options).ToList();
            if (Pack == 5 && rng.NextInt(5) == 0)
            {
                int slot = rng.NextInt(5);
                var prize = rng.NextItem(RemixCard.BossPrizes)!;
                cards[slot] = new CardCreationResult(Owner.RunState.CreateCard(prize, Owner));
            }
            var neutralPool = NeutralBonusPool(
                ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint),
                cards.Select(c => c.Card));
            var neutralOptions = new CardCreationOptions(neutralPool, CardCreationSource.Other, CardRarityOddsType.Uniform)
                .WithRngOverride(rng).WithFlags(CardCreationFlags.NoUpgradeRoll | CardCreationFlags.NoModifyHooks | CardCreationFlags.NoCardPoolModifications | CardCreationFlags.NoCardModelModifications);
            cards.AddRange(CardFactory.CreateForReward(Owner, 2, neutralOptions));
            Contents = cards.Select(c => c.Card.ToSerializable()).ToList();
        }
        // Mark only these card instances; unrelated rewards/screens never receive the opening.
        // Native selection returns an empty result while combat is ending. Preserve
        // the unopened pack for the next room rather than treating that as rejection.
        if (CombatManager.Instance.IsEnding) return;
        foreach (var card in cards) BoosterOpening.Mark(card.Card, Labels[Pack], PackedIconPath);
        var prefs = new CardSelectorPrefs(new LocString("relics", Id.Entry + ".selection"), 0, cards.Count);
        try
        {
            var chosen = (await CardSelectCmd.FromSimpleGridForRewards(new BlockingPlayerChoiceContext(), cards, Owner, prefs)).ToArray();
            CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(chosen, PileType.Deck));
            Opened = true;
            Contents = [];
            ModEntry.Log.Info($"BOOSTER player={Owner.NetId} pack={Id.Entry} offered={string.Join(',', cards.Select(c => c.Card.Id.Entry))} kept={string.Join(',', chosen.Select(c => c.Id.Entry))}");
        }
        finally { foreach (var card in cards) BoosterOpening.Unmark(card.Card); }
    }

    internal static CardModel[] NeutralBonusPool(IEnumerable<CardModel> unlocked, IEnumerable<CardModel> offered) =>
        unlocked.Concat(RemixCard.Neutrals)
            .Where(c => c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)
            .ExceptBy(offered.Select(c => c.Id), c => c.Id).DistinctBy(c => c.Id)
            .OrderBy(c => c.Id.Entry, StringComparer.Ordinal).ToArray();
}

[Pool(typeof(SharedRelicPool))] public sealed class IroncladBooster : BoosterRelic { protected override int Pack => 0; }
[Pool(typeof(SharedRelicPool))] public sealed class SilentBooster : BoosterRelic { protected override int Pack => 1; }
[Pool(typeof(SharedRelicPool))] public sealed class DefectBooster : BoosterRelic { protected override int Pack => 2; }
[Pool(typeof(SharedRelicPool))] public sealed class RegentBooster : BoosterRelic { protected override int Pack => 3; }
[Pool(typeof(SharedRelicPool))] public sealed class NecrobinderBooster : BoosterRelic { protected override int Pack => 4; }
[Pool(typeof(SharedRelicPool))] public sealed class RoyalBooster : BoosterRelic { protected override int Pack => 5; }
