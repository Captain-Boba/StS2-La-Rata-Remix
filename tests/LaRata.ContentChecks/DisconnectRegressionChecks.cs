using System.Text.Json;
using System.Reflection;
using BaseLib.Patches.Saves;
using BaseLib.Utils;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

// Two independent in-memory native run states, not a running network session.
// The native state builder, packet serializer and checksum algorithm remain intact.
internal static class DisconnectRegressionChecks
{
    private static readonly List<string> Failures = [];
    private static int assertions;
    // Statistics are disabled in 0.9.5. Seed historical save data directly so
    // identity/load regression coverage does not depend on active recording.
    private static void SeedLegacyMetric(RelicModel relic, string key, decimal value)
    {
        var values = (SavedSpireField<RelicModel, string>)AccessTools.Field(typeof(RelicMetrics), "Values").GetValue(null)!;
        values.Set(relic, JsonSerializer.Serialize(new
        {
            Version = 1, Coverage = "Complete",
            Metrics = new[] { new RelicRunMetric(key, value, "Count", "Contribution") }
        }));
    }
    private static void Check(bool value, string reason)
    {
        assertions++;
        if (!value) { Failures.Add(reason); Console.WriteLine("FAIL: " + reason); }
    }

    internal static RunState NewRun(int count)
    {
        var players = Enumerable.Range(0, count).Select(i =>
        {
            var p = NativeConsoleFixture.Player((ulong)(101 + i));
            NativeConsoleFixture.Set(p, nameof(p.Creature), new Creature(p, 50, 50));
            NativeConsoleFixture.Set(p, nameof(p.PlayerRng), new PlayerRngSet((uint)(101 + i)));
            NativeConsoleFixture.Set(p, nameof(p.PlayerOdds), new PlayerOddsSet(p.PlayerRng));
            NativeConsoleFixture.Set(p, nameof(p.RelicGrabBag), new RelicGrabBag());
            p.DiscoveredCards = []; p.DiscoveredRelics = []; p.DiscoveredEnemies = [];
            p.DiscoveredPotions = []; p.DiscoveredEpochs = [];
            return p;
        }).ToArray();
        return RunState.CreateForTest(players: players, seed: "DISCONNECT20261026");
    }

    private static T Add<T>(Player owner) where T : RelicModel, new()
    {
        var relic = (T)ModelDb.Relic<T>().ToMutable();
        owner.AddRelicInternal(relic);
        return relic;
    }

    internal static (uint checksum, string dump) Snapshot(RunState run, PlayerChoiceSynchronizer? actualChoices = null)
    {
        var manager = RunManager.Instance;
        var previousChoice = manager.PlayerChoiceSynchronizer;
        var previousRewards = manager.RewardsSetSynchronizer;
        var service = new NetSingleplayerGameService();
        var buffer = new RunLocationTargetedMessageBuffer(service);
        using var choices = new PlayerChoiceSynchronizer(service, run);
        using var rewards = new RewardsSetSynchronizer(buffer, service, run, run.Players[0].NetId);
        using var tracker = new ChecksumTracker(service, run);
        try
        {
            NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), actualChoices ?? choices);
            NativeConsoleFixture.Set(manager, nameof(manager.RewardsSetSynchronizer), rewards);
            var state = NetFullCombatState.FromRun(run, null);
            return (tracker.GenerateChecksum(state), state.ToString());
        }
        finally
        {
            NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), previousChoice);
            NativeConsoleFixture.Set(manager, nameof(manager.RewardsSetSynchronizer), previousRewards);
        }
    }

    private static void PeerCase<T>(int count, string observation) where T : RelicModel, new()
    {
        var left = NewRun(count); var right = NewRun(count);
        foreach (var run in new[] { left, right })
            foreach (var p in run.Players) Add<Anchor>(p);
        var owner = left.Players[count - 1];
        var peer = right.Players[count - 1];
        // Native EventModel.RelicOption assigns Owner before the option is obtained.
        // Deliberately inspect only one side's uncommitted option, then obtain
        // a separate identical option on both sides. No UI invocation is claimed.
        var preview = (T)ModelDb.Relic<T>().ToMutable();
        preview.Owner = owner;
        for (int i = 0; i < 3; i++)
        {
            if (observation == "serialize") _ = preview.ToSerializable();
            if (observation == "context")
            {
                var prior = RelicMetricSource.Enter(preview);
                RelicMetricSource.Restore(prior);
            }
        }
        var a = Add<T>(owner); var b = Add<T>(peer);
        var first = Snapshot(left); var second = Snapshot(right);
        string label = $"{typeof(T).Name}/{count} players/{observation}";
        Check(first.checksum == second.checksum && first.dump == second.dump,
            $"{label}: native states diverged ({first.checksum}/{second.checksum}); acquired IDs {RelicMetrics.Acquisition.Get(a)}/{RelicMetrics.Acquisition.Get(b)}");
        Check(RelicMetrics.Acquisition.Get(a) == 2 && RelicMetrics.Acquisition.Get(b) == 2,
            label + ": uncommitted option consumed an acquisition ordinal");
        Check(RelicMetrics.Acquisition.Get(preview) == 0, label + ": preview gained a persisted identity");
        var repeat = Snapshot(left);
        Check(repeat == first, label + ": state capture mutated serialized identity");
        // Sensitivity control: real identity and metric differences MUST still
        // change the native checksum. No exclusions or patched checksum methods.
        int original = RelicMetrics.Acquisition.Get(a);
        RelicMetrics.Acquisition.Set(a, original + 100);
        Check(Snapshot(left).checksum != first.checksum, label + ": acquisition is missing from checksum");
        RelicMetrics.Acquisition.Set(a, original);
        SeedLegacyMetric(a, "qa_probe", 7);
        Check(Snapshot(left).checksum != first.checksum, label + ": metrics are missing from checksum");
        string json = JsonSerializer.Serialize(a.ToSerializable(), JsonSerializationUtility.GetTypeInfo<SerializableRelic>());
        var loaded = RelicModel.FromSerializable(JsonSerializer.Deserialize(json, JsonSerializationUtility.GetTypeInfo<SerializableRelic>())!);
        loaded.Owner = owner;
        Check(RelicMetrics.Acquisition.Get(loaded) == original &&
              RelicMetrics.Read(loaded).Metrics.Single(m => m.Key == "qa_probe").Value == 7,
            label + ": native relic JSON lost existing identity or metrics");
    }

    private static void RewardTransferCase<T>(int count) where T : RelicModel, new()
    {
        var left = NewRun(count); var right = NewRun(count);
        foreach (var run in new[] { left, right })
            foreach (var p in run.Players) Add<Anchor>(p);
        var owner = left.Players[count - 1]; var peer = right.Players[count - 1];
        var purchased = Add<MembershipCard>(owner);
        var message = new RewardObtainedMessage
        {
            rewardType = RewardType.Relic, location = left.RunLocation,
            relicModel = purchased, wasSkipped = false
        };
        var writer = new PacketWriter(); message.Serialize(writer);
        writer.ZeroByteRemainder();
        var reader = new PacketReader(); reader.Reset(writer.Buffer);
        RewardObtainedMessage received = default; received.Deserialize(reader);
        peer.AddRelicInternal(received.relicModel!);
        string label = $"reward transfer -> {typeof(T).Name}/{count} players";
        var before = Snapshot(left); var remoteBefore = Snapshot(right);
        Check(before == remoteBefore, label + ": native states already differ immediately after receipt");
        var ancient = Add<T>(owner); var remoteAncient = Add<T>(peer);
        var after = Snapshot(left); var remoteAfter = Snapshot(right);
        Console.WriteLine($"REWARD PATH {label}: before={before.checksum}/{remoteBefore.checksum}; after={after.checksum}/{remoteAfter.checksum}; Ancient IDs={RelicMetrics.Acquisition.Get(ancient)}/{RelicMetrics.Acquisition.Get(remoteAncient)}");
        Check(after == remoteAfter, label + ": received identity failed to advance remote acquisition sequence");
        Check(RelicMetrics.Acquisition.Get(ancient) == 3 && RelicMetrics.Acquisition.Get(remoteAncient) == 3,
            label + ": expected next identity 3 on both sides");
        Check(peer.Relics.Select(RelicMetrics.Acquisition.Get).Distinct().Count() == peer.Relics.Count,
            label + ": duplicate owned identities after remote receipt");
    }

    private static void SyncCase()
    {
        var run = NewRun(2); var source = run.Players[0];
        var anchor = Add<Anchor>(source);
        SeedLegacyMetric(anchor, "retained", 9);
        var last = Add<MusicBox>(source);
        source.RemoveRelicInternal(last); // sequence 2, only live ID 1 remains
        var save = source.ToSerializable();
        string json = JsonSerializer.Serialize(save, JsonSerializationUtility.GetTypeInfo<SerializablePlayer>());
        Check(json.Contains("LaRataRelicSequenceV1"), "player JSON must contain sequence above live inventory maximum");
        var restoredSave = JsonSerializer.Deserialize(json, JsonSerializationUtility.GetTypeInfo<SerializablePlayer>())!;
        var peerRun = NewRun(2); var peer = peerRun.Players[0];
        // Receiver has advanced past this checkpoint. The payload must replace
        // its old next counter, not keep its unrelated future allocations.
        Add<Anchor>(peer); Add<MusicBox>(peer); Add<PaelsFlesh>(peer); Add<PaelsTooth>(peer);
        peer.SyncWithSerializedPlayer(restoredSave);
        Check(peer.Relics.Count == 1 && RelicMetrics.Acquisition.Get(peer.Relics[0]) == 1 &&
              RelicMetrics.Read(peer.Relics[0]).Metrics.Single(m => m.Key == "retained").Value == 9,
            "native player sync changed existing identity or ledger");
        var next = Add<MusicBox>(peer);
        Check(RelicMetrics.Acquisition.Get(next) == 3, "native player sync did not restore higher-than-live checkpoint sequence 2");
        Check(RelicMetrics.Acquisition.Get(Add<Anchor>(peerRun.Players[1])) == 1, "native sync contaminated another owner's sequence");
        var loaded = Player.FromSerializable(restoredSave);
        _ = RunState.CreateForTest(players: [loaded], seed: "DISCONNECT20261026");
        Check(RelicMetrics.Acquisition.Get(loaded.Relics[0]) == 1 &&
              RelicMetrics.Read(loaded.Relics[0]).Metrics.Single(m => m.Key == "retained").Value == 9,
            "native FromSerializable changed existing ID/metrics");
        Check(RelicMetrics.Acquisition.Get(Add<MusicBox>(loaded)) == 3,
            "native FromSerializable lost removed-highest sequence");
        // A legacy payload can have a stale owner sequence below a live ID.
        RelicMetrics.Acquisition.Set(anchor, 7);
        var stale = source.ToSerializable();
        peer.SyncWithSerializedPlayer(stale);
        Check(RelicMetrics.Acquisition.Get(Add<MusicBox>(peer)) == 8,
            "sync failed to bound stale owner sequence by preserved live ID");
        var legacy = NewRun(2); var legacyOwner = legacy.Players[0];
        var oldA = Add<Anchor>(legacyOwner); var oldB = Add<MusicBox>(legacyOwner);
        RelicMetrics.Acquisition.Set(oldA, 5); RelicMetrics.Acquisition.Set(oldB, 5);
        SeedLegacyMetric(oldA, "legacyA", 2); SeedLegacyMetric(oldB, "legacyB", 3);
        var removed = (SavedSpireField<IRunState, string>)AccessTools.Field(typeof(RelicMetrics), "Removed").GetValue(null)!;
        removed.Set(legacy, JsonSerializer.Serialize(new[]
        {
            RelicMetrics.Read(oldA) with { Removed = true },
            RelicMetrics.Read(oldB) with { Removed = true }
        }));
        string legacyArchive = removed.Get(legacy);
        legacyOwner.RemoveRelicInternal(oldA); legacyOwner.RemoveRelicInternal(oldB);
        var archived = RelicMetrics.ReadRemoved(legacy);
        Check(removed.Get(legacy) == legacyArchive, "disabled statistics changed a legacy archive on removal");
        Check(archived.Count == 2 && archived.Any(r => r.Metrics.Any(m => m.Key == "legacyA" && m.Value == 2)) &&
              archived.Any(r => r.Metrics.Any(m => m.Key == "legacyB" && m.Value == 3)),
            "legacy duplicate ordinal overwrote another relic's archived ledger");
        var previous = RelicMetricSource.EnterDeferred(legacyOwner, 5);
        Check(RelicMetricSource.Attribution == null, "ambiguous legacy ordinal was attributed to an arbitrary relic");
        RelicMetricSource.Restore(previous);
        Check(RelicMetrics.Acquisition.Get(Add<PaelsFlesh>(legacyOwner)) == 6,
            "new identity reused an archived high ordinal");
    }

    private static void PickupReplay()
    {
        var a = NewRun(2); var b = NewRun(2);
        var local = LocalContext.NetId; LocalContext.NetId = a.Players[1].NetId;
        try
        {
            var original = RelicCmd.Obtain(ModelDb.Relic<OldCoin>().ToMutable(), a.Players[0]).GetAwaiter().GetResult();
            var sent = new RewardObtainedMessage { rewardType = RewardType.Relic, relicModel = original, location = a.RunLocation, wasSkipped = false };
            var writer = new PacketWriter(); sent.Serialize(writer);
            var reader = new PacketReader(); reader.Reset(writer.Buffer);
            RewardObtainedMessage received = default; received.Deserialize(reader);
            var remote = RelicCmd.Obtain(received.relicModel!, b.Players[0]).GetAwaiter().GetResult();
            Check(a.Players[0].Gold == 10300 && b.Players[0].Gold == 10300, "native OldCoin pickup did not apply once per peer");
            Check(RelicMetrics.Read(original).Metrics.Count == 0 && RelicMetrics.Read(remote).Metrics.Count == 0,
                "fresh native Obtain recorded statistics despite global disablement");
            Check(Snapshot(a) == Snapshot(b), "native OldCoin reward replay changed final state/checksum");
        }
        finally { LocalContext.NetId = local; }
    }

    internal static void Run(bool shopOnly = false)
    {
        Failures.Clear(); assertions = 0;
        var harmony = new Harmony("LaRata.ContentChecks.DisconnectRegression");
        NativeConsoleFixture.PatchDesktop(harmony);
        if (shopOnly) ShopBoosterDisconnectChecks.Prepare(harmony);
        RelicMetrics.Register();
        ModEntry.ApplyPatches(harmony);
        var saved = typeof(SavedSpireField<,>).Assembly.GetType("BaseLib.Patches.Utils.SavedSpireFieldPatch")!;
        harmony.CreateClassProcessor(saved).Patch();
        harmony.CreateClassProcessor(typeof(ExtendedSaveTypes)).Patch();
        ExtendedSavePatches.Patch(harmony);
        foreach (var type in typeof(ExtendedSavePatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(t => t.Name.Contains("Player", StringComparison.Ordinal)))
        {
            Console.WriteLine("BaseLib player hook: " + type.Name);
            harmony.CreateClassProcessor(type).Patch();
        }
        var postInit = typeof(ExtendedSavePatches).Assembly.GetType("BaseLib.Patches.PostModInitPatch")!;
        var canModify = AccessTools.Property(postInit, "CanModifyGameplay");
        var oldCanModify = canModify.GetValue(null);
        canModify.SetValue(null, true);
        AccessTools.Method(saved, "AddFieldsSorted").Invoke(null, null);
        var fixtureMod = new Mod { path = "console-fixture", manifest = new ModManifest { id = "LaRata" }, state = ModLoadState.Loaded, assembly = typeof(ModEntry).Assembly };
        var mods = (List<Mod>)AccessTools.Field(typeof(ModManager), "_mods").GetValue(null)!;
        mods.Add(fixtureMod);
        ModelIdSerializationCache.Init();
        try
        {
            if (shopOnly) { ShopBoosterDisconnectChecks.Run(harmony); return; }
            SyncCase();
            PickupReplay();
            foreach (int count in new[] { 1, 2, 3, 4 })
            {
                RewardTransferCase<MusicBox>(count);
                RewardTransferCase<PaelsTooth>(count);
                RewardTransferCase<PaelsFlesh>(count);
            }
            foreach (int count in new[] { 1, 2, 3, 4 })
                foreach (string observation in new[] { "none", "serialize", "context" })
                {
                    PeerCase<MusicBox>(count, observation);
                    PeerCase<PaelsTooth>(count, observation);
                    PeerCase<PaelsFlesh>(count, observation);
                }
            Console.WriteLine($"Disconnect regression: {assertions} assertions; {Failures.Count} failures. Native models/serialization/checksum, no live UI/network or disk-save acceptance.");
            if (Failures.Count != 0) throw new Exception($"Disconnect regression failed: {Failures.Count}/{assertions}");
        }
        finally { mods.Remove(fixtureMod); canModify.SetValue(null, oldCanModify); harmony.UnpatchAll(harmony.Id); }
    }
}
