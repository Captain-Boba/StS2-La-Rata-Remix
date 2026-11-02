using System.Text.Json;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;

// Executes real card effects, native pile/copy/selection/attack/draw hooks in
// TestMode. Desktop/VFX are absent; OnPlay entry and initial combat are supplied.
internal static class NovemberCardChecks
{
    private static readonly PlayerChoiceContext Context = new ThrowingPlayerChoiceContext();
    private static int checks;
    private static void Check(bool ok, string reason) { checks++; if (!ok) throw new Exception(reason); }
    private static void Await(Task task) => task.GetAwaiter().GetResult();
    internal static void Hp(Creature creature, int value) => AccessTools.PropertySetter(typeof(Creature), nameof(Creature.CurrentHp)).Invoke(creature, [value]);
    internal static (RunState Run, CombatState Combat, Player A, Player B, Creature Enemy) Setup(int count = 2)
    {
        var players = Enumerable.Range(0, count).Select(i => NativeConsoleFixture.Player((ulong)(1101 + i))).ToArray();
        var run = RunState.CreateForTest(players: players, seed: "NOVEMBERCARDS");
        NativeConsoleFixture.Set(RunManager.Instance, "State", run);
        NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.AscensionManager), new MegaCrit.Sts2.Core.Entities.Ascension.AscensionManager(0));
        var combat = new CombatState(ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable(), run);
        AccessTools.Field(typeof(CombatManager), "_state").SetValue(CombatManager.Instance, combat);
        NativeConsoleFixture.Set(CombatManager.Instance, nameof(CombatManager.IsInProgress), true);
        foreach (var player in players)
        {
            var body = new Creature(player, 100, 100) { CombatState = combat };
            NativeConsoleFixture.Set(player, nameof(player.Creature), body);
            player.ResetCombatState(); combat.AddCreature(body); player.PlayerCombatState!.Energy = 3;
        }
        var enemy = new Creature(ModelDb.Monster<TwoTailedRat>().ToMutable(), CombatSide.Enemy, "rat") { CombatState = combat };
        combat.AddCreature(enemy);
        AccessTools.PropertySetter(typeof(Creature), nameof(Creature.MaxHp)).Invoke(enemy, [1000]); Hp(enemy, 1000);
        return (run, combat, players[0], players[^1], enemy);
    }
    internal static T Card<T>(Player player, PileType pile = PileType.Hand, bool upgraded = false) where T : CardModel, new()
    {
        var card = player.Creature.CombatState!.CreateCard<T>(player);
        if (upgraded) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
        pile.GetPile(player).AddInternal(card); return card;
    }
    internal static T Power<T>(Creature creature, int amount = 1) where T : PowerModel, new()
    {
        var power = (T)ModelDb.Power<T>().ToMutable(); power.ApplyInternal(creature, amount, true); return power;
    }
    internal static void Resolve(CardModel card, Creature? target = null, CardModel? selected = null)
    {
        Await(CardPileCmd.Add(card, PileType.Play, skipVisuals: true));
        var selector = new TestCardSelector();
        selector.PrepareToSelect(selected == null ? Array.Empty<CardModel>() : [selected]);
        using var scope = CardSelectCmd.PushSelector(selector);
        var play = new CardPlay { Card = card, Target = target, ResultPile = PileType.Discard,
            Resources = new() { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 }, IsAutoPlay = false, PlayIndex = 0, PlayCount = 1 };
        Await((Task)AccessTools.Method(card.GetType(), "OnPlay").Invoke(card, [Context, play])!);
    }
    internal static void Run(Action? focused = null)
    {
        var harmony = new Harmony("LaRata.ContentChecks.NovemberCards"); NativeConsoleFixture.PatchDesktop(harmony);
        var shuffleMachine = AccessTools.Method(typeof(CardPileCmd), nameof(CardPileCmd.Shuffle))
            .GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        harmony.Patch(AccessTools.Method(shuffleMachine, "MoveNext"), transpiler: new(typeof(NovemberCardChecks), nameof(HeadlessShuffleTiming)));
        ModelDb.Inject(typeof(NovemberProbePower));
        var oldRun = RunManager.Instance.DebugOnlyGetState(); var oldAscension = RunManager.Instance.AscensionManager;
        var state = AccessTools.Field(typeof(CombatManager), "_state"); var oldState = state.GetValue(CombatManager.Instance);
        bool oldProgress = CombatManager.Instance.IsInProgress;
        var oldNet = RunManager.Instance.NetService;
        var oldLoc = LocManager.Instance;
        var oldSmart = SmartFormat.Smart.Default;
        var oldFormatter = AccessTools.Field(typeof(LocManager), "_smartFormatter").GetValue(null);
        try
        {
            var loc = (LocManager)RuntimeHelpers.GetUninitializedObject(typeof(LocManager));
            AccessTools.PropertySetter(typeof(LocManager), nameof(LocManager.Instance)).Invoke(null, [loc]);
            AccessTools.Method(typeof(LocManager), "LoadLocFormatters").Invoke(loc, null);
            LoadLocale(loc, "eng", "en-US");
            // A native, disconnected host service supplies multiplayer turn semantics;
            // no transport is started and no sockets/game clients are created.
            NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.NetService), new NetHostGameService());
            if (focused != null) { focused(); return; }
            foreach (var test in new Action[] { MetadataAndSave, Replication, DeadLetters, Parade, Overture, Promotion, Laugh, Midnight, RevisionCards, Localization })
            { test(); Console.WriteLine("PASS November " + test.Method.Name); }
            Console.WriteLine($"PASS: {checks} November card assertions, real native model/command/hook execution. Predetermined choice selector, supplied combat/OnPlay boundary and viewport animation-time substitution only; no engine/UI, disk save or live networking claim.");
        }
        finally
        {
            NativeConsoleFixture.Set(RunManager.Instance, "State", oldRun);
            NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.AscensionManager), oldAscension);
            state.SetValue(CombatManager.Instance, oldState);
            NativeConsoleFixture.Set(CombatManager.Instance, nameof(CombatManager.IsInProgress), oldProgress);
            NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.NetService), oldNet);
            AccessTools.PropertySetter(typeof(LocManager), nameof(LocManager.Instance)).Invoke(null, [oldLoc]);
            SmartFormat.Smart.Default = oldSmart;
            AccessTools.Field(typeof(LocManager), "_smartFormatter").SetValue(null, oldFormatter);
            harmony.UnpatchAll(harmony.Id);
        }
    }
    // Shuffle has one unguarded animation-timing query even in native TestMode.
    // Replace only Engine.GetMainLoop().Root.GetProcessDeltaTime(), preserving
    // native shuffle RNG, order modification, pile mutations and AfterShuffle.
    private static IEnumerable<CodeInstruction> HeadlessShuffleTiming(IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList(); int replacements = 0;
        for (int i = 0; i + 3 < codes.Count; i++)
        {
            if (codes[i].operand is not MethodInfo method || method.DeclaringType != typeof(Godot.Engine) || method.Name != "GetMainLoop") continue;
            if (codes[i + 1].opcode != OpCodes.Castclass || codes[i + 1].operand as Type != typeof(Godot.SceneTree) ||
                codes[i + 2].operand is not MethodInfo root || root.Name != "get_Root" ||
                codes[i + 3].operand is not MethodInfo delta || delta.Name != "GetProcessDeltaTime")
                throw new InvalidOperationException("Native Shuffle timing expression changed; fixture refuses a broader substitution.");
            codes[i].opcode = OpCodes.Ldc_R8; codes[i].operand = double.PositiveInfinity;
            for (int j = 1; j <= 3; j++) { codes[i + j].opcode = OpCodes.Nop; codes[i + j].operand = null; }
            replacements++;
        }
        if (replacements != 1) throw new InvalidOperationException("Expected exactly one native Shuffle viewport-time expression.");
        return codes;
    }
    internal static void LoadLocale(LocManager manager, string language, string culture)
    {
        var tables = new Dictionary<string, LocTable>();
        foreach (var directory in new[] { $".research/native-ui-20261027/localization/{language}", $"assets/LaRata/localization/{language}" })
        foreach (var file in Directory.GetFiles(directory, "*.json"))
        {
            var key = Path.GetFileNameWithoutExtension(file);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file))!;
            if (tables.TryGetValue(key, out var table)) table.MergeWith(data); else tables[key] = new LocTable(key, data);
        }
        AccessTools.Field(typeof(LocManager), "_tables").SetValue(manager, tables);
        NativeConsoleFixture.Set(manager, nameof(LocManager.CultureInfo), CultureInfo.GetCultureInfo(culture));
        NativeConsoleFixture.Set(manager, nameof(LocManager.Language), language);
    }
    private static void MetadataAndSave()
    {
        Type[] types = [typeof(RoyalReplication), typeof(DeadLetter), typeof(PlagueParade), typeof(AshenOverture), typeof(PawnPromotion), typeof(LastLaugh)];
        var entries = LaRataCardCatalog.Entries.Where(e => types.Contains(e.Model.GetType())).ToArray();
        Check(entries.Length == 6 && entries.All(e => e.PlayerPlayable && e.BossId == null && !e.BossEligible && e.BoosterEligibility == LaRataBoosterEligibility.Neutral), "New cards must be player neutrals only");
        Check(LaRataCardCatalog.BossCards.Count == 48 && LaRataCardCatalog.BossPrizes.Length == 10, "Boss identities/prizes changed");
        Check(LaRataCardCatalog.Neutrals.Length == 34 && LaRataCardCatalog.Neutrals.Count(c => c.Rarity == CardRarity.Uncommon) == 21 && LaRataCardCatalog.Neutrals.Count(c => c.Rarity == CardRarity.Rare) == 13, "Neutral counts differ");
        Check(BoosterRelic.ClassMainRarityWeights.Select(x => x.Weight).SequenceEqual(new[] { 60, 30, 10 }) && BoosterRelic.NeutralRarityWeights.Select(x => x.Weight).SequenceEqual(new[] { 75, 25 }), "Existing rarity weights changed");
        foreach (var entry in entries)
        {
            var card = entry.Model.ToMutable();
            Check(card.EnergyCost.Canonical == (card is LastLaugh or DeadLetter ? 2 : 1), "Card cost " + card.Id);
            Check(card.Keywords.Contains(CardKeyword.Exhaust) == (card is RoyalReplication or PawnPromotion), "Exhaust " + card.Id);
            Check(card.Rarity == (card is RoyalReplication or PawnPromotion or LastLaugh ? CardRarity.Rare : CardRarity.Uncommon), "Rarity " + card.Id);
            card.UpgradeInternal(); card.FinalizeUpgradeInternal();
            var saved = JsonSerializer.Serialize(card.ToSerializable(), JsonSerializationUtility.GetTypeInfo<SerializableCard>());
            var restored = CardModel.FromSerializable(JsonSerializer.Deserialize(saved, JsonSerializationUtility.GetTypeInfo<SerializableCard>())!);
            Check(restored.CurrentUpgradeLevel == 1 && restored.Id == card.Id && restored.DynamicVars.All(v => v.Value.BaseValue == card.DynamicVars[v.Key].BaseValue), "Native card JSON upgrade roundtrip " + card.Id);
        }
        var (_, _, a, _, _) = Setup();
        var bonus = BoosterRelic.NeutralBonusPool(ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(a.UnlockState, a.RunState.CardMultiplayerConstraint), []);
        Check(entries.All(e => bonus.Any(c => c.Id == e.ModelId)), "New cards missing from native bonus candidates");
    }
    private static void Replication()
    {
        foreach (bool upgraded in new[] { false, true }) foreach (int count in new[] { 1, 2, 4 })
        {
            string[]? previous = null;
            for (int replay = 0; replay < 2; replay++)
            {
                var (run, _, a, b, _) = Setup(count);
                var original = Card<Bash>(a, PileType.Draw, true);
                original.EnchantInternal(ModelDb.Enchantment<Sharp>().ToMutable(), 2);
                var wound = Card<Wound>(a, PileType.Draw); var curse = Card<Injury>(a, PileType.Draw);
                var originals = new CardModel[] { original, wound, curse };
                if (a != b) Card<StrikeIronclad>(b, PileType.Draw);
                int deckCount = a.Deck.Cards.Count;
                Resolve(Card<RoyalReplication>(a, upgraded: upgraded));
                var cards = a.PlayerCombatState!.DrawPile.Cards;
                Check(cards.Count == (upgraded ? 9 : 6), "Replication wrong multiplier/recursive clone");
                Check(cards.Where(originals.Contains).SequenceEqual(originals), "Replication shuffled originals");
                var copies = cards.Where(c => !originals.Contains(c)).ToArray();
                Check(copies.All(c => c.Owner == a && c.DeckVersion == null && originals.Contains(c.CloneOf!)), "Replication not native owner-local combat clones");
                Check(copies.OfType<Bash>().All(c => c.IsUpgraded && c.DynamicVars.Damage.BaseValue == 10 && c.Enchantment?.Amount == 2 && !ReferenceEquals(c.Enchantment, original.Enchantment)), "Clone upgrade/enchantment preservation");
                Check(copies.Count(c => c.Type == CardType.Status) == (upgraded ? 2 : 1) && copies.Count(c => c.Type == CardType.Curse) == (upgraded ? 2 : 1), "Status/Curse omitted");
                Check(a.Deck.Cards.Count == deckCount && (a == b || b.PlayerCombatState!.DrawPile.Cards.Count == 1), "Replication touched permanent/other deck");
                var order = cards.Select(c => c.Id.Entry + ":" + Array.IndexOf(originals, c)).ToArray();
                Check(previous == null || previous.SequenceEqual(order), "Seed replay replication order"); previous = order;
            }
        }
        {
            var (_, _, a, _, _) = Setup(); Resolve(Card<RoyalReplication>(a));
            Check(a.PlayerCombatState!.DrawPile.IsEmpty, "Empty replication generates cards");
            Card<Wound>(a, PileType.Draw); Card<Injury>(a, PileType.Draw);
            Power<NovemberProbePower>(a.Creature).Generated = (_, _) => { Hp(a.Creature, 0); return Task.CompletedTask; };
            Resolve(Card<RoyalReplication>(a, upgraded: true));
            Check(a.PlayerCombatState.DrawPile.Cards.Count == 3, "Replication continues after owner death");
        }
        {
            var (_, _, a, _, _) = Setup(); var original = Card<Bash>(a, PileType.Draw);
            Power<NovemberProbePower>(a.Creature).Generated = (_, _) => { if (original.IsUpgradable) CardCmd.Upgrade(original); return Task.CompletedTask; };
            Resolve(Card<RoyalReplication>(a, upgraded: true));
            Check(a.PlayerCombatState!.DrawPile.Cards.Where(c => c != original).All(c => !c.IsUpgraded), "Generation hook mutated not-yet-added copies after starting snapshot");
        }
    }
    private static void DeadLetters()
    {
        foreach (bool upgraded in new[] { false, true }) foreach (bool empty in new[] { false, true })
        {
            var (_, _, a, b, _) = Setup(); var foreign = Card<Bash>(b, PileType.Exhaust);
            var top = Card<StrikeIronclad>(a, PileType.Draw);
            var exhausted = empty ? null : Card<Bash>(a, PileType.Exhaust, true);
            var letter = Card<DeadLetter>(a, upgraded: upgraded);
            Check(letter.EnergyCost.GetWithModifiers(CostModifiers.None) == (upgraded ? 1 : 2) && !letter.GainsBlock, "Death Letter cost/no Block metadata");
            Resolve(letter, selected: exhausted);
            Check(a.Creature.Block == 0, "Death Letter must not grant Block");
            Check(a.PlayerCombatState!.DrawPile.Cards[0] == ((CardModel?)exhausted ?? top) && (exhausted == null || exhausted.IsUpgraded), "Dead Letter top/identity/upgrade");
            Check(foreign.Pile?.Type == PileType.Exhaust && b.Creature.Block == 0, "Dead Letter touches other owner");
            var energy = a.Creature.GetPower<EnergyNextTurnPower>()!;
            Check(energy.Amount == 1 && a.PlayerCombatState.Energy == 3, "Dead Letter energy paid early");
            Await(energy.AfterEnergyReset(b)); Check(a.PlayerCombatState.Energy == 3, "Ally reset consumed delayed energy");
            Await(energy.AfterEnergyReset(a));
            Check(a.PlayerCombatState.Energy == 4 && !a.Creature.HasPower<EnergyNextTurnPower>(), "Native next-turn energy payout/removal");
        }
    }
    private static void Parade()
    {
        foreach (bool upgraded in new[] { false, true }) foreach (int block in new[] { 0, 4, 50 })
        {
            var (_, _, a, b, enemy) = Setup(); enemy.GainBlockInternal(block);
            int damage = upgraded ? 9 : 6;
            Resolve(Card<PlagueParade>(a, upgraded: upgraded), enemy);
            Check(enemy.GetPowerAmount<PoisonPower>() == Math.Max(0, damage - block), "Poison != native unblocked damage");
            Check(enemy.CurrentHp == 1000 - Math.Max(0, damage - block) && b.Creature.CurrentHp == 100, "Parade damage/owner");
        }
        {
            var (_, _, a, _, enemy) = Setup(); Power<StrengthPower>(a.Creature, 2); Power<VulnerablePower>(enemy, 1); enemy.GainBlockInternal(3);
            Resolve(Card<PlagueParade>(a), enemy); Check(enemy.GetPowerAmount<PoisonPower>() == 9, "Strength/Vulnerable/block receipt");
        }
        {
            var (_, _, a, _, enemy) = Setup(); Power<BufferPower>(enemy); Resolve(Card<PlagueParade>(a), enemy);
            Check(enemy.CurrentHp == 1000 && enemy.GetPowerAmount<PoisonPower>() == 0, "Buffer generated poison without damage");
        }
        {
            var (_, _, a, _, enemy) = Setup(); Power<ArtifactPower>(enemy); Resolve(Card<PlagueParade>(a), enemy);
            Check(enemy.GetPowerAmount<PoisonPower>() == 0 && enemy.GetPowerAmount<ArtifactPower>() == 0, "Poison bypassed Artifact");
        }
        {
            var (_, _, a, _, enemy) = Setup(); Hp(a.Creature, 1); Power<ThornsPower>(enemy, 2);
            Resolve(Card<PlagueParade>(a), enemy); Check(a.Creature.IsDead && enemy.GetPowerAmount<PoisonPower>() == 0, "Thorns death did not stop follow-up");
        }
    }
    private static void Overture()
    {
        foreach (bool upgraded in new[] { false, true }) foreach (int cards in new[] { 0, 1, 9 })
        {
            var (_, _, a, b, _) = Setup(); for (int i = 0; i < cards; i++) Card<DefendIronclad>(a);
            for (int i = 0; i < 4; i++) Card<StrikeIronclad>(a, PileType.Draw);
            Card<DefendIronclad>(b); Resolve(Card<AshenOverture>(a, upgraded: upgraded));
            Check(a.PlayerCombatState!.Hand.Cards.Count == 3 && a.PlayerCombatState.Hand.Cards.All(c => c is StrikeIronclad), "Overture draws three after discards, also from an empty hand");
            Check(a.Creature.Block == cards * (upgraded ? 3 : 2) && a.Creature.GetPowerAmount<VigorPower>() == cards * (upgraded ? 3 : 2), "Overture base/upgraded totals");
            Check(b.PlayerCombatState!.Hand.Cards.Count == 1 && b.Creature.Block == 0, "Overture other owner");
        }
        {
            var (_, _, a, _, _) = Setup(); var first = Card<DefendIronclad>(a); var moved = Card<Bash>(a);
            var probe = Power<NovemberProbePower>(a.Creature);
            probe.Discarded = async (ctx, card) => { if (card == first) { await CardCmd.Exhaust(ctx, moved); Card<StrikeIronclad>(a); } };
            Resolve(Card<AshenOverture>(a));
            Check(a.Creature.Block == 2 && a.Creature.GetPowerAmount<VigorPower>() == 2 && moved.Pile?.Type == PileType.Exhaust, "Stale hand snapshot counted/moved exhausted fuel");
            Check(a.PlayerCombatState!.Hand.Cards.Any(c => c is StrikeIronclad), "Hook-generated hand card got discarded recursively");
        }
        {
            var (_, _, a, _, _) = Setup(); Card<DefendIronclad>(a); Card<Bash>(a);
            Power<NovemberProbePower>(a.Creature).Discarded = (_, _) => { Hp(a.Creature, 0); return Task.CompletedTask; };
            Resolve(Card<AshenOverture>(a)); Check(a.Creature.Block == 0 && a.Creature.GetPowerAmount<VigorPower>() == 0 && a.PlayerCombatState!.Hand.Cards.Count == 1, "Overture continued after lethal discard hook");
        }
        {
            var (_, _, a, b, _) = Setup(); var first = Card<Bash>(a); var second = Card<DefendIronclad>(a); var foreign = Card<DefendIronclad>(b);
            Power<DexterityPower>(a.Creature, 2);
            Power<NovemberProbePower>(a.Creature).Discarded = async (ctx, card) =>
            { if (card == first) { await CardCmd.Discard(ctx, second); await CardCmd.Discard(ctx, foreign); } };
            Resolve(Card<AshenOverture>(a));
            Check(a.Creature.Block == 8 && a.Creature.GetPowerAmount<VigorPower>() == 4, "Actual nested original-hand discards/per-card Dexterity/foreign exclusion");
        }
    }
    private static void Promotion()
    {
        foreach (int count in new[] { 1, 2, 4 })
        {
            var (_, _, a, _, _) = Setup(count);
            var all = PawnPromotion.GenerationPool(a);
            Check(all.Length > 0 && all.All(c => c.Rarity == CardRarity.Rare && c.CanBeGeneratedInCombat && c.Type is CardType.Attack or CardType.Skill or CardType.Power), "Promotion eligibility");
            Check(all.All(c => !c.EnergyCost.CostsX && !c.HasStarCostX), "Promotion must honor actual 0-cost upgrade, not X exceptions");
            Check(!all.Intersect(LaRataCardCatalog.BossPrizes).Any() && !all.Intersect(LaRataCardCatalog.BossCards.Where(c => c is RoyalCard)).Any(), "Boss/prize in promotion pool");
            NativeConsoleFixture.Set(a, nameof(a.UnlockState), UnlockState.none);
            var locked = PawnPromotion.GenerationPool(a);
            var allowed = a.UnlockState.CharacterCardPools.Select(p => p.Id).ToHashSet();
            Check(locked.All(c => c.GetType().Assembly == typeof(ModEntry).Assembly || allowed.Contains(c.Pool.Id)), "Locked character pool leaked");
            Check(locked.All(c => all.Any(x => x.Id == c.Id)), "Locked pool grew");
        }
        foreach (bool upgraded in new[] { false, true })
        {
            string? replayId = null;
            for (int replay = 0; replay < 2; replay++)
            {
                var (_, _, a, b, _) = Setup(); var fuel = Card<DefendIronclad>(a);
                Resolve(Card<PawnPromotion>(a, upgraded: upgraded), selected: fuel);
                var reward = a.PlayerCombatState!.Hand.Cards.Single();
                Check(fuel.Pile?.Type == PileType.Exhaust && reward.IsUpgraded && reward.Rarity == CardRarity.Rare && reward.Owner == a && reward.DeckVersion == null, "Promotion native fuel/reward identity");
                Check(replayId == null || replayId == reward.Id.Entry, "Promotion RNG replay differs"); replayId = reward.Id.Entry;
                if (upgraded && !reward.EnergyCost.CostsX) Check(reward.EnergyCost.GetWithModifiers(CostModifiers.All) == 0, "Promotion+ not free this turn");
                reward.EndOfTurnCleanup();
                Check(!upgraded || reward.EnergyCost.GetWithModifiers(CostModifiers.All) == reward.EnergyCost.GetWithModifiers(CostModifiers.None), "Free cost survived end of turn");
                Check(b.PlayerCombatState!.AllCards.Count() == 0 && a.Deck.IsEmpty, "Promotion touched ally/permanent deck");
            }
        }
        {
            var (_, _, a, _, _) = Setup(); Resolve(Card<PawnPromotion>(a)); Check(a.PlayerCombatState!.Hand.IsEmpty, "Empty fuel generated reward");
        }
        {
            var (_, _, a, _, _) = Setup(); var fuel = Card<DefendIronclad>(a);
            // Native exhaust pile event refills hand before generation: overflow belongs in Discard.
            a.PlayerCombatState!.ExhaustPile.CardAdded += _ => { for (int i = 0; i < 10; i++) Card<StrikeIronclad>(a); };
            Resolve(Card<PawnPromotion>(a), selected: fuel);
            Check(a.PlayerCombatState.Hand.Cards.Count == 10 && a.PlayerCombatState.DiscardPile.Cards.Single().IsUpgraded, "Promotion hand cap/overflow");
        }
    }
    private static void Laugh()
    {
        foreach (bool upgraded in new[] { false, true })
        {
            var (_, _, a, b, _) = Setup(); Resolve(Card<LastLaugh>(a, upgraded: upgraded));
            Card<Wound>(b, PileType.Draw); Await(CardPileCmd.Draw(Context, 1, b));
            Check(a.Creature.Block == 0 && b.Creature.Block == 0, "Partner draw triggered Last Laugh");
            Card<Wound>(a, PileType.Draw); Card<Injury>(a, PileType.Draw);
            for (int i = 0; i < 5; i++) Card<StrikeIronclad>(a, PileType.Draw);
            Await(CardPileCmd.Draw(Context, 1, a));
            Check(a.PlayerCombatState!.Hand.Cards.Count == (upgraded ? 5 : 3) && a.Creature.Block == (upgraded ? 10 : 6), "Status/Curse draw chain/block");
            var power = a.Creature.GetPower<LastLaughPower>()!;
            var description = power.Description; description.Add("Amount", power.Amount);
            Check(power.DynamicVars.Cards.IntValue == (upgraded ? 2 : 1) && !description.GetFormattedText().Contains('{'), "Last Laugh instance draw count/description");
        }
        {
            var (_, _, a, _, _) = Setup(); Resolve(Card<LastLaugh>(a));
            for (int i = 0; i < 50; i++) Card<Wound>(a, PileType.Draw);
            Await(CardPileCmd.Draw(Context, 1, a));
            Check(a.PlayerCombatState!.Hand.Cards.Count == 10 && a.Creature.Block == 30 && a.PlayerCombatState.DrawPile.Cards.Count == 40, "Last Laugh hand limit/recursive chain");
        }
        {
            var (_, _, a, _, _) = Setup(); Resolve(Card<LastLaugh>(a));
            Card<Wound>(a, PileType.Draw); Card<StrikeIronclad>(a, PileType.Discard);
            Await(CardPileCmd.Draw(Context, 1, a));
            Check(a.PlayerCombatState!.Hand.Cards.Count == 2 && a.Creature.Block == 3, "Last Laugh native reshuffle");
        }
        {
            var (_, _, a, _, _) = Setup(); Resolve(Card<LastLaugh>(a));
            Card<Wound>(a, PileType.Draw); Card<StrikeIronclad>(a, PileType.Draw);
            Power<NovemberProbePower>(a.Creature).BlockGained = (_, _, _, _) => { Hp(a.Creature, 0); return Task.CompletedTask; };
            Await(CardPileCmd.Draw(Context, 1, a));
            Check(a.PlayerCombatState!.Hand.Cards.Count == 1, "Last Laugh drew after owner death during block hook");
        }
        {
            var (_, _, a, _, _) = Setup(); Resolve(Card<LastLaugh>(a)); Resolve(Card<LastLaugh>(a, upgraded: true));
            Card<Wound>(a, PileType.Draw); for (int i = 0; i < 4; i++) Card<StrikeIronclad>(a, PileType.Draw);
            Await(CardPileCmd.Draw(Context, 1, a, fromHandDraw: true));
            Check(a.Creature.Block == 8 && a.PlayerCombatState!.Hand.Cards.Count == 4, "Each Last Laugh copy grants its own Block/draw upgrade, including initial hand draw");
        }
    }
    private static void Midnight()
    {
        foreach (bool upgraded in new[] { false, true }) foreach (bool empty in new[] { false, true })
        {
            var (_, combat, a, b, _) = Setup(); var fuel = empty ? null : Card<Bash>(a);
            for (int i = 0; i < 5; i++) Card<StrikeIronclad>(a, PileType.Draw);
            bool drawStarted = false;
            var probe = Power<NovemberProbePower>(a.Creature);
            probe.Drawn = (_, card, _) => { if (card.Owner == a) drawStarted = true; return Task.CompletedTask; };
            probe.Discarded = (_, card) => { Check(!drawStarted, "Midnight drew before discard"); return Task.CompletedTask; };
            Resolve(Card<MidnightRehearsal>(a, upgraded: upgraded), selected: fuel);
            Check(a.PlayerCombatState!.Hand.Cards.Count == (upgraded ? 3 : 2), "Midnight immediate draw count");
            Check(empty || fuel!.Pile?.Type == PileType.Discard, "Midnight selection not discarded");
            var next = a.Creature.GetPower<DrawCardsNextTurnPower>()!;
            Check(next.Amount == 1 && next.ModifyHandDraw(a, 5) == 5, "Midnight next draw early");
            Await(Hook.BeforeSideTurnStart(combat, CombatSide.Player, [a.Creature, b.Creature]));
            a.Creature.BeforeTurnStart(CombatSide.Player);
            b.Creature.BeforeTurnStart(CombatSide.Player);
            Check(next.ModifyHandDraw(a, 5) == 6 && next.ModifyHandDraw(b, 5) == 5, "Midnight native next draw/owner");
            Await(next.AfterSideTurnStart(CombatSide.Player, [a.Creature, b.Creature], combat));
            Check(!a.Creature.HasPower<DrawCardsNextTurnPower>(), "Midnight delayed draw not consumed");
        }
    }
    private static void Localization()
    {
        Type[] types = [typeof(RoyalReplication), typeof(DeadLetter), typeof(PlagueParade), typeof(AshenOverture), typeof(PawnPromotion), typeof(LastLaugh), typeof(MidnightRehearsal), typeof(CinderReceipt), typeof(LaRata.Cleanse), typeof(ScrapCoronation), typeof(SerKevsThornBuster), typeof(SewerPact), typeof(SmugglersDividend), typeof(VenomousEncore)];
        foreach (var (language, culture) in new[] { ("eng", "en-US"), ("deu", "de-DE"), ("zhs", "zh-CN") })
        {
            LoadLocale(LocManager.Instance, language, culture);
            foreach (var type in types) foreach (bool upgraded in new[] { false, true })
            {
                var card = ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable();
                if (upgraded) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
                var text = card.GetDescriptionForPile(PileType.None);
                Check(!text.Contains('{') && !text.Contains('}'), $"Unresolved native description {language}/{card.Id}/{upgraded}");
                Check(!text.Contains("If you do") && !text.Contains("up to"), "Verbose card condition");
                if (language == "eng" && card is RoyalReplication)
                    Check(text.StartsWith(upgraded ? "Triple" : "Double"), "Replication upgrade text");
                if (language == "eng" && card is PawnPromotion)
                    Check(text.Contains("It costs 0 this turn.") == upgraded, "Promotion upgraded-only cost text");
            }
            var powerText = ModelDb.Power<LastLaughPower>().Description;
            powerText.Add("Amount", 5);
            Check(!powerText.GetFormattedText().Contains('{'), "Power amount localization");
        }
    }

    private static void RevisionCards()
    {
        Check(LaRataCardCatalog.Entries.Where(e => e.BossId.HasValue && !e.BossEligible).Select(e => e.BossId!.Value).Order().SequenceEqual(new[] { 3, 20, 21, 25, 26, 28, 47 }), "Boss exclusions changed identities/player admission");
        Check(LaRataCardCatalog.GetBossEntry(25).PlayerPlayable && LaRataCardCatalog.Neutrals.Any(c => c is LaRata.Cleanse), "Royal Cleansing boss exclusion removed player/booster admission");
        Check(LaRataCardCatalog.Neutrals.Any(c => c is GraveSpark) && LaRataCardCatalog.Neutrals.Any(c => c is SewerPact) && LaRataCardCatalog.BossPrizes.Any(c => c is RoyalTithePrize), "Boss exclusion leaked into player/prize pools");
        foreach (bool upgraded in new[] { false, true })
        {
            {
                var (_, _, a, b, enemy) = Setup(); Power<ThornsPower>(enemy, 7); Power<ThornsPower>(b.Creature, 3);
                for (int i = 0; i < 4; i++) Card<StrikeIronclad>(a, PileType.Draw);
                var thorn = Card<SerKevsThornBuster>(a, upgraded: upgraded); Resolve(thorn);
                Check(thorn.EnergyCost.Canonical == 1 && thorn.Keywords.Contains(CardKeyword.Exhaust), "Thorn cost/Exhaust");
                Check(!enemy.HasPower<ThornsPower>() && enemy.GetPowerAmount<VulnerablePower>() == (upgraded ? 3 : 2) && b.Creature.GetPowerAmount<ThornsPower>() == 3, "Thorn removal/conditional Vulnerable/owner");
                Check(a.PlayerCombatState!.Hand.Cards.Count == (upgraded ? 2 : 1), "Thorn draw upgrade");
            }
            {
                var (_, _, a, _, enemy) = Setup(); Power<ArtifactPower>(enemy); Resolve(Card<SerKevsThornBuster>(a, upgraded: upgraded));
                Check(enemy.GetPowerAmount<ArtifactPower>() == 1 && !enemy.HasPower<VulnerablePower>(), "Thorn applied Vulnerable without Thorns loss");
            }
            {
                var (_, _, a, _, _) = Setup(); var fuel = Card<Bash>(a);
                Resolve(Card<CinderReceipt>(a, upgraded: upgraded), selected: fuel);
                Check(fuel.Pile?.Type == PileType.Exhaust && a.Creature.GetPowerAmount<VigorPower>() == (upgraded ? 15 : 10), "Cinder actual exhaust/Vigor");
            }
            {
                var (_, _, a, _, enemy) = Setup(); Resolve(Card<SewerPact>(a, upgraded: upgraded), enemy);
                Check(enemy.GetPowerAmount<PoisonPower>() == (upgraded ? 8 : 5) && enemy.GetPowerAmount<DoomPower>() == (upgraded ? 8 : 5), "Sewer Pact Poison/Doom");
            }
            foreach (bool poisoned in new[] { false, true })
            {
                var (_, _, a, _, enemy) = Setup(); if (poisoned) Power<PoisonPower>(enemy, 1);
                Resolve(Card<VenomousEncore>(a, upgraded: upgraded), enemy);
                Check(enemy.CurrentHp == 1000 - 2 * (upgraded ? 7 : 5) && enemy.GetPowerAmount<DoomPower>() == (poisoned ? (upgraded ? 10 : 5) : 0), "Encore hits/conditional Doom");
            }
            foreach (bool fuelPresent in new[] { false, true })
            {
                var (_, _, a, _, _) = Setup(); var fuel = fuelPresent ? Card<Bash>(a) : null;
                Card<StrikeIronclad>(a, PileType.Draw); Resolve(Card<ScrapCoronation>(a, upgraded: upgraded), selected: fuel);
                Check(a.PlayerCombatState!.Hand.Cards.Count(c => c is StrikeIronclad) == 1, "Scrap unconditional additional draw");
                Check(!fuelPresent || fuel!.Pile?.Type == PileType.Exhaust, "Scrap fuel not exhausted");
            }
            {
                var (_, _, a, b, _) = Setup(); Resolve(Card<SmugglersDividend>(a, upgraded: upgraded));
                for (int i = 0; i < 3; i++) Await(CardCmd.Discard(Context, Card<StrikeIronclad>(a)));
                Await(CardCmd.Discard(Context, Card<StrikeIronclad>(b)));
                Check(a.Creature.GetPowerAmount<VigorPower>() == 3 * (upgraded ? 5 : 3) && b.Creature.GetPowerAmount<VigorPower>() == 0, "Dividend must reward every actual own discard");
                Await(CardCmd.Exhaust(Context, Card<DefendIronclad>(a)));
                Check(a.Creature.GetPowerAmount<VigorPower>() == 3 * (upgraded ? 5 : 3), "Exhaust incorrectly counted as discard");
            }
        }
    }
}

// Fixture-only native hook observer. No product command is replaced by a fake.
internal sealed class NovemberProbePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    internal Func<PlayerChoiceContext, CardModel, Task>? Discarded;
    internal Func<PlayerChoiceContext, CardModel, bool, Task>? Drawn;
    internal Func<CardModel, Player?, Task>? Generated;
    internal Func<Creature, decimal, ValueProp, CardModel?, Task>? BlockGained;
    public override Task AfterCardDiscarded(PlayerChoiceContext ctx, CardModel card) => Discarded?.Invoke(ctx, card) ?? Task.CompletedTask;
    public override Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw) => Drawn?.Invoke(ctx, card, fromHandDraw) ?? Task.CompletedTask;
    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator) => Generated?.Invoke(card, creator) ?? Task.CompletedTask;
    public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource) => BlockGained?.Invoke(creature, amount, props, cardSource) ?? Task.CompletedTask;
}
