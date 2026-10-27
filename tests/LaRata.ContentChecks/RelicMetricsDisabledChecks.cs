using System.Reflection;
using System.Text.Json;
using BaseLib.Patches.Saves;
using BaseLib.Utils;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

internal static class RelicMetricsDisabledChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }

    internal static void CheckPatchSet(Harmony harmony)
    {
        Check(!RelicMetrics.Enabled, "Relic statistics must be disabled for all peers");
        var disabled = typeof(ModEntry).Assembly.GetTypes().Where(t =>
            t.GetCustomAttributes<HarmonyPatchCategory>().Any(a => a.info.category == RelicMetrics.PatchCategory)).ToHashSet();
        Check(disabled.Contains(typeof(RelicNumericContribution)) && disabled.Contains(typeof(RelicScalarObservation)),
            "Numeric/scalar observers missing from disabled category");
        var patches = harmony.GetPatchedMethods().SelectMany(m =>
        {
            var p = Harmony.GetPatchInfo(m)!;
            return p.Prefixes.Concat(p.Postfixes).Concat(p.Transpilers).Concat(p.Finalizers);
        }).Where(p => p.owner == harmony.Id).ToArray();
        foreach (var type in disabled)
            Check(!patches.Any(p => p.PatchMethod.DeclaringType == type), $"Statistics patch remains active: {type}");
        Check(patches.Any(p => p.PatchMethod.DeclaringType?.Name == "ReopenRemoval"), "Relic Remover reopen hook disabled");
        Check(patches.Any(p => p.PatchMethod.DeclaringType == typeof(RelicMetricsAcquired)), "Gameplay instance identity hook disabled");
        Console.WriteLine($"PASS: {disabled.Count} statistics patch classes inactive; remover and identity hooks remain active.");
    }

    private static T Add<T>(Player owner) where T : RelicModel, new()
    {
        var relic = (T)ModelDb.Relic<T>().ToMutable();
        owner.AddRelicInternal(relic);
        return relic;
    }

    private static readonly SavedSpireField<RelicModel, string> Values =
        (SavedSpireField<RelicModel, string>)AccessTools.Field(typeof(RelicMetrics), "Values").GetValue(null)!;
    private static string Serialize(RelicModel relic) => JsonSerializer.Serialize(relic.ToSerializable(),
        JsonSerializationUtility.GetTypeInfo<SerializableRelic>());

    internal static void Run()
    {
        checks = 0;
        var harmony = new Harmony("LaRata.ContentChecks.MetricsDisabled");
        NativeConsoleFixture.PatchDesktop(harmony);
        RelicMetrics.Register();
        ModEntry.ApplyPatches(harmony);
        var saved = typeof(SavedSpireField<,>).Assembly.GetType("BaseLib.Patches.Utils.SavedSpireFieldPatch")!;
        harmony.CreateClassProcessor(saved).Patch();
        harmony.CreateClassProcessor(typeof(ExtendedSaveTypes)).Patch();
        ExtendedSavePatches.Patch(harmony);
        AccessTools.Method(saved, "AddFieldsSorted").Invoke(null, null);
        bool wasStarting = CombatManager.Instance.IsStarting;
        NativeConsoleFixture.Set(CombatManager.Instance, nameof(CombatManager.IsStarting), true);
        try
        {
            CheckPatchSet(harmony);
            var run = DisconnectRegressionChecks.NewRun(2);
            var owner = run.Players[1];
            var skull = Add<SneckoSkull>(owner);
            Check(Values.Get(skull) == "", "Fresh acquisition initialized a statistics ledger");
            const string legacy = "{\"Version\":1,\"Coverage\":\"Complete\",\"Metrics\":[{\"Key\":\"power_value_adjustment\",\"Value\":50,\"Unit\":\"PowerValue\",\"Attribution\":\"Contribution\"}],\"Deferred\":null}";
            Values.Set(skull, legacy); // Seed a pre-disable saved ledger; never a production write.
            string before = Serialize(skull);
            var target = new RelicMetricSource.Target(owner, RelicMetrics.Identity(skull), skull.Id.ToString(), skull);
            RelicMetrics.Record(skull, "power_value_adjustment", 3, "PowerValue", "Contribution");
            RelicMetrics.RecordCommittedRemoval(skull);
            RelicMetrics.RecordTarget(target, "cards_drawn", 2, "Cards");
            RelicMetrics.MarkDeferred(target, "test");
            RelicMetrics.Archive(skull);
            RelicMetrics.Obtained(skull);
            Check(Serialize(skull) == before && RelicMetrics.ReadRemoved(run).Count == 0,
                "A direct recording/archive/acquisition path changed legacy statistics");
            Check(!RelicStatsPresentation.ResultLines(skull).Any(), "Legacy statistics still displayed");
            var restored = RelicModel.FromSerializable(JsonSerializer.Deserialize(before,
                JsonSerializationUtility.GetTypeInfo<SerializableRelic>())!);
            restored.Owner = owner;
            Check(Values.Get(restored) == legacy && RelicMetrics.Identity(restored) == RelicMetrics.Identity(skull),
                "Native relic JSON roundtrip lost legacy payload or gameplay identity");

            // Genuine native power application, with UI-like preview callbacks
            // nested inside both first application and subsequent stacking.
            var combat = new CombatState(ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable(), run);
            foreach (var player in run.Players)
            { player.Creature.CombatState = combat; player.ResetCombatState(); combat.AddCreature(player.Creature); }
            var enemy = new Creature(ModelDb.Monster<TwoTailedRat>().ToMutable(), CombatSide.Enemy, "target") { CombatState = combat };
            combat.AddCreature(enemy);
            var card = run.CreateCard<DeadlyPoison>(owner);
            owner.PlayerCombatState!.Hand.AddInternal(card);
            var preview = new PowerVar<PoisonPower>(5);
            int previews = 0;
            void Preview()
            {
                for (int i = 0; i < 9; i++)
                {
                    preview.UpdateCardPreview(card, CardPreviewMode.Normal, enemy, true);
                    Check(preview.PreviewValue == 6, "Snecko Skull's actual poison preview bonus changed");
                    previews++;
                }
            }
            enemy.PowerApplied += _ => Preview();
            enemy.PowerIncreased += (_, _, _) => Preview();
            Preview();
            PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(), enemy, 5, owner.Creature, card, true).GetAwaiter().GetResult();
            Check(enemy.GetPowerAmount<PoisonPower>() == 6, "Snecko Skull's first application lost its +1 Poison");
            PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(), enemy, 5, owner.Creature, card, true).GetAwaiter().GetResult();
            Check(enemy.GetPowerAmount<PoisonPower>() == 12 && previews >= 27, "Stacking or nested preview callbacks did not execute");
            Check(Serialize(skull) == before, "Actual poison or repeated/nested previews changed statistics");

            // Scalar pickup effects remain native; their statistics are inert.
            var coin = Add<OldCoin>(owner);
            int gold = owner.Gold;
            coin.AfterObtained().GetAwaiter().GetResult();
            Check(owner.Gold == gold + 300 && Values.Get(coin) == "", "Old Coin effect changed or generated statistics");
            var remover = Add<RelicRemover>(owner);
            Check(RelicMetrics.Identity(remover) > RelicMetrics.Identity(coin), "Remover lacks a unique persisted identity");
            var snapshot = owner.Relics.ToArray();
            Check(!remover.RemoveConfirmed(snapshot, []).GetAwaiter().GetResult() && !remover.Used,
                "Empty removal consumes remover");
            Check(remover.RemoveConfirmed(snapshot, [Array.IndexOf(snapshot, coin)]).GetAwaiter().GetResult()
                && remover.Used && !owner.Relics.Contains(coin) && owner.Gold == gold + 300,
                "Confirmed removal or retained pickup benefit broken with statistics disabled");
            Check(RelicMetrics.ReadRemoved(run).Count == 0, "Relic removal generated archived statistics");
            RelicChoiceWaveChecks.Run();
            Console.WriteLine($"PASS: {checks} focused checks; native poison, {previews} previews including nested callbacks, inert old/new statistics, relic JSON roundtrip and remover. Console models only; no game process or live multiplayer test.");
        }
        finally
        {
            NativeConsoleFixture.Set(CombatManager.Instance, nameof(CombatManager.IsStarting), wasStarting);
            harmony.UnpatchAll(harmony.Id);
        }
    }
}
