using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Hooks;

namespace LaRata;

public sealed class RatKing : CustomMonsterModel, ILocalizationProvider
{
    public override int MinInitialHp => 430;
    public override int MaxInitialHp => 430;
    public override string? CustomVisualPath => "res://LaRata/scenes/rat_king.tscn";
    public List<(string, string)> Localization =>
    [
        ("name", "La Rata"),
        ("moves.ROYAL_DECREE.title", "Royal Decree"),
        ("moves.TAX_COLLECTION.title", "Tax Collection"),
        ("moves.SCEPTER_STRIKES.title", "Scepter Strikes"),
        ("moves.EXPOSED_CROWN.title", "Exposed Crown"),
        ("decree.description", "Applies 2 Weak and 2 Frail to every living player."),
        ("crown.description", "Gains {Block} Block and applies 2 Vulnerable to itself. The crown is exposed: attack next turn!")
    ];
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var decree = new MoveState("ROYAL_DECREE", Decree, new RoyalDecreeIntent());
        var levy = new MoveState("TAX_COLLECTION", Levy, new SingleAttackIntent(20));
        var scepter = new MoveState("SCEPTER_STRIKES", Scepter, new MultiAttackIntent(7, 3));
        var crown = new MoveState("EXPOSED_CROWN", Crown, new ExposedCrownIntent());
        decree.FollowUpState = levy; levy.FollowUpState = scepter; scepter.FollowUpState = crown; crown.FollowUpState = decree;
        return new MonsterMoveStateMachine([decree, levy, scepter, crown], decree);
    }
    private async Task Decree(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", .4f);
        var context = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<WeakPower>(context, targets.Where(t => t.IsAlive), 2, Creature, null);
        await PowerCmd.Apply<FrailPower>(context, targets.Where(t => t.IsAlive), 2, Creature, null);
        ModEntry.Log.Info("RAT KING Royal Decree: 2 Weak, 2 Frail.");
    }
    private async Task Levy(IReadOnlyList<Creature> _) => await DamageCmd.Attack(20).FromMonster(this).WithAttackerAnim("Attack", .25f).WithHitFx("vfx/vfx_attack_blunt").Execute(null);
    private async Task Scepter(IReadOnlyList<Creature> _) => await DamageCmd.Attack(7).WithHitCount(3).FromMonster(this).WithAttackerAnim("Attack", .25f).WithHitFx("vfx/vfx_attack_slash").Execute(null);
    private async Task Crown(IReadOnlyList<Creature> _)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", .4f);
        await CreatureCmd.GainBlock(Creature, 25, ValueProp.Move, null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), Creature, 2, Creature, null);
        ModEntry.Log.Info("RAT KING Exposed Crown: 25 Block, self 2 Vulnerable; attack window.");
    }
}

internal sealed class RoyalDecreeIntent : DebuffIntent
{
    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner) => new("monsters", ModelDb.Monster<RatKing>().Id.Entry + ".decree.description");
}

internal sealed class ExposedCrownIntent : DefendIntent
{
    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
    {
        var description = new LocString("monsters", ModelDb.Monster<RatKing>().Id.Entry + ".crown.description");
        var block = owner.CombatState is { } combat
            ? Hook.ModifyBlock(combat, owner, 25, ValueProp.Move, null, null, out _)
            : 25;
        description.Add("Block", (int)Math.Max(0, block));
        return description;
    }
}

public sealed class RatKingEncounter : CustomEncounterModel, ILocalizationProvider
{
    public RatKingEncounter() : base(RoomType.Boss) { }
    public List<(string, string)> Localization => [("title", "La Rata")];
    public override bool IsValidForAct(ActModel act) => false; // Explicitly included only by RatDomain.
    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<RatKing>(), ModelDb.Monster<TwoTailedRat>()];
    public override string? CustomScenePath => "res://LaRata/scenes/rat_king_encounter.tscn";
    public override string BossNodePath => "res://LaRata/images/map_rat_king";
    public override string? CustomRunHistoryIconPath => "res://LaRata/images/rat_king.png";
    public override string? CustomRunHistoryIconOutlinePath => CustomRunHistoryIconPath;
    public override BackgroundAssets? CustomEncounterBackground(ActModel parentAct, Rng rng) => new("queen_boss", rng);
    public override float GetCameraScaling() => .80f;
    public override Vector2 GetCameraOffset() => Vector2.Down * 30;
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var left = (TwoTailedRat)ModelDb.Monster<TwoTailedRat>().ToMutable();
        var right = (TwoTailedRat)ModelDb.Monster<TwoTailedRat>().ToMutable();
        left.StarterMoveIndex = 0; right.StarterMoveIndex = 1;
        return [(ModelDb.Monster<RatKing>().ToMutable(), "king"), (left, "court_left"), (right, "court_right")];
    }
}
