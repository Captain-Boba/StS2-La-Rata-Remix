using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

// RelicModel requires a Player owner. Enemy adaptations use native commands and
// a native power amount (bit mask), included in combat checksums.
public sealed class RoyalRegaliaPower : CustomPowerModel
{
    internal const int RelicCount = 10;
    internal static RelicModel[] Relics => [ModelDb.Relic<Vajra>(), ModelDb.Relic<OddlySmoothStone>(), ModelDb.Relic<Anchor>(), ModelDb.Relic<BronzeScales>(), ModelDb.Relic<HornCleat>(), ModelDb.Relic<CaptainsWheel>(), ModelDb.Relic<Orichalcum>(), ModelDb.Relic<MercuryHourglass>(), ModelDb.Relic<Lantern>(), ModelDb.Relic<BagOfPreparation>(), ModelDb.Relic<HappyFlower>(), ModelDb.Relic<BagOfMarbles>(), ModelDb.Relic<TungstenRod>(), ModelDb.Relic<SelfFormingClay>(), ModelDb.Relic<LetterOpener>(), ModelDb.Relic<ArtOfWar>(), ModelDb.Relic<CentennialPuzzle>(), ModelDb.Relic<Nunchaku>()];
    internal static string[] Effects => EnglishEffects.Select((text, i) => RoyalBossText.Text("relic_" + i, text)).ToArray();
    internal static readonly string[] EnglishEffects =
    [
        "Vajra: Start with 1 Strength.", "Oddly Smooth Stone: Start with 1 Dexterity.",
        "Anchor: Start with 10 base Block.", "Bronze Scales: Start with 3 Thorns.",
        "Horn Cleat: Gain 14 base Block on enemy turn 2.", "Captain's Wheel: Gain 18 base Block on enemy turn 3.",
        "Orichalcum: Gain 6 base Block at the end of an enemy turn if no Block remains.",
        "Mercury Hourglass: Deal 3 damage to every player at the start of each enemy turn.",
        "Lantern: The first royal hand has 1 additional Energy.",
        "Bag of Preparation: Draw 2 additional cards in the first royal hand.",
        "Happy Flower: Every third royal hand has 1 additional Energy.",
        "Bag of Marbles: Apply 1 Vulnerable to every player at combat start.",
        "Tungsten Rod: Reduce each instance of HP loss by 1.",
        "Self-Forming Clay: After taking unblocked damage, gain 3 Block after the next block clear.",
        "Letter Opener: After playing 3 royal Skills in one hand, deal 5 damage to every player. At most once per hand.",
        "Art of War: After a hand without an Attack card, reserve 1 Energy for the next royal hand. The fallback monster attack does not count as a card.",
        "Centennial Puzzle: The first time La Rata takes unblocked damage this combat, reserve 3 extra cards for the next royal hand. Self-damage can trigger this.",
        "Nunchaku: After every 10 royal Attack cards, reserve 1 Energy for the next royal hand. Multiple hits count as one card."
    ];
    internal static int Choose(Rng rng) => ChaosSelections.Distinct(RelicCount, EnglishEffects.Length, n => rng.NextInt(n)).Aggregate(0, (mask, i) => mask | (1 << i));
    internal static string Describe(int mask)
    {
        var effects = Effects;
        return string.Join("\n", Enumerable.Range(0, effects.Length).Where(i => (mask & (1 << i)) != 0).Select(i => effects[i]));
    }
    private bool Has(int i) => (Amount & (1 << i)) != 0;
    private int skillsThisHand, attackCards;
    private bool puzzleUsed;
    internal string Progress => $"skills={skillsThisHand};attacks={attackCards};puzzle={puzzleUsed}";
    internal void BeginRoyalHand() => skillsThisHand = 0;
    internal async Task AfterRoyalCard(int card)
    {
        if (!Owner.IsAlive || CombatManager.Instance.IsEnding || Owner.Monster is not RatKing king) return;
        if (RoyalDeck.IsAttack(card))
        {
            attackCards++;
            if (Has(17) && attackCards % 10 == 0) await king.ReserveRelic(17, 0, 1);
        }
        else if (!RoyalDeck.IsPower(card))
        {
            skillsThisHand++;
            if (Has(14) && skillsThisHand == 3)
                await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), RoyalBossEffects.Players(king), 5, ValueProp.Unpowered, Owner, null);
        }
    }
    internal async Task EndRoyalHand(bool playedAttack)
    {
        if (Has(15) && !playedAttack && Owner.IsAlive && !CombatManager.Instance.IsEnding && Owner.Monster is RatKing king)
            await king.ReserveRelic(15, 0, 1);
    }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => System.Numerics.BitOperations.PopCount((uint)Amount);
    public override string? CustomPackedIconPath => ModelDb.Relic<CaptainsWheel>().IconPath;
    public override string? CustomBigIconPath => CustomPackedIconPath;
    public override List<(string, string)> Localization => [];
    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource) =>
        target == Owner && Has(12) ? Math.Max(0, amount - 1) : amount;
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner && Owner.IsAlive && CombatManager.Instance.IsInProgress && result.UnblockedDamage > 0 && Has(13))
            await PowerCmd.Apply<SelfFormingClayPower>(choiceContext, Owner, 3, Owner, null);
        if (target == Owner && Owner.IsAlive && CombatManager.Instance.IsInProgress && result.UnblockedDamage > 0 && Has(16) && !puzzleUsed && Owner.Monster is RatKing king)
        {
            puzzleUsed = true;
            await king.ReserveRelic(16, 3, 0);
        }
    }
    public override LocString Description { get { var text = base.Description; text.Add("Relics", Describe(Amount)); return text; } }
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!Owner.IsAlive) return;
        if (side != CombatSide.Enemy || !participants.Contains(Owner)) return;
        if (combatState.RoundNumber == 2 && Has(4)) await CreatureCmd.GainBlock(Owner, 14, ValueProp.Move, null);
        if (combatState.RoundNumber == 3 && Has(5)) await CreatureCmd.GainBlock(Owner, 18, ValueProp.Move, null);
        if (Has(7))
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), combatState.Players.Select(p => p.Creature).Where(c => c.IsAlive), 3, ValueProp.Unpowered, Owner, null);
    }
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy && Owner.IsAlive && participants.Contains(Owner) && Has(6) && Owner.Block == 0)
            await CreatureCmd.GainBlock(Owner, 6, ValueProp.Move, null);
    }
}
