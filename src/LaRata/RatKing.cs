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
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

public sealed class RatKing : CustomMonsterModel, ILocalizationProvider
{
    // Shared with the native AnimationPlayer scene exported by the art owner.
    internal const float AttackImpactTime = .34f;
    internal const float CastImpactTime = .50f;
    internal const float AttackRecoveryTime = .51f;
    internal const float CastRecoveryTime = .65f;
    internal const float DeathAnimationTime = 1.40f;
    private RoyalDeck? deck;
    internal RoyalDeck? Deck => deck;
    internal int PendingStrength { get; set; }
    private int preparedTurns;
    private HashSet<int> resolvedCards = [];
    private Dictionary<int, ulong> targetIds = [];
    internal bool IsResolvingHand { get; private set; }
    internal string LastResolutionNote { get; private set; } = "";
    internal string TargetSummary
    {
        get
        {
            var players = CombatState.Players.OrderBy(p => p.NetId).ToList();
            return string.Join("; ", targetIds.OrderBy(p => p.Key).Select(pair =>
            {
                int seat = players.FindIndex(player => player.NetId == pair.Value);
                return seat < 0 ? null : $"{LaRataCardCatalog.GetBossModel(pair.Key).Title} → #{seat + 1} {players[seat].Creature.Name}";
            }).Where(text => text != null));
        }
    }
    internal IEnumerable<int> RemainingPlan => deck?.Planned.Where(i => !resolvedCards.Contains(i)) ?? [];
    private int? regaliaMask;
    internal int RegaliaMask => regaliaMask ??= RoyalRegaliaPower.Choose(EncounterDirector.Random(CombatState.RunState, CombatState.Encounter!, "royal-relics"));
    public override int MinInitialHp => 1500;
    public override int MaxInitialHp => 1500;
    public override float DeathAnimLengthOverride => DeathAnimationTime;
    public override string? CustomVisualPath => "res://LaRata/scenes/rat_king.tscn";
    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        resolvedCards = new(resolvedCards);
        targetIds = new(targetIds);
    }
    public List<(string, string)> Localization => [];

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal damage, ValueProp props,
        Creature? dealer, CardModel? cardSource) => RoyalDamageRamp.For(Creature, dealer, props);

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

    private RoyalDeck GetOrCreateDeck()
    {
        if (deck == null)
        {
            RoyalBossEffects.ValidateCatalog();
            var rng = EncounterDirector.Random(CombatState.RunState, CombatState.Encounter ?? throw new InvalidOperationException("Royal deck needs an encounter."), "royal-deck");
            deck = new RoyalDeck(n => rng.NextInt(n));
        }
        return deck;
    }

    private string PrepareHand(MoveState turn)
    {
        var deck = GetOrCreateDeck();
        int turnNumber = ++preparedTurns;
        int energy = (Creature.CurrentHp * 2 <= Creature.MaxHp ? 8 : 6)
            + (turnNumber == 1 && (RegaliaMask & (1 << 8)) != 0 ? 1 : 0)
            + (turnNumber % 3 == 0 && (RegaliaMask & (1 << 10)) != 0 ? 1 : 0);
        resolvedCards.Clear(); targetIds.Clear();
        LastResolutionNote = "";
        deck.Prepare(energy, 5 + (turnNumber == 1 && (RegaliaMask & (1 << 9)) != 0 ? 2 : 0), i => RoyalBossEffects.Unavailable(this, i));
        var targeting = EncounterDirector.Random(CombatState.RunState, CombatState.Encounter!, $"royal-targets/{turnNumber}");
        var players = CombatState.Players.Where(p => p.Creature.IsAlive).OrderBy(p => p.NetId).ToArray();
        foreach (int index in deck.Planned.Where(i => i is 28 or 41 or 44))
        {
            // Graveyard Shift consumes an existing opening. Select and announce
            // once; losing that status or dying later never triggers retargeting.
            var eligible = index == 41 ? players.Where(p => p.Creature.GetPowerAmount<VulnerablePower>() > 0).ToArray() : players;
            if (eligible.Length > 0) targetIds[index] = eligible[targeting.NextInt(eligible.Length)].NetId;
        }
        PendingStrength = (deck.Planned.Contains(7) ? 2 : 0) + (deck.Planned.Contains(22) ? 3 : 0);
        var intents = deck.Planned.SelectMany(i => RoyalBossEffects.Intents(this, i)).ToList();
        if (!deck.Planned.Any(RoyalDeck.IsAttack)) intents.Add(new RoyalForecastAttackIntent(this, -1, 12, 1));
        intents.Add(new RoyalHandIntent());
        AccessTools.PropertySetter(typeof(MoveState), nameof(MoveState.Intents)).Invoke(turn, [intents]);
        foreach (var skipped in deck.Skipped) Note(skipped.Key, skipped.Value == "no_material" ? RoyalBossText.Text("no_material", "No spare hand card available.") : skipped.Value);
        ModEntry.Log.Info($"ROYAL PLAN {Snapshot}");
        return turn.Id;
    }

    private async Task PlayHand(IReadOnlyList<Creature> targets)
    {
        var activeDeck = deck ?? throw new InvalidOperationException("Royal hand was not prepared.");
        int[] plan = activeDeck.Planned.ToArray();
        var cards = LaRataCardCatalog.BossCards;
        bool execution = false;
        IsResolvingHand = true;
        Creature.GetPower<RoyalRegaliaPower>()?.BeginRoyalHand();
        foreach (int index in plan)
        {
            if (!Creature.IsAlive || CombatManager.Instance.IsEnding) break;
            activeDeck.Spend(index);
            LastResolutionNote = "";
            ModEntry.Log.Info($"ROYAL PLAY card={cards[index].Id.Entry} {activeDeck.Snapshot}");
            var shadow = Creature.GetPower<RoyalSecondShadowPower>();
            var preview = await RoyalDeckView.Show(this, cards[index], RoyalBossEffects.Description(index));
            CombatJournal.Record(CombatState, "card", cards[index]);
            try
            {
                await RoyalBossEffects.Resolve(this, index, targets);
                activeDeck.Complete(index);
                resolvedCards.Add(index);
                if (Creature.IsAlive && !CombatManager.Instance.IsEnding)
                {
                    if (RoyalDeck.IsExhaust(index)) await AfterPileAction(true);
                    if (shadow != null) await shadow.AfterRoyalCard();
                    if (Creature.GetPower<RoyalRegaliaPower>() is { } regalia) await regalia.AfterRoyalCard(index);
                }
                await FinishActionAnimation(RoyalDeck.IsAttack(index));
                await RoyalDeckView.ShowResolution(preview, LastResolutionNote);
            }
            finally { RoyalDeckView.Dismiss(preview); }
            execution |= index == 2;
        }
        if (!plan.Any(RoyalDeck.IsAttack) && Creature.IsAlive && !CombatManager.Instance.IsEnding)
        {
            await DamageCmd.Attack(12).FromMonster(this).WithAttackerAnim("Attack", AttackImpactTime).Execute(new ThrowingPlayerChoiceContext());
            await FinishActionAnimation(true);
        }
        if (execution && Creature.IsAlive && !CombatManager.Instance.IsEnding)
            await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets.Where(c => c.IsAlive), 2, Creature, null);
        if (Creature.IsAlive && Creature.GetPower<RoyalRegaliaPower>() is { } courtRelics)
            await courtRelics.EndRoyalHand(activeDeck.LastHandHadAttack);
        activeDeck.Finish();
        IsResolvingHand = false;
        if (Creature.IsAlive) await CreatureCmd.TriggerAnim(Creature, "Idle", 0);
    }

    private Task FinishActionAnimation(bool attack)
    {
        if (!Creature.IsAlive || CombatManager.Instance.IsEnding) return Task.CompletedTask;
        float seconds = attack ? AttackRecoveryTime : CastRecoveryTime;
        return Cmd.CustomScaledWait(seconds / 2, seconds);
    }

    internal string Snapshot => $"turn={preparedTurns};mask={RegaliaMask};{deck?.Snapshot};targets={string.Join(',', targetIds.OrderBy(p => p.Key).Select(p => $"{p.Key}:{p.Value}"))};resolved={string.Join(',', resolvedCards.Order())};relics={Creature.GetPower<RoyalRegaliaPower>()?.Progress}";
    internal Creature? TargetFor(int index) => targetIds.TryGetValue(index, out ulong id) ? CombatState.Players.FirstOrDefault(p => p.NetId == id)?.Creature : null;
    internal void Note(int index, string reason)
    {
        LastResolutionNote = string.IsNullOrEmpty(LastResolutionNote) ? $"{LaRataCardCatalog.GetBossModel(index).Title}: {reason}" : LastResolutionNote + "\n" + reason;
        ModEntry.Log.Info($"ROYAL RESULT card={index} {reason}");
    }
    internal Task ReserveNext(int source, int cards, int energy, bool relic = false)
    {
        var granted = GetOrCreateDeck().ReserveNext(cards, energy, relic);
        Note(source, RoyalBossText.Text("reserve", "Next royal hand: +{Draw} cards, +{Energy} Energy. Card caps: +3 draw / +2 Energy; hand limit 10.", ("Draw", granted.Draw), ("Energy", granted.Energy)));
        return Task.CompletedTask;
    }
    internal Task ReserveRelic(int relic, int cards, int energy)
    {
        GetOrCreateDeck().ReserveNext(cards, energy, relic: true);
        string note = RoyalRegaliaPower.Relics[relic].Title.GetFormattedText() + ": " +
            RoyalBossText.Text("relic_reserve", "Next royal hand: +{Draw} cards, +{Energy} Energy. Hand limit 10.", ("Draw", cards), ("Energy", energy));
        LastResolutionNote = string.IsNullOrEmpty(LastResolutionNote) ? note : LastResolutionNote + "\n" + note;
        ModEntry.Log.Info($"ROYAL RELIC index={relic} {LastResolutionNote}");
        return Task.CompletedTask;
    }
    internal async Task AfterPileAction(bool isExhaust)
    {
        if (isExhaust)
        {
            if (Creature.GetPower<RoyalCourtOfCindersPower>() is { } cinders) await cinders.AfterRoyalExhaust(this);
        }
        else if (Creature.GetPower<RoyalSmugglersDividendPower>() is { } dividend) await dividend.AfterRoyalDiscard();
    }
    internal decimal PreviewAttackDamage(int index, int basis)
    {
        bool first = RemainingPlan.FirstOrDefault(RoyalDeck.IsAttack, -1) == index;
        return basis + PendingStrength + (first ? PendingVigor() : -Creature.GetPowerAmount<VigorPower>());
    }
    internal int PendingVigor()
    {
        int vigor = 0;
        var dividendPower = Creature.GetPower<RoyalSmugglersDividendPower>();
        bool dividend = dividendPower != null;
        bool dividendUsed = IsResolvingHand && dividendPower?.Amount == 2;
        foreach (int index in RemainingPlan)
        {
            if (RoyalDeck.IsAttack(index)) break;
            if (index == 45) dividend = true;
            if (index == 27 || index is 39 or 42 && deck!.HasMaterial(index)) vigor += 4;
            if (dividend && !dividendUsed && index is 32 or 38 or 43 && deck!.HasMaterial(index))
            { vigor += 3; dividendUsed = true; }
        }
        return vigor;
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
        var king = owner.Monster as RatKing;
        var deck = king?.Deck;
        string plan = deck == null ? RoyalBossText.Text("hand_unprepared", "Draws 5 hidden cards. 6 Energy, increasing to 8 at half HP. Skills and Powers precede attacks.") :
            RoyalBossText.Text("hand_summary", "Drew {Drawn} hidden cards from {Total} families. Skills and Powers precede attacks. Energy {Energy}/{Capacity}. Draw {Draw}; discard {Discard}; Exhaust {Exhaust}; active Powers {Powers}. NEXT hand bonuses: {NextDraw} cards, {NextEnergy} Energy. Hand limit 10; play limit 8.",
                ("Drawn", deck.DrawnThisTurn), ("Total", RoyalDeck.ActiveCards.Length), ("Energy", deck.Energy), ("Capacity", deck.Capacity), ("Draw", deck.DrawCount), ("Discard", deck.DiscardCount), ("Exhaust", deck.ExhaustCount), ("Powers", deck.PersistentCount), ("NextDraw", deck.NextDraw), ("NextEnergy", deck.NextEnergy));
        plan += "\n" + RoyalBossText.Text("damage_ramp", "Royal attacks deal {Percent}% damage. Increases by 10 percentage points each round, up to 100%.",
            ("Percent", RoyalDamageRamp.Factor(owner.CombatState?.RoundNumber ?? 1) * 100));
        if (!string.IsNullOrEmpty(king?.TargetSummary)) plan += "\n" + RoyalBossText.Text("targets", "Announced single-target effects: {Targets}.", ("Targets", king.TargetSummary));
        if (!string.IsNullOrEmpty(king?.LastResolutionNote)) plan += "\n" + RoyalBossText.Text("last_result", "Last resolution: {Result}", ("Result", king.LastResolutionNote));
        description.Add("Plan", plan);
        return description;
    }
}

public sealed class RatKingEncounter : CustomEncounterModel, ILocalizationProvider
{
    public RatKingEncounter() : base(RoomType.Boss) { }
    public List<(string, string)> Localization => [];
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
