using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using BaseLib.Patches.Saves;
using System.Text.Json;

internal static class PaymentMetricChecks
{
    private static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }
    private static decimal Value(RelicModel relic, string key) => RelicMetrics.Read(relic).Metrics.Where(m => m.Key == key).Sum(m => m.Value);
    private static bool InProgress(ref bool __result) { __result = true; return false; }
    private static bool Active(ref bool __result) { __result = false; return false; }
    private static T Obtain<T>(Player owner) where T : RelicModel, new()
    { var relic = (T)ModelDb.Relic<T>().ToMutable(); owner.AddRelicInternal(relic); return relic; }
    private static void Scoped(RelicModel? relic, Action action)
    { var old = RelicMetricSource.Enter(relic); try { action(); } finally { RelicMetricSource.Restore(old); } }
    private static void Flush(Player owner, CardModel card) => Hook.AfterFlush(owner.Creature.CombatState!, owner, new BlockingPlayerChoiceContext(), [], [card]).GetAwaiter().GetResult();
    private static (Player owner, Player other, RunState run) Setup()
    {
        var owner = NativeConsoleFixture.Player(501); var other = NativeConsoleFixture.Player(502);
        var run = RunState.CreateForTest(players: [owner, other], seed: "PAYMENTMETRICS");
        NativeConsoleFixture.Set(RunManager.Instance, "State", run);
        var combat = new CombatState(ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable(), run);
        foreach (var p in new[] { owner, other })
        {
            var body = new Creature(p, 50, 50) { CombatState = combat };
            NativeConsoleFixture.Set(p, nameof(p.Creature), body); p.ResetCombatState(); combat.AddCreature(body);
            p.PlayerCombatState!.Energy = 10; p.PlayerCombatState.Stars = 10;
        }
        return (owner, other, run);
    }
    private static T Card<T>(RunState run, Player owner) where T : CardModel, new()
    { var card = run.CreateCard<T>(owner); owner.PlayerCombatState!.Hand.AddInternal(card); return card; }
    private static void Preview(CardModel card, RelicModel source)
    {
        for (int i = 0; i < 8; i++) { _ = card.EnergyCost.GetAmountToSpend(); _ = card.GetStarCostWithModifiers(); }
        Require(Value(source, "energy_saved") == 0 && Value(source, "stars_saved") == 0 && Value(source, "energy_cost_adjustment_used") == 0, "Preview/discount assignment counted payment");
    }
    private static void ExplainModifiers(CardModel card, string label)
    {
        var table = AccessTools.Field(typeof(ModEntry).Assembly.GetType("LaRata.RelicPaymentMetrics"), "LocalSources").GetValue(null)!;
        foreach (var modifier in (IEnumerable<LocalCostModifier>)AccessTools.Field(typeof(CardEnergyCost), "_localModifiers").GetValue(card.EnergyCost)!)
        {
            object?[] args = [modifier, null];
            bool tagged = (bool)table.GetType().GetMethod("TryGetValue")!.Invoke(table, args)!;
            Console.WriteLine($"TRACE {label}: modifier {modifier.Type}/{modifier.Amount}, tagged={tagged}, source={args[1]}");
        }
    }
    internal static void Run(Harmony harmony)
    {
        var oldState = RunManager.Instance.DebugOnlyGetState();
        bool oldProgress = CombatManager.Instance.IsInProgress;
        NativeConsoleFixture.Set(CombatManager.Instance, nameof(CombatManager.IsInProgress), true);
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsInProgress)), prefix: new(typeof(PaymentMetricChecks), nameof(InProgress)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsOverOrEnding)), prefix: new(typeof(PaymentMetricChecks), nameof(Active)));
        var failures = new List<Exception>();
        try
        {
            foreach (var (name, action) in new (string, Action)[] {
                ("native Mummified Hand + expiry", Mummified), ("native Mummified stars", Stars), ("native Bookmark + actual debit", BookmarkPayment),
                ("native Brilliant Scarf and Spiked Gauntlets", Global), ("combined and overwritten modifiers", Combined),
                ("clamped debit and owner isolation", Clamped), ("marked effect after source removal", Removed), ("native cost clone provenance", Clone),
                ("deferred gap native JSON/archive checkpoint", GapPersistence) })
            { try { action(); Console.WriteLine("PASS: " + name); } catch (Exception e) { failures.Add(e); Console.WriteLine("FAIL: " + name + ": " + e); } }
        }
        finally { NativeConsoleFixture.Set(RunManager.Instance, "State", oldState); NativeConsoleFixture.Set(CombatManager.Instance, nameof(CombatManager.IsInProgress), oldProgress); }
        if (failures.Count != 0) throw new AggregateException(failures);
        Console.WriteLine("PASS: targeted native SpendResources/actual setter checks; combat guards supplied and direct hook invocation. No full card-play, real multiplayer or complete save/load acceptance.");
    }
    private static void Mummified()
    {
        foreach (bool expire in new[] { false, true })
        {
            var (owner, _, run) = Setup(); var source = Obtain<MummifiedHand>(owner);
            var card = Card<DemonForm>(run, owner);
            source.AfterCardPlayed(new BlockingPlayerChoiceContext(), TesterCardChecks.Play(card)).GetAwaiter().GetResult();
            Require(card.EnergyCost.GetAmountToSpend() == 0, $"Native Mummified Hand did not discount the sole candidate; cost={card.EnergyCost.GetAmountToSpend()}, local={card.EnergyCost.HasLocalModifiers}, progress={CombatManager.Instance.IsInProgress}");
            Preview(card, source);
            if (expire) card.EnergyCost.EndOfTurnCleanup();
            int before = owner.PlayerCombatState!.Energy;
            var paid = card.SpendResources().GetAwaiter().GetResult();
            Require(paid.Item1 == (expire ? 3 : 0) && before - owner.PlayerCombatState.Energy == paid.Item1 && Value(source, "energy_saved") == (expire ? 0 : 3), "Mummified expired/used discount attribution differs");
        }
    }
    private static void BookmarkPayment()
    {
        var (owner, _, run) = Setup(); var source = Obtain<Bookmark>(owner); var card = Card<DemonForm>(run, owner);
        source.Flashed += (_, _) => Console.WriteLine($"TRACE Bookmark.Flash context={RelicMetricSource.Attribution}, id={RelicMetrics.Acquisition.Get(source)}, owner={source.Owner.NetId}");
        var info = Harmony.GetPatchInfo(AccessTools.Method(typeof(Bookmark), nameof(Bookmark.AfterFlush)));
        Console.WriteLine("TRACE Bookmark patches=" + string.Join(',', info?.Prefixes.Select(p => p.PatchMethod.DeclaringType + "." + p.PatchMethod.Name) ?? []));
        Flush(owner, card);
        ExplainModifiers(card, "Bookmark");
        Preview(card, source);
        int before = owner.PlayerCombatState!.Energy;
        var paid = card.SpendResources().GetAwaiter().GetResult();
        Require(paid.Item1 == 2 && before - owner.PlayerCombatState.Energy == 2 && Value(source, "energy_saved") == 1 && Value(source, "energy_cost_adjustment_used") == 0, $"Bookmark payment not exact once; requested={paid}, actual={before-owner.PlayerCombatState.Energy}, saved={Value(source, "energy_saved")}, contribution={Value(source, "energy_cost_adjustment_used")}");
    }
    private static void Stars()
    {
        var (owner, other, run) = Setup(); var source = Obtain<MummifiedHand>(owner);
        var card = Card<FallingStar>(run, owner);
        // The played Power is outside Hand; Falling Star is the sole selectable candidate.
        var trigger = run.CreateCard<DemonForm>(owner);
        source.AfterCardPlayed(new BlockingPlayerChoiceContext(), TesterCardChecks.Play(trigger)).GetAwaiter().GetResult();
        Require(card.GetStarCostWithModifiers() == 0, "Native Mummified star discount absent");
        Preview(card, source);
        var paid = card.SpendResources().GetAwaiter().GetResult();
        Require(paid == (0, 0) && owner.PlayerCombatState!.Stars == 10 && other.PlayerCombatState!.Stars == 10 && Value(source, "stars_saved") == 2 && Value(source, "energy_saved") == 0, "Native two-star saving not isolated/exact");
    }
    private static void Global()
    {
        {
            var (owner, _, run) = Setup(); var source = Obtain<BrilliantScarf>(owner); var card = Card<TheKikilyst>(run, owner);
            for (int i = 0; i < 4; i++) source.AfterCardPlayed(new BlockingPlayerChoiceContext(), TesterCardChecks.Play(card)).GetAwaiter().GetResult();
            Preview(card, source);
            Require(card.SpendResources().GetAwaiter().GetResult().Item1 == 0 && Value(source, "energy_saved") == 2, "Scarf fifth card did not save two energy");
        }
        {
            var (owner, _, run) = Setup(); var source = Obtain<SpikedGauntlets>(owner); var card = Card<DemonForm>(run, owner);
            Preview(card, source);
            Require(card.SpendResources().GetAwaiter().GetResult().Item1 == 4 && Value(source, "energy_surcharge_paid") == 1, "Native surcharge not counted as actual payment");
        }
    }
    private static void Combined()
    {
        foreach (bool overwrite in new[] { false, true })
        {
            var (owner, _, run) = Setup(); var first = Obtain<Bookmark>(owner); var second = Obtain<MummifiedHand>(owner); var card = Card<DemonForm>(run, owner);
            Flush(owner, card);
            if (overwrite) Scoped(null, () => card.EnergyCost.SetUntilPlayed(1));
            else second.AfterCardPlayed(new BlockingPlayerChoiceContext(), TesterCardChecks.Play(card)).GetAwaiter().GetResult();
            card.SpendResources().GetAwaiter().GetResult();
            Require(Value(first, "energy_saved") == 0 && Value(second, "energy_saved") == 0 && Value(first, "energy_cost_adjustment_used") == 0 &&
                Value(second, "energy_cost_adjustment_used") == 0 && RelicMetrics.Read(first).Gaps.Contains("complex_payments_deferred"), "Ambiguous payment created a value or lacked deferred flag");
        }
    }
    private static void Clamped()
    {
        var (owner, other, run) = Setup(); var source = Obtain<Bookmark>(owner); var foreign = Obtain<Bookmark>(other);
        var card = Card<DemonForm>(run, owner); var otherCard = Card<DemonForm>(run, other);
        Flush(owner, card);
        Flush(other, otherCard);
        owner.PlayerCombatState!.Energy = 1;
        card.SpendResources().GetAwaiter().GetResult();
        Require(owner.PlayerCombatState.Energy == 0 && Value(source, "energy_saved") == 0 && Value(source, "energy_cost_adjustment_used") == 0 && Value(foreign, "energy_saved") == 0 && RelicMetrics.Read(source).Gaps.Contains("complex_payments_deferred"), "Clamp or owner separation differs");
        otherCard.SpendResources().GetAwaiter().GetResult();
        Require(Value(foreign, "energy_saved") == 1 && Value(source, "energy_saved") == 0, "Other owner payment contaminated source");
    }
    private static void Removed()
    {
        var (owner, _, run) = Setup(); var source = Obtain<Bookmark>(owner); var card = Card<DemonForm>(run, owner);
        Flush(owner, card);
        int id = RelicMetrics.Acquisition.Get(source);
        RelicMetrics.Archive(source); owner.RemoveRelicInternal(source, silent: true);
        var replacement = Obtain<Bookmark>(owner);
        card.SpendResources().GetAwaiter().GetResult();
        Require(RelicMetrics.ReadRemoved(run).Single(r => r.OwnerId == owner.NetId && r.AcquisitionOrdinal == id).Metrics.Where(m => m.Key == "energy_saved").Sum(m => m.Value) == 1 && Value(replacement, "energy_saved") == 0, "Pending old discount reassigned to replacement acquisition");
    }
    private static void Clone()
    {
        var (owner, _, run) = Setup(); var source = Obtain<Bookmark>(owner); var card = Card<DemonForm>(run, owner);
        Flush(owner, card);
        var copy = Card<DemonForm>(run, owner);
        AccessTools.Field(typeof(CardModel), "_energyCost").SetValue(copy, card.EnergyCost.Clone(copy));
        Preview(copy, source);
        Require(copy.SpendResources().GetAwaiter().GetResult().Item1 == 2 && Value(source, "energy_saved") == 1, "Native cost clone lost original source");
        Require(card.SpendResources().GetAwaiter().GetResult().Item1 == 2 && Value(source, "energy_saved") == 2, "Independent original and clone payments lost/duplicated source");
    }
    private static void GapPersistence()
    {
        var (owner, _, run) = Setup(); var source = Obtain<Bookmark>(owner); var card = Card<DemonForm>(run, owner);
        Flush(owner, card); Scoped(null, () => card.EnergyCost.SetUntilPlayed(1));
        card.SpendResources().GetAwaiter().GetResult();
        Require(RelicMetrics.Read(source).Gaps.Contains("complex_payments_deferred") && RelicMetrics.Read(source).Metrics.Count == 0, "Deferred payment rendered as metric");
        string json = JsonSerializer.Serialize(source.ToSerializable(), JsonSerializationUtility.GetTypeInfo<SerializableRelic>());
        var loaded = RelicModel.FromSerializable(JsonSerializer.Deserialize(json, JsonSerializationUtility.GetTypeInfo<SerializableRelic>())!); loaded.Owner = owner;
        Require(RelicMetrics.Read(loaded).Gaps.Contains("complex_payments_deferred") && RelicMetrics.Read(loaded).Metrics.Count == 0, "Deferred flag missing after native relic JSON");
        RelicMetrics.Archive(source); owner.RemoveRelicInternal(source, silent: true);
        var checkpoint = new SerializableRun(); ExtendedSaveHandlers<IRunState, SerializableRun>.ExtendedData[checkpoint] = new(run);
        string checkpointJson = JsonSerializer.Serialize(checkpoint, JsonSerializationUtility.GetTypeInfo<SerializableRun>());
        var future = Obtain<Anchor>(owner); RelicMetrics.Archive(future);
        ExtendedSaveHandlers<IRunState, SerializableRun>.Load(JsonSerializer.Deserialize(checkpointJson, JsonSerializationUtility.GetTypeInfo<SerializableRun>())!, run);
        var archive = RelicMetrics.ReadRemoved(run);
        Require(archive.Count == 1 && archive[0].Gaps.Contains("complex_payments_deferred") && archive[0].Metrics.Count == 0, "Deferred archive rollback retained future records or lost flag");
        Require(RelicMetrics.Read(Obtain<MembershipCard>(owner)).Gaps.Contains("shop_savings_deferred") && RelicMetrics.Read(Obtain<TheCourier>(owner)).Gaps.Contains("shop_savings_deferred"), "Deferred shop gap absent");
    }
}
