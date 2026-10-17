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
    internal static readonly string[] Names = ["Royal Tax", "Scepter Storm", "Off With Their Heads", "Sewer Alchemy", "Borrowed Tomorrow", "Royal Flush", "Stolen Aegis", "Blood Contract", "Exposed Crown", "Emerald Tempest"];
    internal static readonly string[] Descriptions =
    [
        "Deal 12 damage to every player.",
        "Deal 7 damage to every player 3 times.",
        "Deal 34 damage to every player. After all attacks, apply 2 Vulnerable to every player.",
        "Double each player's Poison and add 4 more, up to 14 total Poison.",
        "Apply 10 Doom and 1 Weak to every player. Doom kills at the end of a turn if it reaches current HP.",
        "Shuffle 2 Dazed into each player's draw pile. Add 1 Burn to each player's discard pile.",
        "Gain 24 base Block and 1 Buffer. Block and Buffer use native co-op scaling. Does not stack existing Buffer.",
        "Lose up to 12 HP, without killing La Rata. Give every living member of the royal court 2 Strength before attacking.",
        "Gain 16 base Block. Apply 2 Vulnerable to La Rata. An opening for your next turn!",
        "Deal 5 damage to every player 4 times. Add 1 Burn to each player's discard pile."
    ];
    internal int Index { get; }
    protected RoyalCard(int index) : base(RoyalDeck.Costs[index], RoyalDeck.IsAttack(index) ? CardType.Attack : index is 6 or 7 ? CardType.Power : CardType.Skill, CardRarity.Token, TargetType.None, false, false)
    {
        Index = index;
        CustomContentDictionary.RegisterType(GetType());
    }
    public override int MaxUpgradeLevel => 0;
    public override CardPoolModel Pool => ModelDb.CardPool<RoyalCardPool>();
    public override Material? CreateCustomBannerMaterial => RoyalCardStyle.Banner();
    public override string? CustomPortraitPath => "res://LaRata/images/rat_king.png";
    public override List<(string, string)> Localization => [("title", Names[Index]), ("description", Descriptions[Index])];
    internal static RoyalCard[] All => [ModelDb.Card<RoyalTax>(), ModelDb.Card<ScepterStorm>(), ModelDb.Card<OffWithTheirHeads>(), ModelDb.Card<SewerAlchemy>(), ModelDb.Card<BorrowedTomorrow>(), ModelDb.Card<RoyalFlush>(), ModelDb.Card<StolenAegis>(), ModelDb.Card<BloodContract>(), ModelDb.Card<ExposedCrown>(), ModelDb.Card<EmeraldTempest>()];

    internal IEnumerable<AbstractIntent> GetIntents(RatKing king) => Index switch
    {
        0 => [new SingleAttackIntent(() => 12 + king.PendingStrength)],
        1 => [new RoyalMultiAttackIntent(() => 7 + king.PendingStrength, 3)],
        2 => [new SingleAttackIntent(() => 34 + king.PendingStrength), new DebuffIntent()],
        5 => [new StatusIntent(3)],
        6 or 8 => [new DefendIntent(), new BuffIntent()],
        7 => [new BuffIntent()],
        9 => [new RoyalMultiAttackIntent(() => 5 + king.PendingStrength, 4), new StatusIntent(1)],
        _ => [new DebuffIntent()]
    };

    internal async Task Resolve(RatKing king, IReadOnlyList<Creature> targets)
    {
        var context = new ThrowingPlayerChoiceContext();
        var living = targets.Where(c => c.IsAlive).ToArray();
        var owner = king.Creature;
        if (!RoyalDeck.IsAttack(Index)) await CreatureCmd.TriggerAnim(owner, "Cast", .4f);
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
                king.PendingStrength = 0;
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
        }
        async Task Attack(int damage, int hits) => await DamageCmd.Attack(damage).FromMonster(king).WithHitCount(hits).WithAttackerAnim("Attack", .3f).Execute(context);
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
