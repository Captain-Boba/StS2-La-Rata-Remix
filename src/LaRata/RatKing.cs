using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
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
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Combat;

namespace LaRata;

public sealed class RatKing : CustomMonsterModel, ILocalizationProvider
{
    private RoyalDeck? deck;
    internal RoyalDeck? Deck => deck;
    internal int PendingStrength { get; set; }
    private int preparedTurns;
    private int? regaliaMask;
    internal int RegaliaMask => regaliaMask ??= RoyalRegaliaPower.Choose(EncounterDirector.Random(CombatState.RunState, CombatState.Encounter!, "royal-relics"));
    public override int MinInitialHp => 1080;
    public override int MaxInitialHp => 1080;
    public override string? CustomVisualPath => "res://LaRata/scenes/rat_king.tscn";
    public List<(string, string)> Localization =>
    [
        ("name", "La Rata"),
        ("moves.ROYAL_HAND.title", "The Royal Hand"),
        ("deck.description", "{Plan}"),
    ];

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var turn = new MoveState("ROYAL_HAND", PlayHand, new RoyalHandIntent());
        var draw = new RoyalDrawState(() => PrepareHand(turn));
        turn.FollowUpState = draw;
        // Include the complete intent asset set even during canonical preload.
        var assets = new MoveState("ROYAL_ASSETS", _ => Task.CompletedTask,
            new SingleAttackIntent(12), new MultiAttackIntent(7, 3), new DefendIntent(), new DebuffIntent(), new BuffIntent(), new StatusIntent(3));
        return new MonsterMoveStateMachine([turn, draw, assets], draw);
    }

    private string PrepareHand(MoveState turn)
    {
        if (deck == null)
        {
            var rng = EncounterDirector.Random(CombatState.RunState, CombatState.Encounter ?? throw new InvalidOperationException("Royal deck needs an encounter."), "royal-deck");
            deck = new RoyalDeck(n => rng.NextInt(n));
        }
        deck.Prepare((Creature.CurrentHp * 2 <= Creature.MaxHp ? 4 : 3) + (preparedTurns++ == 0 && (RegaliaMask & (1 << 8)) != 0 ? 1 : 0));
        PendingStrength = deck.Planned.Contains(7) ? 2 : 0;
        var cards = RoyalCard.All;
        var intents = deck.Planned.SelectMany(i => cards[i].GetIntents(this)).ToList();
        if (!deck.Planned.Any(RoyalDeck.IsAttack)) intents.Add(new SingleAttackIntent(() => 12 + PendingStrength));
        intents.Add(new RoyalHandIntent());
        AccessTools.PropertySetter(typeof(MoveState), nameof(MoveState.Intents)).Invoke(turn, [intents]);
        ModEntry.Log.Info($"ROYAL PLAN {deck.Snapshot}");
        return turn.Id;
    }

    private async Task PlayHand(IReadOnlyList<Creature> targets)
    {
        var activeDeck = deck ?? throw new InvalidOperationException("Royal hand was not prepared.");
        int[] plan = activeDeck.Planned.ToArray();
        var cards = RoyalCard.All;
        bool execution = false;
        foreach (int index in plan)
        {
            if (!Creature.IsAlive || CombatManager.Instance.IsEnding) break;
            activeDeck.Spend(index);
            ModEntry.Log.Info($"ROYAL PLAY card={cards[index].Id.Entry} {activeDeck.Snapshot}");
            var preview = await RoyalDeckView.Show(this, cards[index]);
            await cards[index].Resolve(this, targets);
            RoyalDeckView.Dismiss(preview);
            execution |= index == 2;
        }
        if (!plan.Any(RoyalDeck.IsAttack) && Creature.IsAlive && !CombatManager.Instance.IsEnding)
            await DamageCmd.Attack(12).FromMonster(this).WithAttackerAnim("Attack", .3f).Execute(new ThrowingPlayerChoiceContext());
        if (execution && Creature.IsAlive && !CombatManager.Instance.IsEnding)
            await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets.Where(c => c.IsAlive), 2, Creature, null);
        activeDeck.Finish();
        if (Creature.IsAlive) await CreatureCmd.TriggerAnim(Creature, "Idle", 0);
    }

    public override async Task BeforeCombatStart()
    {
        var context = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<RoyalRegaliaPower>(context, Creature, RegaliaMask, Creature, null);
        if ((RegaliaMask & 1) != 0) await PowerCmd.Apply<StrengthPower>(context, Creature, 1, Creature, null);
        if ((RegaliaMask & 2) != 0) await PowerCmd.Apply<DexterityPower>(context, Creature, 1, Creature, null);
        if ((RegaliaMask & 4) != 0) await CreatureCmd.GainBlock(Creature, 10, MegaCrit.Sts2.Core.ValueProps.ValueProp.Move, null);
        if ((RegaliaMask & 8) != 0) await PowerCmd.Apply<ThornsPower>(context, Creature, 3, Creature, null);
        ModEntry.Log.Info($"ROYAL RELICS mask={RegaliaMask} {RoyalRegaliaPower.Describe(RegaliaMask)}");
    }
}

internal sealed class RoyalDrawState(Func<string> prepare) : MonsterState
{
    public override string Id => "DRAW_ROYAL_HAND";
    public override bool ShouldAppearInLogs => false;
    public override string GetNextState(Creature owner, Rng rng) => prepare();
    public override void RegisterStates(Dictionary<string, MonsterState> states) => states.Add(Id, this);
}

internal sealed class RoyalHandIntent : BuffIntent
{
    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
    {
        var description = new LocString("monsters", ModelDb.Monster<RatKing>().Id.Entry + ".deck.description");
        var deck = (owner.Monster as RatKing)?.Deck;
        description.Add("Plan", "Draws a hidden hand from a 10-card deck. Plays spells and powers before attacking.\n" +
            (deck == null ? "3 Energy, increasing to 4 below half HP." : $"Energy: {deck.Energy}/{deck.Capacity}. Draw: {deck.DrawCount}. Discard: {deck.DiscardCount}."));
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
