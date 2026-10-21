using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

// Real native card models provide the standard card presentation and energy costs.
// Their enemy effects execute through MonsterModel/commands, not player-only OnPlayWrapper.
public abstract class RoyalCard : CustomCardModel
{
    internal static readonly string[] Names = ["Royal Tax", "Scepter Storm", "Off With Their Heads", "Sewer Alchemy", "Borrowed Tomorrow", "Royal Flush", "Stolen Aegis", "Blood Contract", "Exposed Crown", "Emerald Tempest", "Grave Levy", "Funeral Procession", "Bone Treasury", "Crown of Night", "Rotting Tribute", "Stolen Hour", "Drowned Court", "Green Guillotine", "Rattling Arsenal", "Royal Pardon", "Black Tithe", "Plague of Crowns", "Kneel Before the Crown", "Soul Foreclosure", "Nightfall Decree"];
    internal static readonly string[] Art = ["royal_tax", "scepter_storm", "off_with_their_heads", "sewer_alchemy", "borrowed_tomorrow", "royal_flush", "stolen_aegis", "blood_contract", "exposed_crown", "emerald_tempest", "grave_levy", "funeral_procession", "bone_treasury", "crown_of_night", "rotting_tribute", "stolen_hour", "drowned_court", "green_guillotine", "rattling_arsenal", "royal_pardon", "black_tithe", "plague_of_crowns", "kneel_before_the_crown", "soul_foreclosure", "nightfall_decree"];
    internal static readonly string[] Descriptions =
    [
        "Deal 12 damage to every player.",
        "Deal 7 damage to every player 3 times.",
        "Deal 34 damage to every player. After all attacks, apply 2 Vulnerable to every player.",
        "Double each player's Poison and add 4 more, up to 14 total Poison.",
        "Apply 10 Doom and 1 Weak to every player. Doom kills at the end of a turn if it reaches current HP.",
        "Shuffle 2 Dazed into each player's draw pile. Add 1 Burn to each player's discard pile.",
        "Gain 24 base Block and 1 Buffer. Block and Buffer use native co-op scaling. Does not stack existing Buffer.",
        "The caster loses up to 12 HP, without dying. Give every living ally 2 Strength before attacking.",
        "The caster gains 16 base Block and 2 Vulnerable. An opening for your next turn!",
        "Deal 5 damage to every player 4 times. Add 1 Burn to each player's discard pile.",
        "Deal 18 damage to every player. Restore 24 HP to the caster.",
        "Deal 6 damage to every player 3 times. Apply 1 Weak to every player.",
        "Give every living court member 3 base Plating, up to 14 base Plating.",
        "Gain 30 base Block and 2 base Buffer. Does not add Buffer if already protected.",
        "Apply 5 Poison and 2 Frail to every player.",
        "Shuffle 2 Dazed into each player's draw pile. The caster gains 12 base Block.",
        "Heal every living court member for 5% of their maximum HP. Give them 2 base Plating, up to 14 base Plating.",
        "Deal 40 damage to every player. The caster gains 2 Vulnerable.",
        "Deal 4 damage to every player 5 times. Shuffle 1 Dazed into each player's draw pile.",
        "Remove Weak and Frail from every living court member. The caster gains 10 base Block.",
        "Remove ALL Block from every player. Apply 2 Weak and 2 Frail to every player.",
        "Apply 8 Poison and 12 Doom to every player. Add 2 Burns to each player's discard pile.",
        "Give every living court member 30 base Block and 3 Strength before attacking.",
        "Deal 26 damage to every player. Heal every living court member for 8% of their maximum HP.",
        "Give every living court member 2 Ritual, up to 6 total. Ritual grants Strength at the end of their turns, starting next turn."
    ];
    internal int Index { get; }
    protected RoyalCard(int index) : base(RoyalDeck.Costs[index], RoyalDeck.IsAttack(index) ? CardType.Attack : index is 6 or 7 or 24 ? CardType.Power : CardType.Skill, CardRarity.Token, TargetType.None, false, false)
    {
        Index = index;
        CustomContentDictionary.RegisterType(GetType());
    }
    public override int MaxUpgradeLevel => 0;
    public override CardPoolModel Pool => ModelDb.CardPool<RoyalCardPool>();
    public override Material? CreateCustomBannerMaterial => RoyalCardStyle.Banner();
    public override Material? CreateCustomFrameMaterial => RoyalCardStyle.Frame();
    public override string? CustomPortraitPath => $"res://LaRata/images/cards/{Art[Index]}.png";
    public override List<(string, string)> Localization => [("title", Names[Index]), ("description", Descriptions[Index])];
    internal static RoyalCard[] All => [ModelDb.Card<RoyalTax>(), ModelDb.Card<ScepterStorm>(), ModelDb.Card<OffWithTheirHeads>(), ModelDb.Card<SewerAlchemy>(), ModelDb.Card<BorrowedTomorrow>(), ModelDb.Card<RoyalFlush>(), ModelDb.Card<StolenAegis>(), ModelDb.Card<BloodContract>(), ModelDb.Card<ExposedCrown>(), ModelDb.Card<EmeraldTempest>(), ModelDb.Card<GraveLevy>(), ModelDb.Card<FuneralProcession>(), ModelDb.Card<BoneTreasury>(), ModelDb.Card<CrownOfNight>(), ModelDb.Card<RottingTribute>(), ModelDb.Card<StolenHour>(), ModelDb.Card<DrownedCourt>(), ModelDb.Card<GreenGuillotine>(), ModelDb.Card<RattlingArsenal>(), ModelDb.Card<RoyalPardon>(), ModelDb.Card<BlackTithe>(), ModelDb.Card<PlagueOfCrowns>(), ModelDb.Card<KneelBeforeTheCrown>(), ModelDb.Card<SoulForeclosure>(), ModelDb.Card<NightfallDecree>()];

    internal IEnumerable<AbstractIntent> GetIntents(RatKing king) => Index switch
    {
        0 => [new SingleAttackIntent(() => 12 + king.PendingStrength)],
        1 => [new RoyalMultiAttackIntent(() => 7 + king.PendingStrength, 3)],
        2 => [new SingleAttackIntent(() => 34 + king.PendingStrength), new DebuffIntent()],
        5 => [new StatusIntent(3)],
        6 or 8 => [new DefendIntent(), new BuffIntent()],
        7 => [new BuffIntent()],
        9 => [new RoyalMultiAttackIntent(() => 5 + king.PendingStrength, 4), new StatusIntent(1)],
        10 => [new SingleAttackIntent(() => 18 + king.PendingStrength), new BuffIntent()],
        11 => [new RoyalMultiAttackIntent(() => 6 + king.PendingStrength, 3), new DebuffIntent()],
        12 or 13 or 16 or 19 => [new DefendIntent(), new BuffIntent()],
        15 => [new StatusIntent(2), new DefendIntent()],
        17 => [new SingleAttackIntent(() => 40 + king.PendingStrength)],
        18 => [new RoyalMultiAttackIntent(() => 4 + king.PendingStrength, 5), new StatusIntent(1)],
        20 => [new RoyalEdictIntent("black_tithe")],
        21 => [new RoyalEdictIntent("plague_of_crowns"), new StatusIntent(2)],
        22 => [new DefendIntent(), new BuffIntent()],
        23 => [new SingleAttackIntent(() => 26 + king.PendingStrength), new HealIntent()],
        24 => [new BuffIntent()],
        _ => [new DebuffIntent()]
    };

    internal async Task Resolve(MonsterModel king, IReadOnlyList<Creature> targets)
    {
        var context = new ThrowingPlayerChoiceContext();
        var living = targets.Where(c => c.IsAlive).ToArray();
        var owner = king.Creature;
        if (king is RatKing && !RoyalDeck.IsAttack(Index)) await CreatureCmd.TriggerAnim(owner, "Cast", .4f);
        switch (Index)
        {
            case 0: await Attack(12, 1); break;
            case 1: await Attack(7, 3); break;
            case 2: await Attack(34, 1); break; // Vulnerable is deferred until every announced attack finishes.
            case 3:
                foreach (var target in living)
                {
                    int poison = target.GetPowerAmount<PoisonPower>();
                    int add = Math.Max(0, Math.Min(14, poison * 2 + 4) - poison);
                    if (add > 0) await PowerCmd.Apply<PoisonPower>(context, target, add, owner, null);
                }
                break;
            case 4:
                await PowerCmd.Apply<DoomPower>(context, living, 10, owner, null);
                await PowerCmd.Apply<WeakPower>(context, living, 1, owner, null);
                break;
            case 5:
                await CardPileCmd.AddToCombatAndPreview<Dazed>(living, PileType.Draw, 2, null, CardPilePosition.Random);
                await CardPileCmd.AddToCombatAndPreview<Burn>(living, PileType.Discard, 1, null);
                break;
            case 6:
                await CreatureCmd.GainBlock(owner, 24, ValueProp.Move, null);
                if (owner.GetPowerAmount<BufferPower>() == 0) await PowerCmd.Apply<BufferPower>(context, owner, 1, owner, null);
                break;
            case 7:
                await CreatureCmd.Damage(context, owner, Math.Min(12, Math.Max(0, owner.CurrentHp - 1)), ValueProp.Unblockable | ValueProp.Unpowered, owner, null);
                if (king is RatKing ratKing) ratKing.PendingStrength -= 2;
                await PowerCmd.Apply<StrengthPower>(context, king.CombatState.Enemies.Where(c => c.IsAlive), 2, owner, null);
                break;
            case 8:
                await CreatureCmd.GainBlock(owner, 16, ValueProp.Move, null);
                await PowerCmd.Apply<VulnerablePower>(context, owner, 2, owner, null);
                break;
            case 9:
                await Attack(5, 4);
                await CardPileCmd.AddToCombatAndPreview<Burn>(living.Where(c => c.IsAlive), PileType.Discard, 1, null);
                break;
            case 10:
                await Attack(18, 1);
                if (owner.IsAlive) await CreatureCmd.Heal(owner, 24);
                break;
            case 11:
                await Attack(6, 3);
                await PowerCmd.Apply<WeakPower>(context, living.Where(c => c.IsAlive), 1, owner, null);
                break;
            case 12: await PlateCourt(3); break;
            case 13:
                await CreatureCmd.GainBlock(owner, 30, ValueProp.Move, null);
                if (owner.GetPowerAmount<BufferPower>() == 0) await PowerCmd.Apply<BufferPower>(context, owner, 2, owner, null);
                break;
            case 14:
                await PowerCmd.Apply<PoisonPower>(context, living, 5, owner, null);
                await PowerCmd.Apply<FrailPower>(context, living, 2, owner, null);
                break;
            case 15:
                await CardPileCmd.AddToCombatAndPreview<Dazed>(living, PileType.Draw, 2, null, CardPilePosition.Random);
                await CreatureCmd.GainBlock(owner, 12, ValueProp.Move, null);
                break;
            case 16:
                foreach (var ally in king.CombatState.Enemies.Where(c => c.IsAlive).ToArray())
                    await CreatureCmd.Heal(ally, Math.Max(1, (int)(ally.MaxHp * .05m)));
                await PlateCourt(2);
                break;
            case 17:
                await Attack(40, 1);
                if (owner.IsAlive) await PowerCmd.Apply<VulnerablePower>(context, owner, 2, owner, null);
                break;
            case 18:
                await Attack(4, 5);
                await CardPileCmd.AddToCombatAndPreview<Dazed>(living.Where(c => c.IsAlive), PileType.Draw, 1, null, CardPilePosition.Random);
                break;
            case 19:
                foreach (var ally in king.CombatState.Enemies.Where(c => c.IsAlive).ToArray())
                    foreach (var power in ally.Powers.Where(p => p is WeakPower or FrailPower).ToArray())
                        await PowerCmd.Remove(power);
                await CreatureCmd.GainBlock(owner, 10, ValueProp.Move, null);
                break;
            case 20:
                foreach (var target in living) await CreatureCmd.LoseBlock(target, target.Block);
                await PowerCmd.Apply<WeakPower>(context, living, 2, owner, null);
                await PowerCmd.Apply<FrailPower>(context, living, 2, owner, null);
                break;
            case 21:
                await PowerCmd.Apply<PoisonPower>(context, living, 8, owner, null);
                await PowerCmd.Apply<DoomPower>(context, living, 12, owner, null);
                await CardPileCmd.AddToCombatAndPreview<Burn>(living, PileType.Discard, 2, null);
                break;
            case 22:
                if (king is RatKing sovereign) sovereign.PendingStrength -= 3;
                foreach (var ally in king.CombatState.Enemies.Where(c => c.IsAlive).OrderBy(c => c.CombatId).ToArray())
                {
                    await CreatureCmd.GainBlock(ally, 30, ValueProp.Move, null);
                    await PowerCmd.Apply<StrengthPower>(context, ally, 3, owner, null);
                }
                break;
            case 23:
                await Attack(26, 1);
                foreach (var ally in king.CombatState.Enemies.Where(c => c.IsAlive).OrderBy(c => c.CombatId).ToArray())
                    await CreatureCmd.Heal(ally, Math.Ceiling(ally.MaxHp * .08m));
                break;
            case 24:
                foreach (var ally in king.CombatState.Enemies.Where(c => c.IsAlive).OrderBy(c => c.CombatId).ToArray())
                {
                    int amount = Math.Min(2, Math.Max(0, 6 - ally.GetPowerAmount<RitualPower>()));
                    if (amount > 0) await PowerCmd.Apply<RitualPower>(context, ally, amount, owner, null);
                }
                break;
        }
        async Task Attack(int damage, int hits) => await DamageCmd.Attack(damage).FromMonster(king).WithHitCount(hits).WithAttackerAnim("Attack", .3f).Execute(context);
        async Task PlateCourt(int amount)
        {
            int scale = Math.Max(1, king.CombatState.Players.Count * 2 - 1);
            foreach (var ally in king.CombatState.Enemies.Where(c => c.IsAlive).ToArray())
            {
                int room = Math.Max(0, 14 - (int)Math.Ceiling(ally.GetPowerAmount<PlatingPower>() / (decimal)scale));
                if (room > 0) await PowerCmd.Apply<PlatingPower>(context, ally, Math.Min(amount, room), owner, null);
            }
        }
    }
}

internal sealed class RoyalMultiAttackIntent : MultiAttackIntent
{
    internal RoyalMultiAttackIntent(Func<decimal> damage, int hits) : base(0, hits) => DamageCalc = damage;
}

public sealed class RoyalTax() : RoyalCard(0);
public sealed class ScepterStorm() : RoyalCard(1);
public sealed class OffWithTheirHeads() : RoyalCard(2);
public sealed class SewerAlchemy() : RoyalCard(3);
public sealed class BorrowedTomorrow() : RoyalCard(4);
public sealed class RoyalFlush() : RoyalCard(5);
public sealed class StolenAegis() : RoyalCard(6);
public sealed class BloodContract() : RoyalCard(7);
public sealed class ExposedCrown() : RoyalCard(8);
public sealed class EmeraldTempest() : RoyalCard(9);
public sealed class GraveLevy() : RoyalCard(10);
public sealed class FuneralProcession() : RoyalCard(11);
public sealed class BoneTreasury() : RoyalCard(12);
public sealed class CrownOfNight() : RoyalCard(13);
public sealed class RottingTribute() : RoyalCard(14);
public sealed class StolenHour() : RoyalCard(15);
public sealed class DrownedCourt() : RoyalCard(16);
public sealed class GreenGuillotine() : RoyalCard(17);
public sealed class RattlingArsenal() : RoyalCard(18);
public sealed class RoyalPardon() : RoyalCard(19);
public sealed class BlackTithe() : RoyalCard(20);
public sealed class PlagueOfCrowns() : RoyalCard(21);
public sealed class KneelBeforeTheCrown() : RoyalCard(22);
public sealed class SoulForeclosure() : RoyalCard(23);
public sealed class NightfallDecree() : RoyalCard(24);

internal sealed class RoyalEdictIntent(string key) : DebuffIntent
{
    protected override MegaCrit.Sts2.Core.Localization.LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner) =>
        new("monsters", ModelDb.Monster<RatKing>().Id.Entry + ".edict." + key);
}
