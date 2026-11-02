using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

internal static class EncounterVarietyChecks
{
    private static readonly PlayerChoiceContext Context = new ThrowingPlayerChoiceContext();
    private static readonly TwistKind[] NewKinds = Enum.GetValues<TwistKind>().Where(k => (int)k >= 48).ToArray();
    private static void Check(bool ok, string why) { if (!ok) throw new Exception(why); }
    private static bool Active(ref bool __result) { __result = false; return false; }
    private static bool NoTalk() => false; // UI speech bubble needs the desktop preference singleton.
    private static void Start(CombatState combat) => ((Task)AccessTools.Method(typeof(StartingBuffsPatch), "Apply")
        .Invoke(null, [Task.CompletedTask, combat])!).GetAwaiter().GetResult();

    internal static (CombatState Combat, Creature Enemy, Creature[] Targets) Setup(MonsterModel canonical, int count,
        int act = 0, EnemyTwist? twist = null, bool helper = false, bool scout = false)
    {
        var players = Enumerable.Range(0, count).Select(i => NativeConsoleFixture.Player((ulong)(1201 + i))).ToArray();
        var run = RunState.CreateForTest(players: players, seed: "VARIETY");
        if (act == 3) NativeConsoleFixture.Set(run, nameof(run.Acts), new ActModel[] { run.Acts[0], run.Acts[1], run.Acts[2], ModelDb.Act<RatDomain>().ToMutable() });
        run.CurrentActIndex = act;
        NativeConsoleFixture.Set(RunManager.Instance, "State", run);
        NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.AscensionManager), new MegaCrit.Sts2.Core.Entities.Ascension.AscensionManager(0));
        var encounter = ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable();
        var combat = new CombatState(encounter, run);
        AccessTools.Field(typeof(CombatManager), "_state").SetValue(CombatManager.Instance, combat);
        NativeConsoleFixture.Set(CombatManager.Instance, nameof(CombatManager.IsInProgress), true);
        foreach (var player in players)
        {
            NativeConsoleFixture.Set(player, nameof(player.Creature), new Creature(player, 50000, 50000) { CombatState = combat });
            player.ResetCombatState(); combat.AddCreature(player.Creature);
        }
        var monster = canonical.ToMutable();
        var rules = new EncounterRules(new(0, 0, helper ? 1 : 0, twist == null ? [] : [twist]) { ScoutReinforcement = scout }) { Roster = [monster] };
        if (helper) rules.Reinforcements.Add(monster);
        EncounterDirector.Rules.Add(encounter, rules);
        var body = combat.CreateCreature(monster, CombatSide.Enemy, null);
        combat.AddCreature(body);
        monster.SetUpForCombat();
        return (combat, body, players.Select(p => p.Creature).ToArray());
    }

    internal static void Run(bool witheringOnly = false)
    {
        var harmony = new Harmony("LaRata.EncounterVarietyChecks");
        NativeConsoleFixture.PatchDesktop(harmony);
        harmony.Patch(AccessTools.Method(typeof(TalkCmd), nameof(TalkCmd.Play)), prefix: new(typeof(EncounterVarietyChecks), nameof(NoTalk)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsOverOrEnding)), prefix: new(typeof(EncounterVarietyChecks), nameof(Active)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsEnding)), prefix: new(typeof(EncounterVarietyChecks), nameof(Active)));
        foreach (var patch in new[] { typeof(BonusMultiplayerScalingPatch), typeof(CreatureHpPatch), typeof(DamagePatch), typeof(PowerPotencyPatch), typeof(HexTelegraphPatch), typeof(HexExecutionPatch), typeof(VarietyPowerDescriptionPatch) })
            harmony.CreateClassProcessor(patch).Patch();
        try
        {
            using var localization = new NativeFixtureLocalization(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.research/native-reference-0.107.1-20261025/localization/eng")));
            if (witheringOnly) WitheringChecks.Run();
            else
            {
                Policy(); NativeTwists(); Boundaries(); Helpers();
                Console.WriteLine("PASS encounter variety: focused policy/native-model fixtures only; no rendered scenes, disk reload, live clients or game start.");
            }
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    private static void Policy()
    {
        Check(Enum.GetValues<TwistKind>().Length == 58 && EncounterPlan.ActiveKinds.Length == 53 && NewKinds.Length == 10, "Append-only twist catalog count");
        Check((int)TwistKind.Crossfire == 47 && (int)TwistKind.Sheltered == 48, "Legacy enum IDs moved");
        Check(new[] { TwistKind.Armored, TwistKind.Slippery, TwistKind.Spiteful, TwistKind.SkillHunter, TwistKind.Doomweaver }.All(k => !EncounterPlan.ActiveKinds.Contains(k)), "Retired twist selected");
        var seen = new HashSet<TwistKind>(); var scoutSeen = new HashSet<TwistKind>(); int plans = 0;
        for (uint seed = 0; seed < 192; seed++)
        for (int count = 1; count <= 4; count++)
        for (int act = 0; act <= 3; act++)
        for (int tier = 0; tier <= 2; tier++)
        {
            var ar = new Rng(seed); var br = new Rng(seed);
            var a = EncounterPlan.Roll(n => ar.NextInt(n), count, tier, 3, 1, actIndex: act);
            var b = EncounterPlan.Roll(n => br.NextInt(n), count, tier, 3, 1, actIndex: act);
            int budget = 3 * (tier == 2 ? 2 : tier == 1 ? 3 : 4) * count * (5 + 2 * act);
            Check(a.Budget == budget && a.Spent <= budget && a.Spent == a.Twists.Sum(EncounterPlan.Cost) + a.ReinforcementCount * a.HelperCost, "Budget/cost changed");
            Check(a.SelectionTrace.SequenceEqual(b.SelectionTrace) && a.Twists.SequenceEqual(b.Twists) && ar.Counter == br.Counter, "Replay/RNG differs");
            Check(a.ReinforcementCount <= EncounterPlan.ReinforcementLimit(count, act) && (tier == 0 || a.ReinforcementCount == 0), "Helper limits changed");
            Check(a.Twists.Count(t => EncounterPlan.IsOffense(t.Kind)) <= 1 && a.Twists.Count(t => t.IsHex) <= (count >= 3 ? 2 : 1), "Offense/hex cap broken");
            Check(act > 0 || a.Twists.All(t => t.Kind != TwistKind.Withering), "Withering must not enter act one, including co-op");
            foreach (var group in a.Twists.GroupBy(t => t.Target))
            {
                Check(group.Count(t => EncounterPlan.IsShield(t.Kind)) <= 1 && group.Count(t => EncounterPlan.IsBlock(t.Kind)) <= 1, "Defense conflicts");
                foreach (var x in group) foreach (var y in group.Where(t => t != x)) Check(!EncounterPlan.ExtraConflict(x.Kind, y.Kind), "Tradeoff/sustain conflict");
            }
            seen.UnionWith(a.Twists.Select(t => t.Kind));
            if (count == 1 && act == 0 && tier == 0 && a.ScoutReinforcement) scoutSeen.UnionWith(a.Twists.Select(t => t.Kind));
            plans++;
        }
        Check(NewKinds.All(seen.Contains), "New twist unreachable");
        Check(new[] { TwistKind.Reactive, TwistKind.Overcommitted, TwistKind.Sundering, TwistKind.Purifying, TwistKind.Mending }.All(scoutSeen.Contains), "New 32-unit effects unreachable after act-one scout");
        Check(EncounterPlan.ScoutCost == 28 && EncounterPlan.ReinforcementCost == 64 && EncounterPlan.ReinforcementChance(1, 0) == 70, "Reinforcement rates changed");
        Console.WriteLine($"PASS policy: {plans} directed act/tier/party plans and exact replay; all ten additions reachable, five in solo act-one scout remainder; original budgets, limits and retired IDs.");
    }

    private static CardPlay Play(CardModel card) => new() { Card = card, Target = null, ResultPile = PileType.Discard, Resources = new() { EnergySpent = 1, EnergyValue = 1, StarsSpent = 0, StarValue = 0 }, IsAutoPlay = false, PlayIndex = 0, PlayCount = 1 };
    private static void NativeTwists()
    {
        int cases = 0;
        foreach (int count in new[] { 1, 2, 3, 4 })
        foreach (int act in new[] { 0, 1, 2, 3 })
        foreach (var kind in NewKinds)
        {
            var twist = new EnemyTwist(kind, 0, 1, act);
            var (combat, enemy, targets) = Setup(ModelDb.Monster<TwoTailedRat>(), count, act, twist);
            Start(combat); int powers = enemy.Powers.Count(); Start(combat);
            Check(enemy.Powers.Count() == powers, "Startup duplicated powers");
            var card = combat.CreateCard<StrikeIronclad>(targets[0].Player!);
            PileType.Hand.GetPile(targets[0].Player!).AddInternal(card);
            if (twist.IsHex)
            {
                var marked = enemy.Monster!.MoveStateMachine!.States.Values.OfType<MoveState>().Single(m => HexTelegraphPatch.Hexes.TryGetValue(m, out _));
                Check(marked.Intents.Any(i => EncounterPlan.IsSelfHex(kind) ? i is HexBuffIntent : i is HexDebuffIntent), "Missing matching native telegraph");
                Check(!HexTwistPower.Describe(twist).GetFormattedText().Contains("LARATA-"), "Missing localized concrete tooltip");
                // Controlled base move isolates the real post-move command path.
                var move = new MoveState("VARIETY_HEX", _ => Task.CompletedTask, new BuffIntent());
                HexTelegraphPatch.Hexes.Add(move, new(enemy.Monster!, twist));
                foreach (var target in targets) target.GainBlockInternal(10);
                enemy.SetCurrentHpInternal(enemy.MaxHp - 10);
                ModelDb.Power<WeakPower>().ToMutable().ApplyInternal(enemy, 2, true);
                ModelDb.Power<VulnerablePower>().ToMutable().ApplyInternal(enemy, 2, true);
                int before = enemy.CurrentHp;
                enemy.GetPower<HexTwistPower>()!.RemoveInternal(); // presentation never gates mechanics
                move.PerformMove(targets).GetAwaiter().GetResult();
                switch (kind)
                {
                    case TwistKind.Sundering: Check(targets.All(t => t.Block == 4), "Block stripping amount/party scaling"); break;
                    case TwistKind.Purifying: Check(enemy.GetPower<VulnerablePower>() == null && enemy.GetPowerAmount<WeakPower>() == 2, "Deterministic one-debuff cleanse"); break;
                    case TwistKind.Mending: Check(enemy.CurrentHp == before + Math.Min(3, Math.Max(1, enemy.MaxHp / 20)), "Healing cap/party amplification"); break;
                    case TwistKind.Entangling:
                        Check(targets.All(t => t.GetPowerAmount<TangledPower>() == 1), "Tangle amount");
                        Check(card.EnergyCost.GetWithModifiers(CostModifiers.All) == 2, "Native Attack cost tax missing"); break;
                    case TwistKind.Withering:
                        foreach (var target in targets) target.BeforeTurnStart(CombatSide.Player);
                        Check(targets.All(t => t.GetPower<WitheringPower>()!.ModifyHandDraw(t.Player!, 5) == 4), "Next-turn draw reduction"); break;
                    case TwistKind.Shriveling: Check(targets.All(t => t.GetPower<ShrinkPower>()!.ModifyDamageMultiplicative(enemy, 10, ValueProp.Move, t, null) == .7m), "Native shrink damage"); break;
                }
                move.PerformMove(targets).GetAwaiter().GetResult();
                if (kind == TwistKind.Entangling) Check(targets.All(t => t.GetPowerAmount<TangledPower>() == 1), "Tangle stacked");
                if (kind == TwistKind.Withering) Check(targets.All(t => t.GetPowerAmount<WitheringPower>() == 1), "Withering stacked");
                if (kind == TwistKind.Shriveling) Check(targets.All(t => t.GetPowerAmount<ShrinkPower>() == 1), "Shrink stacked");
                foreach (var target in targets)
                    foreach (var power in target.Powers.ToArray()) power.AfterSideTurnEnd(Context, CombatSide.Player, targets).GetAwaiter().GetResult();
                if (kind == TwistKind.Entangling) Check(targets.All(t => !t.HasPower<TangledPower>()) && card.EnergyCost.GetWithModifiers(CostModifiers.All) == 1, "Tangle expiry/affliction cleanup");
                if (kind == TwistKind.Shriveling) Check(targets.All(t => !t.HasPower<ShrinkPower>()), "Shrink duration");
            }
            else switch (kind)
            {
                case TwistKind.Sheltered:
                    Check(enemy.GetPower<HardToKillPower>()!.SmartDescription.GetFormattedText() == "Takes at most 18 damage per hit.", "Sheltered native text hardcodes another monster/amount");
                    int hp = enemy.CurrentHp;
                    DamageCmd.Attack(24).FromCard(card).Targeting(enemy).Execute(Context).GetAwaiter().GetResult();
                    Check(enemy.CurrentHp == hp - 18 && enemy.GetPowerAmount<HardToKillPower>() == 18, "Native per-hit damage cap"); break;
                case TwistKind.Reactive:
                    Check(enemy.GetPower<SneakyPower>()!.Description.GetFormattedText().Contains("2 Block after each Attack card"), "Reactive enemy tooltip");
                    enemy.GetPower<SneakyPower>()!.AfterCardPlayed(Context, Play(card)).GetAwaiter().GetResult();
                    Check(enemy.Block == 2, "Reactive block amplified"); break;
                case TwistKind.Drifting:
                    Check(enemy.GetPower<SoarPower>()!.RemoteDescription.GetFormattedText() == "Receives 50% less incoming attack damage.", "Drifting misleading landing/outgoing tooltip");
                    var slow = enemy.GetPower<SlowPower>()!;
                    Check(enemy.HasPower<SoarPower>() && slow != null, "Drifting native statuses");
                    slow!.AfterCardPlayed(Context, Play(card)).GetAwaiter().GetResult();
                    decimal damage = Hook.ModifyDamage(combat.RunState, combat, enemy, targets[0], 20, ValueProp.Move, card, ModifyDamageHookType.All, CardPreviewMode.None, out _);
                    Check(damage == 11, "Soar/Slow native multiplier");
                    slow.AfterSideTurnStart(CombatSide.Enemy, [enemy], combat).GetAwaiter().GetResult();
                    Check(slow.DisplayAmount == 0, "Slow does not reset"); break;
                case TwistKind.Overcommitted:
                    Check(enemy.GetPowerAmount<VigorPower>() == 4 && enemy.HasPower<ImbalancedPower>(), "Overcommitted native statuses");
                    enemy.Monster!.RollMove(targets);
                    foreach (var target in targets) target.GainBlockInternal(100);
                    DamageCmd.Attack(2).FromMonster(enemy.Monster).Execute(Context).GetAwaiter().GetResult();
                    Check(enemy.Monster.NextMove.Intents.Any(i => i is StunIntent), "Native fully-blocked stun missing"); break;
            }
            cases++;
        }
        Console.WriteLine($"PASS native twists: {cases} act/party/effect fixtures; real starting grants and commands, native caps/costs/draw/damage/expiry, concrete marked intents and execution with marker removed.");
    }

    private static void Helpers()
    {
        var seen = new HashSet<ModelId>(); int moves = 0;
        for (int act = 0; act < 4; act++)
        {
            var pool = EncounterDirector.ReinforcementCandidates(act, [], []);
            Check(pool.Length == (act == 0 ? 6 : act == 1 ? 13 : 14), "Act pool count");
            Check(pool.All(m => m is not Inklet and not RatKing and not KingsguardKnight), "Forbidden random helper");
            Check(EncounterDirector.ReinforcementCandidates(act, [pool[0].Id], [pool[1].Id]).All(m => m.Id != pool[0].Id && m.Id != pool[1].Id), "Present/recent exclusion");
            var fullHistory = pool.Select(m => m.Id).ToArray();
            Check(EncounterDirector.ReinforcementCandidates(act, [], fullHistory).Single().Id == pool[0].Id, "Least-recent fallback");
            foreach (int count in new[] { 1, 2, 3, 4 })
            foreach (var canonical in pool)
            {
                var a = Simulate(canonical, count, act); var b = Simulate(canonical, count, act);
                Check(a == b, "Helper move/state replay differs: " + canonical.Id);
                seen.Add(canonical.Id); moves += 16;
            }
        }
        Check(seen.Count == 14, "Helper coverage missing");
        Console.WriteLine($"PASS helpers: 14 native types, act pools 6/13/14/14, 1-4 modeled players, {moves} performed native moves including seeded replay, startup statuses, null slots, HP/damage factors, recent/present exclusions and least-recent fallback.");

        string Simulate(MonsterModel canonical, int count, int act)
        {
            int baselineHp = Setup(canonical, count, act).Enemy.MaxHp;
            var (combat, enemy, targets) = Setup(canonical, count, act, helper: true, scout: act == 0);
            Check(enemy.MaxHp == Math.Max(1, Math.Ceiling(baselineHp * (act == 0 ? .35m : .60m))), "Helper actual HP factor changed");
            var monster = enemy.Monster!;
            monster.AfterAddedToRoom().GetAwaiter().GetResult(); Start(combat);
            Check(enemy.SlotName == null && !enemy.IsSecondaryEnemy, "Helper requires a special slot or fatal behavior");
            Check(enemy.GetPowerAmount<ReinforcementTwistPower>() == (act == 0 ? 35 : 60), "Helper marker/factor mismatch");
            decimal damage = Hook.ModifyDamage(combat.RunState, combat, targets[0], enemy, 10, ValueProp.Move, null, ModifyDamageHookType.Multiplicative, CardPreviewMode.None, out _);
            Check(damage == (act == 0 ? 5 : 6.5m), "Helper attack factor changed");
            var trace = new List<string>();
            for (int turn = 1; turn <= 8; turn++)
            {
                combat.RoundNumber = turn;
                monster.RollMove(targets);
                trace.Add(monster.NextMove.Id);
                monster.PerformMove().GetAwaiter().GetResult();
            }
            Check(targets.Any(t => t.CurrentHp < t.MaxHp), "No native attack executed: " + canonical.Id);
            Check(combat.Enemies.Count() == 1, "Helper summoned an unbudgeted creature");
            if (canonical is SnappingJaxfruit) Check(enemy.GetPowerAmount<StrengthPower>() == 16, "Jaxfruit native growth");
            if (canonical is Chomper) Check(enemy.HasPower<ArtifactPower>() && targets.All(t => t.Player!.PlayerCombatState!.DiscardPile.Cards.OfType<Dazed>().Any()), "Chomper startup/status path");
            if (canonical is TwigSlimeM) Check(targets.All(t => t.Player!.PlayerCombatState!.DiscardPile.Cards.OfType<Slimed>().Any()), "Twig slime native status path");
            if (canonical is SewerClam) Check(enemy.HasPower<PlatingPower>() && enemy.HasPower<StrengthPower>(), "Clam startup/growth path");
            if (canonical is SlitheringStrangler) Check(targets.All(t => t.HasPower<ConstrictPower>()), "Strangler native Constrict path");
            trace.Add(string.Join(',', enemy.Powers.Select(p => p.Id.Entry + ":" + p.Amount).Order()));
            trace.Add(string.Join(',', targets.Select(t => t.CurrentHp + ":" + string.Join('/', t.Powers.Select(p => p.Id.Entry + p.Amount).Order()))));
            monster.BeforeRemovedFromRoom();
            return string.Join(';', trace);
        }
    }

    private static void Boundaries()
    {
        foreach (var kind in new[] { TwistKind.Entangling, TwistKind.Withering, TwistKind.Shriveling })
        {
            var twist = new EnemyTwist(kind, 0, 1, 1);
            var (combat, enemy, targets) = Setup(ModelDb.Monster<TwoTailedRat>(), 2, 1, twist);
            Start(combat);
            foreach (var target in targets) ModelDb.Power<ArtifactPower>().ToMutable().ApplyInternal(target, 1, true);
            var move = new MoveState("ARTIFACT_BOUNDARY", _ => Task.CompletedTask);
            HexTelegraphPatch.Hexes.Add(move, new(enemy.Monster!, twist));
            move.PerformMove(targets).GetAwaiter().GetResult();
            Check(targets.All(t => !t.HasPower<ArtifactPower>() && !t.Powers.Any(p => p.TypeForCurrentAmount == PowerType.Debuff)), "New hex bypasses Artifact");
        }
        {
            var (combat, enemy, targets) = Setup(ModelDb.Monster<TwoTailedRat>(), 1, twist: new(TwistKind.Purifying, 0, 1));
            PowerCmd.Apply<ShacklingPotionPower>(Context, enemy, 1, targets[0], null).GetAwaiter().GetResult();
            Check(enemy.GetPowerAmount<StrengthPower>() == -1, "Temporary Strength setup");
            var move = new MoveState("CLEANSE_BOUNDARY", _ => Task.CompletedTask);
            HexTelegraphPatch.Hexes.Add(move, new(enemy.Monster!, new(TwistKind.Purifying, 0, 1)));
            move.PerformMove(targets).GetAwaiter().GetResult();
            Check(enemy.GetPowerAmount<StrengthPower>() == 0 && !enemy.HasPower<ShacklingPotionPower>(), "Cleanse stranded temporary Strength");
        }
        {
            var (combat, enemy, _) = Setup(ModelDb.Monster<TwoTailedRat>(), 4, twist: new(TwistKind.Sheltered, 0, 1));
            ModelDb.Power<HardToKillPower>().ToMutable().ApplyInternal(enemy, 9, true);
            Start(combat); Check(enemy.GetPowerAmount<HardToKillPower>() == 9, "Twist weakened native damage ceiling");
        }
        {
            var (combat, enemy, _) = Setup(ModelDb.Monster<TwoTailedRat>(), 4, twist: new(TwistKind.Reactive, 0, 1));
            ModelDb.Power<SneakyPower>().ToMutable().ApplyInternal(enemy, 3, true);
            Start(combat); Check(enemy.GetPowerAmount<SneakyPower>() == 3, "Reactive stacked above existing native amount");
        }
        Console.WriteLine("PASS boundaries: new player debuffs respect native Artifact; Purifying restores temporary Strength; Sheltered/Reactive preserve existing native amounts.");
    }
}
