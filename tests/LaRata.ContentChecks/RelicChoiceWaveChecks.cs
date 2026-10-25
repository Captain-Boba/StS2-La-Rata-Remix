using System.Text.Json;
using BaseLib.Abstracts;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

internal static class RelicChoiceWaveChecks
{
    private static readonly Dictionary<Player, TaskCompletionSource<IReadOnlyList<int>?>> Choices = [];
    private static int opened;
    private static bool Choose(Player __0, ref Task<IReadOnlyList<int>?> __result)
    { opened++; __result = Choices[__0].Task; return false; }
    private static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }
    internal static void Run()
    {
        var harmony = new Harmony("LaRata.ContentChecks.RelicChoiceWave");
        NativeConsoleFixture.PatchDesktop(harmony);
        harmony.Patch(AccessTools.Method(typeof(RelicRemovalView), "Choose"), prefix: new(typeof(RelicChoiceWaveChecks), nameof(Choose)));
        var manager = RunManager.Instance;
        var priorState = manager.DebugOnlyGetState(); var priorService = manager.NetService; var priorSync = manager.PlayerChoiceSynchronizer;
        ulong? priorLocal = LocalContext.NetId;
        try
        {
            var a = NativeConsoleFixture.Player(81); var b = NativeConsoleFixture.Player(82);
            var run = RunState.CreateForTest(players: [a, b], seed: "REMOVERCHOICE");
            var service = new NetSingleplayerGameService();
            using var sync = new PlayerChoiceSynchronizer(service, run);
            NativeConsoleFixture.Set(manager, "State", run);
            NativeConsoleFixture.Set(manager, nameof(manager.NetService), service);
            NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), sync);
            var x = NativeConsoleFixture.Relic<RelicRemover>(a); var y = NativeConsoleFixture.Relic<RelicRemover>(b);
            RelicMetrics.Obtained(x); RelicMetrics.Obtained(y);
            Choices[a] = new(); Choices[b] = new(); opened = 0;
            LocalContext.NetId = a.NetId; var first = x.AfterObtained();
            LocalContext.NetId = b.NetId; var second = y.AfterObtained();
            x.AfterObtained().GetAwaiter().GetResult(); y.AfterObtained().GetAwaiter().GetResult();
            Require(opened == 2 && sync.ChoiceIds.SequenceEqual(new uint[] { 1, 1 }), "Duplicate/reentrant pickup reserved extra choices");
            Choices[b].SetResult([]); second.GetAwaiter().GetResult();
            Choices[a].SetResult(null); first.GetAwaiter().GetResult();
            Require(!x.Used && !y.Used && a.Relics.Contains(x) && b.Relics.Contains(y), "Cancel/empty cross-owner choices consumed inventory");
            LocalContext.NetId = a.NetId;
            Require(RelicRemover.CanReopen(x) && !RelicRemover.CanReopen(y), "Reopen availability ignores local owner");
            RelicRemoverReopenMessage Request() => new()
            {
                OwnerId = a.NetId, Acquisition = RelicMetrics.Acquisition.Get(x),
                Sequence = x.SelectionSequence + 1, Location = run.RunLocation
            };
            // Actual BaseLib wrapper activation/serialization; discovery is supplied explicitly in this console process.
            CustomTargetedMessageWrapper.TargetedMessages.Add(typeof(RelicRemoverReopenMessage));
            CustomTargetedMessageWrapper.Initialize();
            var wrapped = new CustomTargetedMessageWrapper { Message = Request() };
            var writer = new PacketWriter(); wrapped.Serialize(writer);
            var reader = new PacketReader(); reader.Reset(writer.Buffer);
            var decoded = new CustomTargetedMessageWrapper { Message = null! }; decoded.Deserialize(reader);
            Require(decoded.Message is RelicRemoverReopenMessage loadedMessage && loadedMessage.OwnerId == a.NetId &&
                loadedMessage.Acquisition == RelicMetrics.Acquisition.Get(x) && loadedMessage.Sequence == x.SelectionSequence + 1 &&
                decoded.Location == run.RunLocation && decoded.ShouldBroadcast && reader.BitPosition == writer.BitPosition,
                "Native/BaseLib message activation or payload roundtrip differs");
            int oldSequence = x.SelectionSequence, oldOpened = opened;
            var wrongOwner = Request(); wrongOwner.OwnerId = b.NetId;
            var stale = Request(); stale.Sequence--;
            var wrongId = Request(); wrongId.Acquisition = 99999;
            var wrongRoom = Request(); wrongRoom.Location = new RunLocation(99, null, 0);
            foreach (var request in new[] { wrongOwner, stale, wrongId, wrongRoom }) RelicRemover.ReceiveReopen(request, a.NetId).GetAwaiter().GetResult();
            Require(x.SelectionSequence == oldSequence && opened == oldOpened, "Rejected sender/sequence/instance/location still opened selection");
            for (int i = 0; i < 8; i++)
            {
                Choices[a] = new();
                var pending = RelicRemover.ReceiveReopen(Request(), a.NetId);
                Require(!pending.IsCompleted && !RelicRemover.CanReopen(x), "Pending choice did not reserve reopening guard");
                Choices[a].SetResult(null); pending.GetAwaiter().GetResult();
                Require(!x.Used && RelicRemover.CanReopen(x), "Cancellation consumes or strands remover");
            }
            var props = SavedProperties.From(x)!;
            var round = JsonSerializer.Deserialize(JsonSerializer.Serialize(props, JsonSerializationUtility.GetTypeInfo<SavedProperties>()), JsonSerializationUtility.GetTypeInfo<SavedProperties>())!;
            var loaded = (RelicRemover)ModelDb.Relic<RelicRemover>().ToMutable(); round.Fill(loaded);
            Require(!loaded.Used && loaded.SelectionSequence == 9, "Cancel sequence/unused state lost in native SavedProperty JSON");
        }
        finally
        {
            NativeConsoleFixture.Set(manager, "State", priorState);
            NativeConsoleFixture.Set(manager, nameof(manager.NetService), priorService);
            NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), priorSync);
            LocalContext.NetId = priorLocal; Choices.Clear(); harmony.UnpatchAll(harmony.Id);
        }
        Console.WriteLine("PASS: native remover opening/choice IDs under two pending owner choices, duplicate-start guard, Cancel/empty preservation, eight reopens, sender/sequence/instance/location rejection and native sequence JSON. Selection UI predetermined; singleplayer no-op transport, no real multiplayer/reconnect/full save.");
    }
}
