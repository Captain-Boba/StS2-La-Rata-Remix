using System.Text.Json;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

// Native merchant purchase and native choice reservation are exercised. The
// transport is captured at SendMessage, and both selections use the native
// remote waiter with explicit replay results instead of a desktop/UI scene.
internal static class ShopBoosterDisconnectChecks
{
    private static readonly List<byte[]> Packets = [];
    private static readonly List<int> GoldMessages = [];
    private static bool failTransmission;
    private static int assertions, failures;
    private static void Check(bool condition, string why)
    { assertions++; if (!condition) { failures++; Console.WriteLine("FAIL: " + why); } }
    private static void Capture(RewardObtainedMessage message)
    {
        if (failTransmission) throw new InvalidOperationException("QA transport fault");
        var writer = new PacketWriter(); message.Serialize(writer); writer.ZeroByteRemainder();
        Packets.Add(writer.Buffer.AsSpan(0, writer.BytePosition).ToArray());
    }
    private static void CaptureGold(GoldLostMessage message) => GoldMessages.Add(message.goldLost);
    private static bool NoSeen() => false;
    private static bool DummyText(ref string __result) { __result = "QA booster"; return false; }
    private static bool ClientMode(ref NetGameType __result) { __result = NetGameType.Client; return false; }
    internal static void Prepare(Harmony harmony)
    {
        // Before production async state machines are patched/JIT compiled, so
        // these tiny presentation methods cannot be inlined first.
        harmony.Patch(AccessTools.Method(typeof(SaveManager), nameof(SaveManager.MarkRelicAsSeen)), prefix: new(typeof(ShopBoosterDisconnectChecks), nameof(NoSeen)));
        harmony.Patch(AccessTools.Method(typeof(LocString), nameof(LocString.GetFormattedText), Type.EmptyTypes), prefix: new(typeof(ShopBoosterDisconnectChecks), nameof(DummyText)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(NetSingleplayerGameService), nameof(NetSingleplayerGameService.Type)), prefix: new(typeof(ShopBoosterDisconnectChecks), nameof(ClientMode)));
    }
    private static RewardObtainedMessage Decode(byte[] bytes)
    {
        var reader = new PacketReader(); reader.Reset(bytes);
        RewardObtainedMessage message = default; message.Deserialize(reader); return message;
    }
    private static void Resolve(PlayerChoiceSynchronizer sync, Player owner, uint id, List<int> indexes)
        => sync.ReceiveReplayChoice(owner, id, PlayerChoiceResult.FromIndexes(indexes).ToNetData());

    private static void Case(int count, bool fresh, bool chooseNone)
    {
        Packets.Clear(); GoldMessages.Clear();
        var a = DisconnectRegressionChecks.NewRun(count); var b = DisconnectRegressionChecks.NewRun(count);
        a.PushRoom(new MerchantRoom()); b.PushRoom(new MerchantRoom());
        var owner = a.Players[^1]; var remoteOwner = b.Players[^1];
        var service = new NetSingleplayerGameService();
        using var aChoices = new PlayerChoiceSynchronizer(service, a);
        using var bChoices = new PlayerChoiceSynchronizer(new NetSingleplayerGameService(), b);
        using var reward = new RewardSynchronizer(new RunLocationTargetedMessageBuffer(service), service, a, owner.NetId);
        var manager = RunManager.Instance;
        NativeConsoleFixture.Set(manager, "State", a);
        NativeConsoleFixture.Set(manager, nameof(manager.NetService), service);
        NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), aChoices);
        NativeConsoleFixture.Set(manager, nameof(manager.RewardSynchronizer), reward);
        var booster = (IroncladBooster)ModelDb.Relic<IroncladBooster>().ToMutable();
        if (!fresh)
            booster.Contents = new CardModel[] { ModelDb.Card<StrikeIronclad>(), ModelDb.Card<DefendIronclad>(), ModelDb.Card<Bash>(), ModelDb.Card<Anger>(), ModelDb.Card<Thunderclap>() }
                .Select(c => a.CreateCard(c, owner).ToSerializable()).ToList();
        var entry = new MerchantRelicEntry(booster, owner);
        int price = entry.Cost, completed = 0;
        entry.PurchaseCompleted += (_, _) => completed++;
        var purchase = entry.OnTryPurchaseWrapper(null);
        string label = $"shop/{count} players/{(fresh ? "fresh" : "saved-offer")}/{(chooseNone ? "none" : "keep2")}";
        Check(!purchase.IsCompleted && completed == 0 && entry.IsStocked, label + ": purchase completed before choice resolution");
        Check(owner.Gold == 10000 - price, label + ": native payment not applied exactly once");
        Check(Packets.Count == 1, label + ": pending choice must follow one native reward transmission");
        Task<RelicModel>? remoteObtain = null;
        BoosterRelic? remote = null;
        // Old binary has not sent yet: resolve its local choice, then inspect
        // the actual late packet. This records the original failure, not a hang.
        if (Packets.Count == 0)
        {
            Resolve(aChoices, owner, 0, chooseNone ? [] : [0, 1]);
            purchase.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        }
        if (Packets.Count == 1)
        {
            var received = Decode(Packets[0]); remote = (BoosterRelic)received.relicModel!;
            Check(!remote.Opened, label + ": packet contains Opened=true before remote choice");
            NativeConsoleFixture.Set(manager, "State", b);
            NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), bChoices);
            remoteObtain = RelicCmd.Obtain(remote, remoteOwner);
            Check(aChoices.ChoiceIds.SequenceEqual(bChoices.ChoiceIds), label + ": native choice reservations disagree before completion");
            if (!remoteObtain.IsCompleted) Resolve(bChoices, remoteOwner, 0, chooseNone ? [] : [0, 1]);
            remoteObtain.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        }
        NativeConsoleFixture.Set(manager, "State", a);
        NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), aChoices);
        if (!purchase.IsCompleted) Resolve(aChoices, owner, 0, chooseNone ? [] : [0, 1]);
        Check(purchase.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult(), label + ": native purchase failed");
        Check(completed == 1 && !entry.IsStocked && Packets.Count == 1, label + ": duplicate transmission or incorrect purchase completion/stock");
        Check(booster.Opened && remote?.Opened == true, label + ": peers did not finish opening");
        Check(owner.Deck.Cards.Select(c => c.Id).SequenceEqual(remoteOwner.Deck.Cards.Select(c => c.Id)), label + ": kept decks differ");
        Check(owner.Deck.Cards.Count == (chooseNone ? 0 : 2), label + ": selected cards not committed exactly once");
        // Gold is delivered separately by the native shop. Replay that scalar
        // loss here; the test does not claim a real transport connection.
        Check(GoldMessages.SequenceEqual(new[] { price }), label + ": native gold message missing, duplicated or wrong");
        PlayerCmd.LoseGold(GoldMessages.Single(), remoteOwner).GetAwaiter().GetResult();
        var left = DisconnectRegressionChecks.Snapshot(a, aChoices);
        var right = DisconnectRegressionChecks.Snapshot(b, bChoices);
        Console.WriteLine($"SHOP PATH {label}: choices={string.Join(',', aChoices.ChoiceIds)}/{string.Join(',', bChoices.ChoiceIds)} checksums={left.checksum}/{right.checksum}");
        Check(left == right, label + ": final native state/checksum differs");
        var oldChoice = aChoices.ChoiceIds.ToArray();
        var loaded = (BoosterRelic)RelicModel.FromSerializable(JsonSerializer.Deserialize(
            JsonSerializer.Serialize(booster.ToSerializable(), JsonSerializationUtility.GetTypeInfo<SerializableRelic>()),
            JsonSerializationUtility.GetTypeInfo<SerializableRelic>())!);
        owner.AddRelicInternal(loaded, silent: true);
        loaded.AfterObtained().WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        Check(aChoices.ChoiceIds.SequenceEqual(oldChoice) && loaded.Opened, label + ": opened save reopened or reserved another choice");
        owner.RemoveRelicInternal(loaded, silent: true);
        if (count == 2 && fresh && !chooseNone && failures == 0)
        {
            Packets.Clear(); GoldMessages.Clear();
            var second = (IroncladBooster)ModelDb.Relic<IroncladBooster>().ToMutable();
            var repeatEntry = new MerchantRelicEntry(second, owner);
            var repeatPurchase = repeatEntry.OnTryPurchaseWrapper(null);
            Check(!repeatPurchase.IsCompleted && Packets.Count == 1, "repeat purchase must send once and await second choice");
            var repeatRemote = (BoosterRelic)Decode(Packets.Single()).relicModel!;
            NativeConsoleFixture.Set(manager, "State", b);
            NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), bChoices);
            var repeatObtain = RelicCmd.Obtain(repeatRemote, remoteOwner);
            Resolve(bChoices, remoteOwner, 1, [0, 1]); repeatObtain.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            NativeConsoleFixture.Set(manager, "State", a);
            NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), aChoices);
            Resolve(aChoices, owner, 1, [0, 1]); repeatPurchase.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            PlayerCmd.LoseGold(GoldMessages.Single(), remoteOwner).GetAwaiter().GetResult();
            Check(aChoices.ChoiceIds.SequenceEqual(new uint[] { 0, 2 }) && aChoices.ChoiceIds.SequenceEqual(bChoices.ChoiceIds), "repeat purchase duplicated/skipped choice reservation");
            Check(owner.Deck.Cards.Count == 4 && remoteOwner.Deck.Cards.Count == 4 && RelicMetrics.Acquisition.Get(second) == 2 && RelicMetrics.Acquisition.Get(repeatRemote) == 2,
                "repeat purchase lost selected cards or acquisition sequence");
            foreach (var player in new[] { owner, remoteOwner }) player.AddRelicInternal(ModelDb.Relic<MusicBox>().ToMutable());
            Check(owner.Relics.Select(RelicMetrics.Acquisition.Get).SequenceEqual(new[] { 1, 2, 3 }) &&
                  remoteOwner.Relics.Select(RelicMetrics.Acquisition.Get).SequenceEqual(new[] { 1, 2, 3 }), "shop boosters -> Ancient diverged in acquisition sequence");
            Check(DisconnectRegressionChecks.Snapshot(a, aChoices) == DisconnectRegressionChecks.Snapshot(b, bChoices), "repeat shop/Ancient final native checksums differ");
        }
    }

    private static void Controls()
    {
        var run = DisconnectRegressionChecks.NewRun(1); run.PushRoom(new MerchantRoom());
        var owner = run.Players[0]; var manager = RunManager.Instance; var service = new NetSingleplayerGameService();
        using var choices = new PlayerChoiceSynchronizer(service, run);
        using var rewards = new RewardSynchronizer(new RunLocationTargetedMessageBuffer(service), service, run, owner.NetId);
        NativeConsoleFixture.Set(manager, "State", run); NativeConsoleFixture.Set(manager, nameof(manager.NetService), service);
        NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), choices); NativeConsoleFixture.Set(manager, nameof(manager.RewardSynchronizer), rewards);
        Packets.Clear(); GoldMessages.Clear();
        var normal = new MerchantRelicEntry(ModelDb.Relic<Anchor>().ToMutable(), owner);
        int cost = normal.Cost;
        Check(normal.OnTryPurchaseWrapper(null).GetAwaiter().GetResult() && !normal.IsStocked && Packets.Count == 1 && choices.ChoiceIds.Count == 0 && owner.Gold == 10000 - cost,
            "non-booster native purchase changed");
        Packets.Clear(); GoldMessages.Clear(); owner.Gold = 0;
        var deniedBooster = (IroncladBooster)ModelDb.Relic<IroncladBooster>().ToMutable();
        var denied = new MerchantRelicEntry(deniedBooster, owner);
        Check(!denied.OnTryPurchaseWrapper(null).GetAwaiter().GetResult() && Packets.Count == 0 && denied.IsStocked && choices.ChoiceIds.Count == 0,
            "insufficient gold started booster acquisition/choice/transmission");
        owner.Gold = 10000;
        var failedBooster = (IroncladBooster)ModelDb.Relic<IroncladBooster>().ToMutable();
        var failed = new MerchantRelicEntry(failedBooster, owner); int completions = 0;
        failed.PurchaseCompleted += (_, _) => completions++;
        failTransmission = true;
        try
        {
            failed.OnTryPurchaseWrapper(null).WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            Check(false, "native send fault was swallowed");
        }
        catch (InvalidOperationException ex) when (ex.Message == "QA transport fault") { Check(true, "native fault preserved"); }
        finally { failTransmission = false; }
        Check(completions == 0 && choices.ChoiceIds.Count == 0, "failed purchase completed or opened early");
        // Successful subsequent opening demonstrates that the failed wrapper
        // released its private defer guard, without depending on that field API.
        var recovery = failedBooster.AfterObtained();
        Check(!recovery.IsCompleted && choices.ChoiceIds.SequenceEqual(new uint[] { 1 }), "failed purchase left booster permanently deferred");
        Resolve(choices, owner, 0, []); recovery.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        Check(failedBooster.Opened, "opening after failed purchase could not finish");
    }

    private static void ConcurrentOwners()
    {
        Packets.Clear(); GoldMessages.Clear();
        var a = DisconnectRegressionChecks.NewRun(2); var b = DisconnectRegressionChecks.NewRun(2);
        a.PushRoom(new MerchantRoom()); b.PushRoom(new MerchantRoom());
        var manager = RunManager.Instance; var service = new NetSingleplayerGameService();
        using var ac = new PlayerChoiceSynchronizer(service, a);
        using var bc = new PlayerChoiceSynchronizer(new NetSingleplayerGameService(), b);
        using var rewards = new RewardSynchronizer(new RunLocationTargetedMessageBuffer(service), service, a, a.Players[0].NetId);
        NativeConsoleFixture.Set(manager, "State", a); NativeConsoleFixture.Set(manager, nameof(manager.NetService), service);
        NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), ac); NativeConsoleFixture.Set(manager, nameof(manager.RewardSynchronizer), rewards);
        var buys = a.Players.Select(p => new MerchantRelicEntry(ModelDb.Relic<IroncladBooster>().ToMutable(), p).OnTryPurchaseWrapper(null)).ToArray();
        Check(Packets.Count == 2 && buys.All(t => !t.IsCompleted) && ac.ChoiceIds.SequenceEqual(new uint[] { 1, 1 }),
            "overlapping purchases did not reserve one pending choice for each owner");
        NativeConsoleFixture.Set(manager, "State", b); NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), bc);
        var receives = Packets.Select((packet, i) => RelicCmd.Obtain(Decode(packet).relicModel!, b.Players[i])).ToArray();
        Check(receives.All(t => !t.IsCompleted) && bc.ChoiceIds.SequenceEqual(ac.ChoiceIds), "overlapping remote reservations disagree");
        // Deliberately opposite completion order in the two modeled peers.
        foreach (int i in new[] { 1, 0 })
        { Resolve(bc, b.Players[i], 0, [i]); receives[i].WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult(); }
        NativeConsoleFixture.Set(manager, "State", a); NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), ac);
        foreach (int i in new[] { 0, 1 })
        { Resolve(ac, a.Players[i], 0, [i]); buys[i].WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult(); }
        for (int i = 0; i < 2; i++)
        {
            PlayerCmd.LoseGold(GoldMessages[i], b.Players[i]).GetAwaiter().GetResult();
            Check(a.Players[i].Deck.Cards.Count == 1 && a.Players[i].Deck.Cards.Select(c => c.Id).SequenceEqual(b.Players[i].Deck.Cards.Select(c => c.Id)),
                "opposite owner completion order routed selected cards to wrong player");
        }
        Check(Packets.Count == 2 && DisconnectRegressionChecks.Snapshot(a, ac) == DisconnectRegressionChecks.Snapshot(b, bc),
            "overlapping owner purchases changed message count or final native state");
    }

    internal static void Run(Harmony harmony)
    {
        assertions = failures = 0;
        var manager = RunManager.Instance;
        var state = manager.DebugOnlyGetState(); var service = manager.NetService;
        var choices = manager.PlayerChoiceSynchronizer; var rewards = manager.RewardSynchronizer;
        var local = LocalContext.NetId;
        // Force the native remote waiter on each model; never use Selector,
        // which bypasses the reservation and would conceal this regression.
        LocalContext.NetId = 999999;
        var send = typeof(NetSingleplayerGameService).GetMethods().Single(m => m.Name == "SendMessage" && m.GetParameters().Length == 1).MakeGenericMethod(typeof(RewardObtainedMessage));
        harmony.Patch(send, prefix: new(typeof(ShopBoosterDisconnectChecks), nameof(Capture)));
        var goldSend = typeof(NetSingleplayerGameService).GetMethods().Single(m => m.Name == "SendMessage" && m.GetParameters().Length == 1).MakeGenericMethod(typeof(GoldLostMessage));
        harmony.Patch(goldSend, prefix: new(typeof(ShopBoosterDisconnectChecks), nameof(CaptureGold)));
        try
        {
            foreach (int count in new[] { 1, 2, 3, 4 })
                foreach (bool fresh in new[] { false, true })
                    foreach (bool none in new[] { false, true }) Case(count, fresh, none);
            if (failures == 0) { Controls(); ConcurrentOwners(); } // Baseline already fails the primary transfer; avoid waiting on its deliberately broken ordering.
            Console.WriteLine($"Shop booster regression: {assertions} assertions; {failures} failures. Native shop/packets/choice waiters/checksum; no UI or network session.");
            if (failures != 0) throw new Exception($"Shop booster regression failed: {failures}/{assertions}");
        }
        finally
        {
            LocalContext.NetId = local;
            NativeConsoleFixture.Set(manager, "State", state);
            NativeConsoleFixture.Set(manager, nameof(manager.NetService), service);
            NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), choices);
            NativeConsoleFixture.Set(manager, nameof(manager.RewardSynchronizer), rewards);
        }
    }
}
