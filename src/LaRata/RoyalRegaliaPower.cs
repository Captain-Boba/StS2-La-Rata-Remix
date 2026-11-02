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
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

// RelicModel requires a Player owner. Enemy adaptations use native commands and
// a native power amount (bit mask), included in combat checksums.
public sealed class RoyalRegaliaPower : CustomPowerModel
{
    internal const int RelicCount = 10;
    // Retained relics keep their old bit positions. Slots 8/10 are explicit replacements.
    internal static RelicModel[] Relics => [ModelDb.Relic<Vajra>(), ModelDb.Relic<OddlySmoothStone>(), ModelDb.Relic<Anchor>(), ModelDb.Relic<BronzeScales>(), ModelDb.Relic<HornCleat>(), ModelDb.Relic<CaptainsWheel>(), ModelDb.Relic<Orichalcum>(), ModelDb.Relic<MercuryHourglass>(), ModelDb.Relic<Akabeko>(), ModelDb.Relic<BagOfPreparation>(), ModelDb.Relic<RedMask>(), ModelDb.Relic<BagOfMarbles>(), ModelDb.Relic<TungstenRod>(), ModelDb.Relic<SelfFormingClay>(), ModelDb.Relic<LetterOpener>(), ModelDb.Relic<ArtOfWar>(), ModelDb.Relic<CentennialPuzzle>(), ModelDb.Relic<Nunchaku>(), ModelDb.Relic<OrnamentalFan>(), ModelDb.Relic<Kunai>(), ModelDb.Relic<Shuriken>(), ModelDb.Relic<ToughBandages>(), ModelDb.Relic<Pocketwatch>(), ModelDb.Relic<IntimidatingHelmet>(), ModelDb.Relic<Sai>()];
    internal static string[] Effects => EnglishEffects.Select((text, i) => RoyalBossText.Text("relic_" + i, text)).ToArray();
    internal static readonly string[] EnglishEffects =
    [
        "Vajra: Start with 1 Strength.", "Oddly Smooth Stone: Start with 1 Dexterity.",
        "Anchor: Start with 10 base Block.", "Bronze Scales: Start with 3 Thorns.",
        "Horn Cleat: Gain 14 base Block on enemy turn 2.", "Captain's Wheel: Gain 18 base Block on enemy turn 3.",
        "Orichalcum: Gain 6 base Block at the end of an enemy turn if no Block remains.",
        "Mercury Hourglass: Deal 3 damage to every player at the start of each enemy turn.",
        "Akabeko: Start with 4 Vigor.",
        "Bag of Preparation: Draw 2 additional cards in the first royal hand.",
        "Red Mask: Apply 1 Weak to every player at combat start.",
        "Bag of Marbles: Apply 1 Vulnerable to every player at combat start.",
        "Tungsten Rod: Reduce each instance of HP loss by 1.",
        "Self-Forming Clay: After taking unblocked damage, gain 3 Block after the next block clear.",
        "Letter Opener: After playing 3 royal Skills in one hand, deal 5 damage to every player. At most once per hand.",
        "Art of War: After a hand without an Attack card, reserve 1 Energy for the next royal hand. The fallback monster attack does not count as a card.",
        "Centennial Puzzle: The first time La Rata takes unblocked damage this combat, reserve 3 extra cards for the next royal hand. Self-damage can trigger this.",
        "Nunchaku: After every 10 royal Attack cards, reserve 1 Energy for the next royal hand. Multiple hits count as one card.",
        "Ornamental Fan: After the third royal Attack card in a hand, gain 4 Block. Once per hand.",
        "Kunai: After the third royal Attack card in a hand, gain 1 Dexterity. Once per hand, at most 3 Dexterity per combat.",
        "Shuriken: After a hand with 3 or more Attacks, gain 1 Strength. Max 2 per combat.",
        "Tough Bandages: When a royal card discards another card, gain 3 Block. Max 6 per hand.",
        "Pocketwatch: After a hand with at most 3 cards played, draw 1 extra card next hand.",
        "Intimidating Helmet: Before playing a royal card costing at least 2 Energy, gain 3 Block. At most twice per hand.",
        "Sai: Gain 3 Block at the start of each enemy turn."
    ];
    internal static int Choose(Rng rng) => ChaosSelections.Distinct(RelicCount, EnglishEffects.Length, n => rng.NextInt(n)).Aggregate(0, (mask, i) => mask | (1 << i));
    internal static string Describe(int mask)
    {
        var effects = Effects;
        return string.Join("\n", Enumerable.Range(0, effects.Length).Where(i => (mask & (1 << i)) != 0).Select(i => effects[i]));
    }
    internal static IEnumerable<AbstractIntent> HandIntents(RatKing king, int[] plan)
    {
        int mask = king.RegaliaMask, attacks = plan.Count(RoyalDeck.IsAttack);
        var power = king.Creature.GetPower<RoyalRegaliaPower>();
        bool Selected(int i) => (mask & (1 << i)) != 0;
        var defense = new List<int>();
        var buffs = new List<int>();
        if (Selected(18) && attacks >= 3) defense.Add(18);
        if (Selected(21) && plan.Any(i => i is 32 or 38 or 43 && king.Deck!.HasMaterial(i))) defense.Add(21);
        if (Selected(23) && plan.Any(i => RoyalDeck.Costs[i] >= 2)) defense.Add(23);
        if (Selected(19) && attacks >= 3 && (power?.kunaiGranted ?? 0) < 3) buffs.Add(19);
        if (Selected(20) && attacks >= 3 && (power?.shurikenGranted ?? 0) < 2) buffs.Add(20);
        if (Selected(22) && plan.Length <= 3) buffs.Add(22);
        if (defense.Count > 0) yield return new RoyalRelicDefenseIntent(defense.ToArray());
        if (buffs.Count > 0) yield return new RoyalRelicBuffIntent(buffs.ToArray());
    }
    internal static LocString IntentText(int[] indices)
    {
        var text = new LocString("monsters", ModelDb.Monster<RatKing>().Id.Entry + ".royal_status.relic_intent");
        var effects = Effects;
        text.Add("Relics", string.Join("\n", indices.Select(i => effects[i])));
        return text;
    }
    private bool Has(int i) => (Amount & (1 << i)) != 0;
    private int skillsThisHand, attackCards, attacksThisHand, cardsThisHand, kunaiGranted, shurikenGranted, bandageGrants, helmetGrants;
    private bool openingApplied, newEndResolved;
    private bool puzzleUsed;
    internal string Progress => $"skills={skillsThisHand};attacks={attackCards};puzzle={puzzleUsed};handAttacks={attacksThisHand};handCards={cardsThisHand};kunai={kunaiGranted};shuriken={shurikenGranted};bandages={bandageGrants};helmet={helmetGrants};opening={openingApplied};end={newEndResolved}";
    internal void BeginRoyalHand()
    {
        skillsThisHand = attacksThisHand = cardsThisHand = bandageGrants = helmetGrants = 0;
        newEndResolved = false;
    }
    internal async Task ApplyOpening()
    {
        if (openingApplied || !Owner.IsAlive) return;
        openingApplied = true;
        var context = new ThrowingPlayerChoiceContext();
        if (Has(8)) await PowerCmd.Apply<VigorPower>(context, Owner, 4, Owner, null);
        if (Has(10)) await PowerCmd.Apply<WeakPower>(context, Owner.CombatState!.Players.OrderBy(p => p.NetId).Select(p => p.Creature).Where(c => c.IsAlive), 1, Owner, null);
    }
    internal async Task BeforeRoyalCard(int card)
    {
        if (!Owner.IsAlive || CombatManager.Instance.IsEnding || !Has(23) || RoyalDeck.Costs[card] < 2 || helmetGrants >= 2) return;
        helmetGrants++;
        await CreatureCmd.GainBlock(Owner, 3, ValueProp.Unpowered, null);
    }
    internal async Task AfterRoyalDiscard()
    {
        if (!Owner.IsAlive || CombatManager.Instance.IsEnding || !Has(21) || bandageGrants >= 2) return;
        bandageGrants++;
        await CreatureCmd.GainBlock(Owner, 3, ValueProp.Unpowered, null);
    }
    internal async Task AfterRoyalCard(int card)
    {
        if (!Owner.IsAlive || CombatManager.Instance.IsEnding || Owner.Monster is not RatKing king) return;
        cardsThisHand++;
        if (RoyalDeck.IsAttack(card))
        {
            attackCards++;
            attacksThisHand++;
            if (Has(17) && attackCards % 10 == 0) await king.ReserveRelic(17, 0, 1);
            if (attacksThisHand == 3)
            {
                if (Has(18)) await CreatureCmd.GainBlock(Owner, 4, ValueProp.Unpowered, null);
                if (Has(19) && kunaiGranted < 3)
                {
                    kunaiGranted++;
                    await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null);
                }
            }
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
        if (newEndResolved || !Owner.IsAlive || CombatManager.Instance.IsEnding || Owner.Monster is not RatKing owner) return;
        newEndResolved = true;
        if (Has(20) && attacksThisHand >= 3 && shurikenGranted < 2)
        {
            shurikenGranted++;
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null);
        }
        if (Has(22) && cardsThisHand <= 3) await owner.ReserveRelic(22, 1, 0);
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
        if (Has(24)) await CreatureCmd.GainBlock(Owner, 3, ValueProp.Unpowered, null);
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

internal sealed class RoyalRelicDefenseIntent(int[] indices) : DefendIntent
{
    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner) => RoyalRegaliaPower.IntentText(indices);
}

internal sealed class RoyalRelicBuffIntent(int[] indices) : BuffIntent
{
    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner) => RoyalRegaliaPower.IntentText(indices);
}
