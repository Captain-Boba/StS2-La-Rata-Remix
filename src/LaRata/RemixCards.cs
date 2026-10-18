using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

// Player-owned models use native targeting, costs, upgrade variables and power hooks.
// Boss prizes intentionally have no Pool attribute: only the royal booster awards them.
public abstract class RemixCard : CustomCardModel
{
    internal int Index { get; }
    internal bool IsBossPrize => Index >= 10;
    internal static readonly string[] Names = ["Grave Spark", "Umbral Stitch", "Sewer Pact", "Phantom Parade", "Borrowed Breath", "Crown Exchange", "Midnight Rehearsal", "Emerald Ward", "Second Shadow", "Open the Gates", "Scepter Storm", "Sewer Alchemy", "Stolen Aegis", "Blood Contract", "Emerald Tempest"];
    internal static readonly string[] Art = ["grave_spark", "umbral_stitch", "sewer_pact", "phantom_parade", "borrowed_breath", "crown_exchange", "midnight_rehearsal", "emerald_ward", "second_shadow", "open_the_gates", "scepter_storm", "sewer_alchemy", "stolen_aegis", "blood_contract", "emerald_tempest"];
    private static readonly int[] Costs = [0, 1, 1, 2, 1, 0, 1, 2, 2, 2, 2, 2, 2, 0, 3];
    private static readonly string[] Text =
    [
        "Gain {Energy:energyIcons()}. Draw {Cards:diff()} card.",
        "Gain {Block:diff()} Block. Gain {VigorPower:diff()} Vigor.",
        "Apply {PoisonPower:diff()} Poison and {DoomPower:diff()} Doom.",
        "Deal {Damage:diff()} damage to ALL enemies 4 times.",
        "Heal {Heal:diff()} HP. Draw {Cards:diff()} cards.",
        "Lose 3 HP. Draw {Cards:diff()} cards.",
        "Draw {Cards:diff()} cards. Discard 1 card.",
        "All living players gain {Block:diff()} Block and {Energy:energyIcons()}.",
        "Whenever you play a card, gain 1 Block.",
        "Apply {VulnerablePower:diff()} Vulnerable and {WeakPower:diff()} Weak to ALL enemies.",
        "Apply {WeakPower:diff()} Weak and {VulnerablePower:diff()} Vulnerable. Deal {Damage:diff()} damage.",
        "Double the enemy's Poison, then apply {PoisonPower:diff()} Poison.",
        "Gain {Block:diff()} Block and {BufferPower:diff()} Buffer.",
        "Lose 6 HP. All players gain {StrengthPower:diff()} Strength.",
        "Deal {Damage:diff()} damage to ALL enemies 4 times. Apply {PoisonPower:diff()} Poison to ALL enemies."
    ];
    protected RemixCard(int index) : base(Costs[index], index is 3 or 10 or 14 ? CardType.Attack : index == 8 ? CardType.Power : CardType.Skill,
        index is 0 or 4 or 8 or >= 10 ? CardRarity.Rare : CardRarity.Uncommon,
        index is 2 or 10 or 11 ? TargetType.AnyEnemy : index is 3 or 9 or 14 ? TargetType.AllEnemies : index is 7 or 13 ? TargetType.AllAllies : TargetType.Self, true, index < 10)
    { Index = index; if (index >= 10) CustomContentDictionary.RegisterType(GetType()); }
    public override CardPoolModel Pool => ModelDb.CardPool<ColorlessCardPool>();
    public override Material? CreateCustomFrameMaterial => RoyalCardStyle.Frame();
    public override Material? CreateCustomBannerMaterial => RoyalCardStyle.Banner();
    public override string? CustomPortraitPath => $"res://LaRata/images/cards/{Art[Index]}.png";
    public override List<(string, string)> Localization => [("title", Names[Index]), ("description", Text[Index])];
    public override bool GainsBlock => Index is 1 or 7 or 12;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => Index switch
    {
        1 => [HoverTipFactory.FromPower<VigorPower>()],
        2 => [HoverTipFactory.FromPower<PoisonPower>(), HoverTipFactory.FromPower<DoomPower>()],
        8 => [HoverTipFactory.Static(StaticHoverTip.Block)],
        9 or 10 => [HoverTipFactory.FromPower<VulnerablePower>(), HoverTipFactory.FromPower<WeakPower>()],
        11 or 14 => [HoverTipFactory.FromPower<PoisonPower>()],
        12 => [HoverTipFactory.FromPower<BufferPower>()],
        13 => [HoverTipFactory.FromPower<StrengthPower>()],
        _ => []
    };
    public override IEnumerable<CardKeyword> CanonicalKeywords => Index is 0 or 4 or 5 or 9 or 11 or 12 or 13 ? [CardKeyword.Exhaust] : [];
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(Index == 10 ? 10 : Index == 14 ? 5 : 3, ValueProp.Move),
        new BlockVar(Index == 12 ? 20 : Index == 7 ? 5 : 8, ValueProp.Move),
        new CardsVar(Index == 0 ? 1 : Index == 6 ? 3 : 2), new EnergyVar(1), new HealVar(3),
        new PowerVar<VigorPower>(4), new PowerVar<PoisonPower>(Index == 14 ? 3 : 5), new PowerVar<DoomPower>(5),
        new PowerVar<VulnerablePower>(Index == 10 ? 1 : 2), new PowerVar<WeakPower>(Index == 10 ? 1 : 2), new PowerVar<BufferPower>(1), new PowerVar<StrengthPower>(2)
    ];
    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        var self = Owner.Creature;
        var combat = CombatState!;
        switch (Index)
        {
            case 0:
                await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
                await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner); break;
            case 1:
                await CreatureCmd.GainBlock(self, DynamicVars.Block, play);
                await PowerCmd.Apply<VigorPower>(ctx, self, DynamicVars["VigorPower"].BaseValue, self, this); break;
            case 2:
                await PowerCmd.Apply<PoisonPower>(ctx, play.Target!, DynamicVars["PoisonPower"].BaseValue, self, this);
                await PowerCmd.Apply<DoomPower>(ctx, play.Target!, DynamicVars["DoomPower"].BaseValue, self, this); break;
            case 3: await Volley(4); break;
            case 4:
                await CreatureCmd.Heal(self, DynamicVars["Heal"].BaseValue);
                await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner); break;
            case 5:
                await CreatureCmd.Damage(ctx, self, 3, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
                if (self.IsAlive) await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner); break;
            case 6:
                await CardPileCmd.Draw(ctx, DynamicVars.Cards.BaseValue, Owner);
                await CardCmd.Discard(ctx, await CardSelectCmd.FromHandForDiscard(ctx, Owner, new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1), null, this)); break;
            case 7:
                foreach (var player in combat.Players.OrderBy(p => p.NetId).Where(p => p.Creature.IsAlive))
                {
                    await CreatureCmd.GainBlock(player.Creature, DynamicVars.Block, play);
                    await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, player);
                }
                break;
            case 8: await PowerCmd.Apply<AfterimagePower>(ctx, self, 1, self, this); break;
            case 9:
                await PowerCmd.Apply<VulnerablePower>(ctx, combat.HittableEnemies, DynamicVars["VulnerablePower"].BaseValue, self, this);
                await PowerCmd.Apply<WeakPower>(ctx, combat.HittableEnemies, DynamicVars["WeakPower"].BaseValue, self, this); break;
            case 10:
                await PowerCmd.Apply<WeakPower>(ctx, play.Target!, DynamicVars["WeakPower"].BaseValue, self, this);
                await PowerCmd.Apply<VulnerablePower>(ctx, play.Target!, DynamicVars["VulnerablePower"].BaseValue, self, this);
                await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(play.Target!).Execute(ctx); break;
            case 11:
                await PowerCmd.Apply<PoisonPower>(ctx, play.Target!, play.Target!.GetPowerAmount<PoisonPower>() + DynamicVars["PoisonPower"].BaseValue, self, this); break;
            case 12:
                await CreatureCmd.GainBlock(self, DynamicVars.Block, play);
                await PowerCmd.Apply<BufferPower>(ctx, self, DynamicVars["BufferPower"].BaseValue, self, this); break;
            case 13:
                await CreatureCmd.Damage(ctx, self, 6, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this);
                if (self.IsAlive) await PowerCmd.Apply<StrengthPower>(ctx, combat.Players.OrderBy(p => p.NetId).Select(p => p.Creature).Where(c => c.IsAlive), DynamicVars["StrengthPower"].BaseValue, self, this);
                break;
            case 14:
                await Volley(4);
                await PowerCmd.Apply<PoisonPower>(ctx, combat.HittableEnemies, DynamicVars["PoisonPower"].BaseValue, self, this); break;
        }
        async Task Volley(int hits) => await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).TargetingAllOpponents(combat).WithHitCount(hits).Execute(ctx);
    }
    protected override void OnUpgrade()
    {
        switch (Index)
        {
            case 0: DynamicVars.Energy.UpgradeValueBy(1); break;
            case 1: DynamicVars.Block.UpgradeValueBy(3); DynamicVars["VigorPower"].UpgradeValueBy(2); break;
            case 2: DynamicVars["PoisonPower"].UpgradeValueBy(2); DynamicVars["DoomPower"].UpgradeValueBy(2); break;
            case 3 or 14: DynamicVars.Damage.UpgradeValueBy(2); break;
            case 10: DynamicVars.Damage.UpgradeValueBy(10); DynamicVars["WeakPower"].UpgradeValueBy(1); DynamicVars["VulnerablePower"].UpgradeValueBy(1); break;
            case 4: DynamicVars["Heal"].UpgradeValueBy(3); DynamicVars.Cards.UpgradeValueBy(1); break;
            case 5 or 6: DynamicVars.Cards.UpgradeValueBy(1); break;
            case 7: DynamicVars.Block.UpgradeValueBy(5); DynamicVars.Energy.UpgradeValueBy(1); break;
            case 8 or 9: EnergyCost.UpgradeBy(-1); break;
            case 11: DynamicVars["PoisonPower"].UpgradeValueBy(3); break;
            case 12: DynamicVars["BufferPower"].UpgradeValueBy(1); break;
            case 13: DynamicVars["StrengthPower"].UpgradeValueBy(1); break;
        }
    }
    internal static CardModel[] Neutrals => [ModelDb.Card<Cleanse>(), ModelDb.Card<GraveSpark>(), ModelDb.Card<UmbralStitch>(), ModelDb.Card<SewerPact>(), ModelDb.Card<PhantomParade>(), ModelDb.Card<BorrowedBreath>(), ModelDb.Card<CrownExchange>(), ModelDb.Card<MidnightRehearsal>(), ModelDb.Card<EmeraldWard>(), ModelDb.Card<SecondShadow>(), ModelDb.Card<OpenTheGates>()];
    internal static CardModel[] BossPrizes => [ModelDb.Card<RoyalScepterPrize>(), ModelDb.Card<RoyalAlchemyPrize>(), ModelDb.Card<RoyalAegisPrize>(), ModelDb.Card<RoyalContractPrize>(), ModelDb.Card<RoyalTempestPrize>()];
}

[Pool(typeof(ColorlessCardPool))] public sealed class GraveSpark() : RemixCard(0);
[Pool(typeof(ColorlessCardPool))] public sealed class UmbralStitch() : RemixCard(1);
[Pool(typeof(ColorlessCardPool))] public sealed class SewerPact() : RemixCard(2);
[Pool(typeof(ColorlessCardPool))] public sealed class PhantomParade() : RemixCard(3);
[Pool(typeof(ColorlessCardPool))] public sealed class BorrowedBreath() : RemixCard(4);
[Pool(typeof(ColorlessCardPool))] public sealed class CrownExchange() : RemixCard(5);
[Pool(typeof(ColorlessCardPool))] public sealed class MidnightRehearsal() : RemixCard(6);
[Pool(typeof(ColorlessCardPool))] public sealed class EmeraldWard() : RemixCard(7);
[Pool(typeof(ColorlessCardPool))] public sealed class SecondShadow() : RemixCard(8);
[Pool(typeof(ColorlessCardPool))] public sealed class OpenTheGates() : RemixCard(9);
public sealed class RoyalScepterPrize() : RemixCard(10);
public sealed class RoyalAlchemyPrize() : RemixCard(11);
public sealed class RoyalAegisPrize() : RemixCard(12);
public sealed class RoyalContractPrize() : RemixCard(13);
public sealed class RoyalTempestPrize() : RemixCard(14);
