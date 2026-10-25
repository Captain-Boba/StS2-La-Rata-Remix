using System.Text.Json;
using BaseLib.Utils;
using BaseLib.Patches.Saves;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

internal static class RelicWaveChecks
{
    private static readonly Dictionary<Player, TaskCompletionSource> Gates = [];
    private static bool DelayGold(Player player, ref Task __result)
    {
        if (!Gates.TryGetValue(player, out var gate)) return true;
        __result = gate.Task;
        return false;
    }
    private static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }
    private static decimal Value(RelicModel relic, string key) => RelicMetrics.Read(relic).Metrics.Where(m => m.Key == key).Sum(m => m.Value);
    private static void Scoped(RelicModel? relic, Action action)
    {
        var previous = RelicMetricSource.Enter(relic);
        try { action(); } finally { RelicMetricSource.Restore(previous); }
    }
    private static T Obtain<T>(Player owner) where T : RelicModel, new()
    {
        var relic = (T)ModelDb.Relic<T>().ToMutable();
        owner.AddRelicInternal(relic);
        return relic;
    }
    internal static void Run(bool deferredOnly = false, bool paymentOnly = false, bool nightOnly = false, bool cardsOnly = false)
    {
        var harmony = new Harmony("LaRata.ContentChecks.RelicWave");
        NativeConsoleFixture.PatchDesktop(harmony);
        RelicMetrics.Register();
        // Match production startup order; no test-only re-patching probe.
        harmony.PatchAllUncategorized(typeof(ModEntry).Assembly);
        harmony.PatchCategory(typeof(ModEntry).Assembly, ResetEnergyLedger.PatchCategory);
        harmony.PatchCategory(typeof(ModEntry).Assembly, "LaRataRelicEffectContexts");
        Console.WriteLine($"PATCH SET: {harmony.GetPatchedMethods().Count()} methods including fixture desktop substitutions; production category order.");
        var savedFieldsPatch = typeof(SavedSpireField<,>).Assembly.GetType("BaseLib.Patches.Utils.SavedSpireFieldPatch")!;
        harmony.CreateClassProcessor(savedFieldsPatch).Patch();
        harmony.CreateClassProcessor(typeof(ExtendedSaveTypes)).Patch();
        ExtendedSavePatches.Patch(harmony);
        AccessTools.Method(savedFieldsPatch, "AddFieldsSorted").Invoke(null, null);
        try
        {
            if (cardsOnly) { FollowupCardChecks.Run(harmony); return; }
            if (nightOnly) { NightRelicChecks.Run(harmony); return; }
            if (paymentOnly) { PaymentMetricChecks.Run(harmony); return; }
            if (deferredOnly) { DeferredMetricChecks.Run(harmony); return; }
            var a = NativeConsoleFixture.Player(61); var b = NativeConsoleFixture.Player(62);
            NativeConsoleFixture.Set(a, nameof(a.Creature), new Creature(a, 50, 50));
            NativeConsoleFixture.Set(b, nameof(b.Creature), new Creature(b, 50, 50));
            var run = RunState.CreateForTest(players: [a, b], seed: "RELICWAVE");
            var x = Obtain<OldCoin>(a); var y = Obtain<OldCoin>(b);
            Require(RelicMetrics.Read(x).AcquisitionOrdinal == 1 && RelicMetrics.Read(y).AcquisitionOrdinal == 1, "Acquisition IDs are not owner-local");
            var aa = NativeConsoleFixture.Player(61); var bb = NativeConsoleFixture.Player(62);
            _ = RunState.CreateForTest(players: [aa, bb], seed: "RELICWAVE");
            var yy = Obtain<OldCoin>(bb); var xx = Obtain<OldCoin>(aa);
            Require(RelicMetrics.Read(xx).AcquisitionOrdinal == RelicMetrics.Read(x).AcquisitionOrdinal &&
                RelicMetrics.Read(yy).AcquisitionOrdinal == RelicMetrics.Read(y).AcquisitionOrdinal,
                "Cross-player acquisition arrival order changes instance identities");
            var reacquired = Obtain<Anchor>(aa);
            Require(RelicMetrics.Read(reacquired).AcquisitionOrdinal == 2, "Repeated owner acquisition does not advance ordinal");
            // Observe real native setter assignments, including nested Changed events.
            bool nested = false;
            Action callback = () => { if (!nested) { nested = true; Scoped(y, () => b.Gold += 7); } };
            a.GoldChanged += callback;
            Scoped(x, () => a.Gold += 11);
            a.GoldChanged -= callback;
            Require(Value(x, "gold_gained") == 11 && Value(y, "gold_gained") == 7, "Recursive Changed callback contaminates another relic's delta");
            Scoped(x, () => { a.Gold = a.Gold; Scoped(null, () => a.Gold += 100); });
            Require(Value(x, "gold_gained") == 11, "Zero assignment or explicitly masked effect counted");
            Scoped(x, () => b.Gold += 3);
            Require(Value(x, "gold_gained:other") == 3 && Value(y, "gold_gained") == 7, "Target ownership lost or double counted");
            AccessTools.PropertySetter(typeof(Creature), nameof(Creature.CurrentHp)).Invoke(a.Creature, [48]);
            Scoped(x, () => { a.Creature.HealInternal(10); a.Creature.HealInternal(10); a.Creature.HealInternal(0); });
            Require(a.Creature.CurrentHp == 50 && Value(x, "hp_restored") == 2, "Heal cap/zero/full-health was counted as offered healing");
            var scalarCombat = new CombatState(ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable(), run);
            a.Creature.CombatState = scalarCombat;
            scalarCombat.AddCreature(a.Creature);
            a.ResetCombatState();
            var strength = ModelDb.Power<StrengthPower>().ToMutable(); strength.ApplyInternal(a.Creature, 1, true);
            Scoped(x, () =>
            {
                a.Creature.GainBlockInternal(3.9m);
                AccessTools.PropertySetter(typeof(Creature), nameof(Creature.MaxHp)).Invoke(a.Creature, [52]);
                a.PlayerCombatState!.Energy = 2; a.PlayerCombatState.Stars = 3;
                strength.SetAmount(5, true);
            });
            Require(Value(x, "block_gained") == 3 && Value(x, "max_hp_gained") == 2 && Value(x, "energy_gained") == 2 &&
                Value(x, "stars_gained") == 3 && Value(x, "power_applied") == 4, "A native scalar observer records offered values or misses actual mutation");
            var save = x.ToSerializable();
            string json = JsonSerializer.Serialize(save, JsonSerializationUtility.GetTypeInfo<SerializableRelic>());
            Require(json.Contains("LaRataRelicValuesV1") && json.Contains("LaRataRelicAcquisitionV1"), "Native relic JSON lacks registered metric fields");
            Scoped(x, () => a.Gold += 99);
            var restored = RelicModel.FromSerializable(JsonSerializer.Deserialize(json, JsonSerializationUtility.GetTypeInfo<SerializableRelic>())!);
            restored.Owner = a;
            Require(Value(restored, "gold_gained") == 11 && RelicMetrics.Read(restored).AcquisitionOrdinal == 1, "Relic JSON restoration merged future metrics or lost identity");
            RelicMetrics.Archive(x); RelicMetrics.Archive(x);
            Require(RelicMetrics.ReadRemoved(run).Count == 1, "Removal archive duplicated one acquisition");
            // Capture the registered BaseLib payloads, then use the native JSON serializer and BaseLib Load.
            // This deliberately stops short of launching/reading an actual game save.
            var playerPayload = new SerializablePlayer();
            ExtendedSaveHandlers<Player, SerializablePlayer>.ExtendedData[playerPayload] = new(a);
            string playerJson = JsonSerializer.Serialize(playerPayload, JsonSerializationUtility.GetTypeInfo<SerializablePlayer>());
            var runPayload = new SerializableRun();
            ExtendedSaveHandlers<IRunState, SerializableRun>.ExtendedData[runPayload] = new(run);
            string runJson = JsonSerializer.Serialize(runPayload, JsonSerializationUtility.GetTypeInfo<SerializableRun>());
            Require(playerJson.Contains("LaRataRelicSequenceV1") && runJson.Contains("LaRataRemovedRelicValuesV1"), "BaseLib native JSON omits owner sequence or run archive");
            var later = Obtain<Anchor>(a); RelicMetrics.Archive(later);
            Require(RelicMetrics.ReadRemoved(run).Count == 2, "Fixture did not create future archive state");
            ExtendedSaveHandlers<Player, SerializablePlayer>.Load(JsonSerializer.Deserialize(playerJson, JsonSerializationUtility.GetTypeInfo<SerializablePlayer>())!, a);
            ExtendedSaveHandlers<IRunState, SerializableRun>.Load(JsonSerializer.Deserialize(runJson, JsonSerializationUtility.GetTypeInfo<SerializableRun>())!, run);
            Require(RelicMetrics.ReadRemoved(run).Count == 1, "Archive field restoration merged future archive entries");
            a.RemoveRelicInternal(later, silent: true); // The corresponding inventory checkpoint would also replace the future inventory.
            var remover = Obtain<RelicRemover>(a);
            Require(RelicMetrics.Read(remover).AcquisitionOrdinal == 2, "Player sequence rollback retained a future ordinal");
            var belt = Obtain<PotionBelt>(a);
            belt.AfterObtained().GetAwaiter().GetResult();
            int potionSlots = a.MaxPotionCount, gold = a.Gold, hp = a.Creature.MaxHp, deck = a.Deck.Cards.Count;
            var snapshot = a.Relics.ToArray();
            int beltIndex = Array.IndexOf(snapshot, belt), selfIndex = Array.IndexOf(snapshot, remover);
            Require(!RelicRemover.ValidateSelection(a, snapshot, [beltIndex, beltIndex]) &&
                !RelicRemover.ValidateSelection(a, snapshot, [-1]) && !RelicRemover.ValidateSelection(a, snapshot, [snapshot.Length]) &&
                !RelicRemover.ValidateSelection(a, [y], [0]), "Invalid or foreign selection accepted");
            Require(!remover.RemoveConfirmed(snapshot, []).GetAwaiter().GetResult() && !remover.Used, "Empty selection consumes remover");
            var removed = new List<RelicModel>(); a.RelicRemoved += removed.Add;
            Require(remover.RemoveConfirmed(snapshot, [selfIndex, beltIndex]).GetAwaiter().GetResult(), "Valid native removal rejected");
            Require(removed.SequenceEqual(new RelicModel[] { belt, remover }) && remover.Used &&
                !a.Relics.Contains(belt) && !a.Relics.Contains(remover), "Self-removal order or native inventory differs");
            Require(a.MaxPotionCount == potionSlots && a.Gold == gold && a.Creature.MaxHp == hp && a.Deck.Cards.Count == deck,
                "Already granted pickup benefits were rolled back");
            Require(!remover.RemoveConfirmed(snapshot, [beltIndex]).GetAwaiter().GetResult() &&
                !RelicRemover.ValidateSelection(a, snapshot, [beltIndex]), "Used remover or stale removed snapshot accepted");
            var archivedRemover = RelicMetrics.ReadRemoved(run).Single(s => s.OwnerId == a.NetId && s.AcquisitionOrdinal == RelicMetrics.Read(remover).AcquisitionOrdinal);
            Require(archivedRemover.Metrics.Where(m => m.Key == "relics_removed").Sum(m => m.Value) == 2, "Self-removal loses the final committed removal count");
            string beforeFailedRemoval = JsonSerializer.Serialize(RelicMetrics.ReadRemoved(run));
            bool rejected = false; try { a.RemoveRelicInternal(belt); } catch (InvalidOperationException) { rejected = true; }
            Require(rejected && JsonSerializer.Serialize(RelicMetrics.ReadRemoved(run)) == beforeFailedRemoval, "Failed native removal mutated the archive");
            // Delay only a native hook boundary. OldCoin.AfterObtained, GainGold and setter remain production code.
            harmony.Patch(AccessTools.Method(typeof(Hook), nameof(Hook.AfterModifyingGoldGained)), prefix: new(typeof(RelicWaveChecks), nameof(DelayGold)));
            Gates[a] = new(TaskCreationOptions.RunContinuationsAsynchronously); Gates[b] = new(TaskCreationOptions.RunContinuationsAsynchronously);
            decimal beforeX = Value(x, "gold_gained"), beforeY = Value(y, "gold_gained");
            var ax = x.AfterObtained(); var by = y.AfterObtained();
            Require(!ax.IsCompleted && !by.IsCompleted && RelicMetricSource.Relic == null, "Await did not suspend or leaked caller context");
            Gates[b].SetResult(); by.GetAwaiter().GetResult();
            Gates[a].SetResult(); ax.GetAwaiter().GetResult();
            Require(Value(x, "gold_gained") == beforeX + 300 && Value(y, "gold_gained") == beforeY + 300, "Interleaved native relic awaits lose or cross ownership");
            Gates[a] = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var failure = new InvalidOperationException("K6 injected hook failure");
            var failing = x.AfterObtained(); Gates[a].SetException(failure);
            Exception? actual = null; try { failing.GetAwaiter().GetResult(); } catch (Exception error) { actual = error; }
            Require(ReferenceEquals(actual, failure), "Metric finalizer swallowed or replaced the original effect exception");
            Require(RelicMetricSource.Relic == null, "Effect context leaked outside invocation");
            CheckDamageCalculation();
        }
        finally { Gates.Clear(); harmony.UnpatchAll(harmony.Id); }
        Console.WriteLine("PASS: native metric setter deltas, nested owner attribution, zero/capped healing, relic SavedProperties JSON rollback, archive field rollback, native confirmed remover validation/self-last removal and retained Potion Belt capacity. Run-field export/import is not a full run save; UI/reopen/network and complete relic coverage not tested.");
    }

    private static void CheckDamageCalculation()
    {
        var owner = NativeConsoleFixture.Player(101);
        NativeConsoleFixture.Set(owner, nameof(owner.Creature), new Creature(owner, 50, 50));
        var run = RunState.CreateForTest(players: [owner], seed: "METRICDAMAGE");
        var combat = new CombatState(ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable(), run);
        owner.Creature.CombatState = combat; owner.ResetCombatState(); combat.AddCreature(owner.Creature);
        var targets = Enumerable.Range(0, 2).Select(i => new Creature(ModelDb.Monster<TwoTailedRat>().ToMutable(), CombatSide.Enemy, i.ToString()) { CombatState = combat }).ToArray();
        foreach (var target in targets) combat.AddCreature(target);
        var dummy = Obtain<StrikeDummy>(owner);
        var strike = run.CreateCard<StrikeIronclad>(owner); owner.PlayerCombatState!.PlayPile.AddInternal(strike);
        decimal Query() => Hook.ModifyDamage(run, combat, targets[0], owner.Creature, 5, ValueProp.Move, strike, ModifyDamageHookType.All, CardPreviewMode.None, out _);
        Require(Query() == 8 && Query() == 8 && Value(dummy, "damage_value_adjustment") == 0, "Tooltip/out-of-command damage queries increment relic contribution");
        targets[0].CurrentHpChanged += (_, _) => { _ = Query(); _ = Query(); };
        var hp = targets.Select(t => t.CurrentHp).ToArray();
        var results = CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), targets, 5, ValueProp.Move | ValueProp.SkipHurtAnim, owner.Creature, strike).GetAwaiter().GetResult().ToArray();
        Require(results.Length == 2 && targets.Select((t, i) => hp[i] - t.CurrentHp).All(delta => delta == 8), "Native two-target damage was not executed");
        Require(Value(dummy, "damage_value_adjustment") == 6, "Modifier contribution misses later target or counts repeated Changed/UI queries");
    }
}
