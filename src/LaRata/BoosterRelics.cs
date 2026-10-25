using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
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

internal enum BoosterRarityPolicy { ClassMain, Neutral }

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
    public override List<(string, string)> Localization => [];
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
        List<CardCreationResult> cards;
        if (Contents.Count != 0)
            cards = Contents.Select(c => new CardCreationResult(Owner.RunState.LoadCard(c, Owner))).ToList();
        else
        {
            // Policy v2 applies only to newly generated packs. Saved five/seven-card
            // offers above remain authoritative, including their order and upgrades.
            var rng = new Rng(Owner.RunState.Rng.Seed, $"LaRata/booster/v2/{Owner.NetId}/{Id.Entry}/{FloorAddedToDeck}");
            cards = GenerateContents(Owner, Pack, rng);
            Contents = cards.Select(c => c.Card.ToSerializable()).ToList();
        }
        // Mark only these card instances; unrelated rewards/screens never receive the opening.
        // Native selection returns an empty result while combat is ending. Preserve
        // the unopened pack for the next room rather than treating that as rejection.
        if (CombatManager.Instance.IsEnding) return;
        foreach (var card in cards) BoosterOpening.Mark(card.Card, Title.GetFormattedText(), PackedIconPath);
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

    // This is the actual production creation path, also available to technical checks.
    // Native card creation preserves owners/serialization; no reward hooks, upgrade rolls,
    // or local PlayerOdds can reinterpret the explicit pack contract.
    // Five class main cards use 60/30/10; five royal main cards and both mixed
    // neutral bonuses use 75/25 Uncommon/Rare. The royal prize gate is separate.
    internal static List<CardCreationResult> GenerateContents(Player owner, int pack, Rng rng)
    {
        if (pack is < 0 or > 5) throw new ArgumentOutOfRangeException(nameof(pack));
        CardPoolModel[] pools = [ModelDb.CardPool<IroncladCardPool>(), ModelDb.CardPool<SilentCardPool>(), ModelDb.CardPool<DefectCardPool>(), ModelDb.CardPool<RegentCardPool>(), ModelDb.CardPool<NecrobinderCardPool>()];
        var constraint = owner.RunState.CardMultiplayerConstraint;
        var pool = pack == 5 ? RemixCard.Neutrals : pools[pack].GetUnlockedCards(owner.UnlockState, constraint);
        var mainPolicy = pack == 5 ? BoosterRarityPolicy.Neutral : BoosterRarityPolicy.ClassMain;
        var selected = SelectWeighted(pool, 5, constraint, rng, mainPolicy).ToList();
        if (pack == 5 && rng.NextInt(5) == 0)
        {
            int slot = rng.NextInt(5);
            var prizes = Eligible(RemixCard.BossPrizes, constraint).ToArray();
            selected[slot] = rng.NextItem(prizes) ?? throw new InvalidOperationException("No eligible royal booster prize.");
        }
        var neutralPool = NeutralBonusPool(ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(owner.UnlockState, constraint), selected);
        selected.AddRange(SelectWeighted(neutralPool, 2, constraint, rng, BoosterRarityPolicy.Neutral));
        return selected.Select(c => new CardCreationResult(owner.RunState.CreateCard(c, owner))).ToList();
    }

    internal static IReadOnlyList<CardModel> SelectWeighted(IEnumerable<CardModel> pool, int count, CardMultiplayerConstraint constraint, Rng rng, BoosterRarityPolicy policy)
    {
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        var weights = policy switch
        {
            BoosterRarityPolicy.ClassMain => ClassMainRarityWeights,
            BoosterRarityPolicy.Neutral => NeutralRarityWeights,
            _ => throw new ArgumentOutOfRangeException(nameof(policy))
        };
        // Unsupported rarities cannot satisfy the pool-size check or leak into
        // neutral offers if another mod adds Common colorless cards later.
        var remaining = Eligible(pool, constraint).Where(c => weights.Any(w => w.Rarity == c.Rarity)).ToList();
        if (remaining.Count < count)
            throw new InvalidOperationException($"Booster requires {count} distinct eligible cards, but only {remaining.Count} are available.");
        var selected = new List<CardModel>(count);
        for (int i = 0; i < count; i++)
        {
            var rarities = new WeightedList<CardRarity>();
            foreach (var (rarity, weight) in weights)
                if (remaining.Any(c => c.Rarity == rarity)) rarities.Add(rarity, weight);
            var chosenRarity = rarities.GetRandom(rng);
            var card = rng.NextItem(remaining.Where(c => c.Rarity == chosenRarity))!;
            selected.Add(card);
            remaining.Remove(card);
        }
        return selected;
    }

    internal static IReadOnlyList<(CardRarity Rarity, int Weight)> ClassMainRarityWeights { get; } =
        Array.AsReadOnly<(CardRarity, int)>([(CardRarity.Common, 60), (CardRarity.Uncommon, 30), (CardRarity.Rare, 10)]);

    internal static IReadOnlyList<(CardRarity Rarity, int Weight)> NeutralRarityWeights { get; } =
        Array.AsReadOnly<(CardRarity, int)>([(CardRarity.Uncommon, 75), (CardRarity.Rare, 25)]);

    private static IEnumerable<CardModel> Eligible(IEnumerable<CardModel> cards, CardMultiplayerConstraint constraint) => cards
        .Where(c => c.Rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare)
        .Where(c => constraint != CardMultiplayerConstraint.MultiplayerOnly || c.MultiplayerConstraint != CardMultiplayerConstraint.SingleplayerOnly)
        .Where(c => constraint != CardMultiplayerConstraint.SingleplayerOnly || c.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly)
        .DistinctBy(c => c.Id).OrderBy(c => c.Id.Entry, StringComparer.Ordinal);

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
