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
    public override int MinInitialHp => 3000;
    public override int MaxInitialHp => 3000;
    public override string? CustomVisualPath => "res://LaRata/scenes/rat_king.tscn";
    public List<(string, string)> Localization =>
    [
        ("name", "La Rata"),
        ("moves.ROYAL_HAND.title", "The Royal Hand"),
        ("deck.description", "{Plan}"),
        ("edict.black_tithe", "Removes ALL of your Block before the attacks. Applies 2 Weak and 2 Frail to every player."),
        ("edict.plague_of_crowns", "Applies 8 Poison and 12 Doom to every player. Adds 2 Burns to each player's discard pile."),
        ("history.round", "Round {Round}"),
        ("history.title", "La Rata's played cards"),
        ("history.goblin", "Potion Goblin history"),
        ("history.description", "Royal discard - played cards from this combat. Hover a card to see its round."),
    ];

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var turn = new MoveState("ROYAL_HAND", PlayHand, new RoyalHandIntent());
        var draw = new RoyalDrawState(() => PrepareHand(turn));
        turn.FollowUpState = draw;
        // Include the complete intent asset set even during canonical preload.
        var assets = new MoveState("ROYAL_ASSETS", _ => Task.CompletedTask,
            new SingleAttackIntent(12), new MultiAttackIntent(7, 3), new DefendIntent(), new DebuffIntent(), new BuffIntent(), new HealIntent(), new StatusIntent(3));
        return new MonsterMoveStateMachine([turn, draw, assets], draw);
    }

    private string PrepareHand(MoveState turn)
    {
        if (deck == null)
        {
            var rng = EncounterDirector.Random(CombatState.RunState, CombatState.Encounter ?? throw new InvalidOperationException("Royal deck needs an encounter."), "royal-deck");
            deck = new RoyalDeck(n => rng.NextInt(n));
        }
        int turnNumber = ++preparedTurns;
        int energy = (Creature.CurrentHp * 2 <= Creature.MaxHp ? 8 : 6)
            + (turnNumber == 1 && (RegaliaMask & (1 << 8)) != 0 ? 1 : 0)
            + (turnNumber % 3 == 0 && (RegaliaMask & (1 << 10)) != 0 ? 1 : 0);
        deck.Prepare(energy, 5 + (turnNumber == 1 && (RegaliaMask & (1 << 9)) != 0 ? 2 : 0));
        PendingStrength = (deck.Planned.Contains(7) ? 2 : 0) + (deck.Planned.Contains(22) ? 3 : 0);
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
            CombatJournal.Record(CombatState, "card", cards[index]);
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
        if ((RegaliaMask & (1 << 11)) != 0)
            await PowerCmd.Apply<VulnerablePower>(context, CombatState.Players.OrderBy(p => p.NetId).Select(p => p.Creature).Where(c => c.IsAlive), 1, Creature, null);
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
        description.Add("Plan", "Draws 5 hidden cards from a 25-card deck. Plays spells and powers before attacking.\n" +
            (deck == null ? "6 Energy, increasing to 8 at half HP." : $"Energy: {deck.Energy}/{deck.Capacity}. Draw: {deck.DrawCount}. Discard: {deck.DiscardCount}."));
        return description;
    }
}

public sealed class RatKingEncounter : CustomEncounterModel, ILocalizationProvider
{
    public RatKingEncounter() : base(RoomType.Boss) { }
    public List<(string, string)> Localization => [("title", "La Rata")];
    public override bool IsValidForAct(ActModel act) => false; // Explicitly included only by RatDomain.
    internal static MonsterModel[] Guards => [ModelDb.Monster<CrownBulwark>(), ModelDb.Monster<CrownHeadsman>(), ModelDb.Monster<CrownArmorer>(), ModelDb.Monster<CrownHerald>()];
    public override IEnumerable<MonsterModel> AllPossibleMonsters => new[] { ModelDb.Monster<RatKing>() }.Cast<MonsterModel>().Concat(Guards);
    public override string? CustomScenePath => "res://LaRata/scenes/rat_king_encounter.tscn";
    public override string BossNodePath => "res://LaRata/images/map_rat_king";
    public override string? CustomRunHistoryIconPath => "res://LaRata/images/rat_king.png";
    public override string? CustomRunHistoryIconOutlinePath => CustomRunHistoryIconPath;
    public override BackgroundAssets? CustomEncounterBackground(ActModel parentAct, Rng rng) => new("queen_boss", rng);
    public override float GetCameraScaling() => .80f;
    public override Vector2 GetCameraOffset() => Vector2.Down * 30;
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        // Native encounter RNG is recreated from the saved run seed and floor.
        var indices = ChaosSelections.Distinct(2, Guards.Length, n => Rng.NextInt(n));
        return [(ModelDb.Monster<RatKing>().ToMutable(), "king"),
            (Guards[indices[0]].ToMutable(), "court_left"), (Guards[indices[1]].ToMutable(), "court_right")];
    }
}
