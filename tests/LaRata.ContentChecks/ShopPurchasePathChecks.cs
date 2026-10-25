using System.Text.Json;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

internal static class ShopPurchasePathChecks
{
    private static bool inflation, cancel;
    private static readonly List<int> sent = [];
    private static void Require(bool condition, string reason) { if (!condition) throw new Exception(reason); }
    private static bool Ascension(AscensionLevel level, ref bool __result)
    { __result = inflation && level == AscensionLevel.Inflation; return false; }
    private static bool Choose(Player player, ref Task<IEnumerable<CardModel>> __result)
    { __result = Task.FromResult<IEnumerable<CardModel>>(cancel ? [] : [player.Deck.Cards.First()]); return false; }
    private static void SuppressPreview(ref bool showPreview) => showPreview = false;
    private static void ObserveMessage(MerchantCardRemovalMessage message) => sent.Add(message.goldCost);

    internal static void Run()
    {
        var harmony = new Harmony("LaRata.ShopPurchasePathChecks");
        ulong? previousLocal = LocalContext.NetId;
        var previousSync = RunManager.Instance.OneOffSynchronizer;
        NativeConsoleFixture.PatchDesktop(harmony);
        // Install the ascension context before the native BaseCost delegate is JIT-compiled.
        harmony.Patch(AccessTools.Method(typeof(RunManager), nameof(RunManager.HasAscension)), prefix: new(typeof(ShopPurchasePathChecks), nameof(Ascension)));
        harmony.CreateClassProcessor(typeof(FirstRemovalBasePricePatch)).Patch();
        harmony.CreateClassProcessor(typeof(RemovalAvailablePatch)).Patch();
        harmony.CreateClassProcessor(typeof(RepeatRemovalPricePatch)).Patch();
        harmony.Patch(AccessTools.Method(typeof(CardSelectCmd), nameof(CardSelectCmd.FromDeckForRemoval)), prefix: new(typeof(ShopPurchasePathChecks), nameof(Choose)));
        harmony.Patch(AccessTools.Method(typeof(CardPileCmd), nameof(CardPileCmd.RemoveFromDeck), [typeof(CardModel), typeof(bool)]), prefix: new(typeof(ShopPurchasePathChecks), nameof(SuppressPreview)));
        var send = typeof(NetSingleplayerGameService).GetMethods().Single(m => m.Name == "SendMessage" && m.GetParameters().Length == 1).MakeGenericMethod(typeof(MerchantCardRemovalMessage));
        harmony.Patch(send, prefix: new(typeof(ShopPurchasePathChecks), nameof(ObserveMessage)));
        int purchases = 0;
        try
        {
            CheckWithoutUiPriceReads();
            // Two local-owner contexts in a three-player model. This is not a network party.
            foreach (ulong ownerId in new ulong[] { 1, 2102 })
            foreach (bool high in new[] { false, true })
            foreach (int discount in new[] { 0, 1, 2, 3 })
            foreach (int prior in new[] { 0, 4 })
            {
                inflation = high; cancel = false; sent.Clear();
                LocalContext.NetId = ownerId;
                var players = new ulong[] { 1, 2101, 2102 }.Select(NativeConsoleFixture.Player).ToArray();
                var run = RunState.CreateForTest(players: players, seed: "SHOPPATH");
                run.PushRoom(new MerchantRoom());
                var owner = players.Single(p => p.NetId == ownerId);
                owner.ExtraFields.CardShopRemovalsUsed = prior;
                NativeConsoleFixture.Relic<AThousandCuts>(owner);
                if ((discount & 1) != 0) NativeConsoleFixture.Relic<MembershipCard>(owner);
                if ((discount & 2) != 0) NativeConsoleFixture.Relic<TheCourier>(owner);
                // A different owner has both discounts; their hooks must not change this buyer.
                var other = players.First(p => p != owner);
                NativeConsoleFixture.Relic<MembershipCard>(other);
                NativeConsoleFixture.Relic<TheCourier>(other);
                for (int i = 0; i < 6; i++) owner.Deck.AddInternal(run.CreateCard<StrikeIronclad>(owner));
                var service = new NetSingleplayerGameService();
                using var sync = new OneOffSynchronizer(new RunLocationTargetedMessageBuffer(service), service, run, ownerId);
                NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.OneOffSynchronizer), sync);
                var entry = new MerchantCardRemovalEntry(owner);
                int[][] normal = [[75,75,75,75], [37,37,37,37], [60,60,60,60], [30,30,30,30]];
                int[][] inflated = [[100,100,100,100], [50,50,50,50], [80,80,80,80], [40,40,40,40]];
                var prices = (high ? inflated : normal)[discount];
                for (int purchase = 0; purchase < 3; purchase++)
                {
                    int beforeGold = owner.Gold, beforeDeck = owner.Deck.Cards.Count;
                    var selected = owner.Deck.Cards.First();
                    int displayed = entry.Cost;
                    Require(displayed == prices[purchase], $"Wrong native discount: owner={ownerId} inflation={high} discount={discount} n={purchase} actual={displayed}");
                    // Entire public purchase -> native DoLocal -> message -> native gold/deck/counter.
                    Require(entry.OnTryPurchaseWrapper(null, false, true).GetAwaiter().GetResult(), "Public purchase rejected");
                    Require(sent.Count == purchase + 1 && sent[^1] == displayed, "Sent price differs from displayed price or sent twice");
                    Require(beforeGold - owner.Gold == displayed, "Native gold debit differs from sent price");
                    Require(owner.Deck.Cards.Count == beforeDeck - 1 && selected.HasBeenRemovedFromState, "Native deck removal missed selected card");
                    Require(owner.ExtraFields.CardShopRemovalsUsed == prior + purchase + 1, "Native saved counter did not advance once");
                    Require(entry.Cost == prices[purchase + 1], "ATC price increased before Used/UI callback");
                    entry.SetUsed();
                    Require(!entry.Used, "ATC slot became used");
                    Require(players.Where(p => p != owner).All(p => p.Gold == 10000 && p.ExtraFields.CardShopRemovalsUsed == 0 && p.Deck.Cards.Count == 0), "Purchase modified another owner");
                    purchases++;
                    Console.WriteLine($"ATC owner={ownerId} inflation={high} discount={discount} n={prior + purchase}->{prior + purchase + 1} display/send/debit={displayed} next={entry.Cost}");
                }
                int gold = owner.Gold, count = owner.Deck.Cards.Count, messages = sent.Count;
                cancel = true;
                Require(!entry.OnTryPurchaseWrapper(null, false, true).GetAwaiter().GetResult(), "Cancelled public purchase succeeded");
                Require(owner.Gold == gold && owner.Deck.Cards.Count == count && owner.ExtraFields.CardShopRemovalsUsed == prior + 3 && entry.Cost == prices[3], "Cancellation mutated state");
                Require(sent.Count == messages + 1 && sent[^1] == prices[3], "Cancellation did not traverse native choice message");
                cancel = false;
                owner.Gold = prices[3] - 1; messages = sent.Count;
                Require(!entry.OnTryPurchaseWrapper(null, false, true).GetAwaiter().GetResult() && sent.Count == messages, "Insufficient gold entered the choice/message path");
                Require(owner.Deck.Cards.Count == count && owner.ExtraFields.CardShopRemovalsUsed == prior + 3, "Insufficient gold mutated deck/counter");
                owner.Gold = prices[3];
                Require(entry.EnoughGold, "Exact price is not affordable");
                var saved = owner.ExtraFields.ToSerializable();
                var restored = JsonSerializer.Deserialize(JsonSerializer.Serialize(saved, JsonSerializationUtility.GetTypeInfo<SerializableExtraPlayerFields>()), JsonSerializationUtility.GetTypeInfo<SerializableExtraPlayerFields>())!;
                NativeConsoleFixture.Set(owner, nameof(owner.ExtraFields), ExtraPlayerFields.FromSerializable(restored));
                Require(new MerchantCardRemovalEntry(owner).Cost == prices[3], "Recreated merchant lost serialized counter/discount");

                // Remote completion must use the already-transmitted cost, not recalculate local discounts.
                LocalContext.NetId = other.NetId;
                int remoteView = entry.Cost;
                var complete = AccessTools.Method(typeof(OneOffSynchronizer), "DoMerchantCardRemoval");
                int transmitted = prices[3];
                Require(((Task<bool>)complete.Invoke(sync, [owner, transmitted, true])!).GetAwaiter().GetResult(), "Remote completion rejected");
                Require(owner.Gold == 0 && owner.ExtraFields.CardShopRemovalsUsed == prior + 4 && owner.Deck.Cards.Count == count - 1, "Remote completion recalculated transmitted price or missed owner");
                if ((discount & 1) != 0) Require(remoteView != transmitted, "Fixture did not exercise MembershipCard's local-owner guard");
                LocalContext.NetId = ownerId;
                var ownerRelics = (List<RelicModel>)AccessTools.Field(typeof(Player), "_relics").GetValue(owner)!;
                ownerRelics.RemoveAll(r => r is AThousandCuts);
                int nativeBase = (high ? 100 : 75) + (high ? 50 : 25) * (prior + 4);
                decimal factor = (discount & 1) != 0 ? 0.5m : 1m;
                if ((discount & 2) != 0) factor *= 0.8m;
                int nativeCost = (int)(nativeBase * factor);
                Require(entry.Cost == nativeCost && entry.Used && !entry.IsStocked, "Relic loss erased price/used history");
                owner.Gold = 10000;
                int beforeRejected = sent.Count;
                Require(!entry.OnTryPurchaseWrapper(null, false, true).GetAwaiter().GetResult() && sent.Count == beforeRejected, "Used native slot reopened after relic loss");
                var newShop = new MerchantCardRemovalEntry(owner);
                Require(newShop.Cost == nativeCost && !newShop.Used, "New non-owner shop price/history differs");
                Require(newShop.OnTryPurchaseWrapper(null, false, true).GetAwaiter().GetResult(), "Native non-owner purchase failed");
                Require(sent[^1] == nativeCost && owner.Gold == 10000 - nativeCost && owner.ExtraFields.CardShopRemovalsUsed == prior + 5, "Native non-owner payment/history differs");
                newShop.SetUsed();
                Require(newShop.Cost == (int)((nativeBase + (high ? 50 : 25)) * factor) && newShop.Used, "Non-owner native rising control failed");
                NativeConsoleFixture.Relic<AThousandCuts>(owner);
                Require(newShop.Cost == prices[0] && !newShop.Used, "Late relic reacquisition failed");
                owner.Gold = prices[0];
                int beforeExact = sent.Count;
                Require(newShop.OnTryPurchaseWrapper(null, false, true).GetAwaiter().GetResult(), "Exact-gold public purchase failed");
                Require(owner.Gold == 0 && owner.Deck.Cards.Count == 0 && sent.Count == beforeExact + 1 && sent[^1] == prices[0] && owner.ExtraFields.CardShopRemovalsUsed == prior + 6, "Exact-gold purchase duplicated or lost payment/removal");
                purchases++;
                ownerRelics.RemoveAll(r => r is MembershipCard or TheCourier);
                Require(newShop.Cost == (high ? 100 : 75), "ATC price froze a removed discount");
            }
        }
        finally
        {
            NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.OneOffSynchronizer), previousSync);
            LocalContext.NetId = previousLocal;
            harmony.UnpatchAll(harmony.Id);
        }
        Console.WriteLine($"PASS: {purchases} public native constant-first-price ATC purchases at prior counters 0/4, 32 native non-owner rising-price controls and relic loss/reacquisition, both inflation tiers, owner-local Membership/Courier/combined hooks, sent price, actual gold/deck/counter, cancellation, affordability, serialized counter and remote cost-consumption. UI selection and desktop services substituted; native singleplayer no-op transport observed. No game, live UI, full save/load or network acceptance.");
    }

    private static void CheckWithoutUiPriceReads()
    {
        inflation = cancel = false; sent.Clear();
        var owner = NativeConsoleFixture.Player(1);
        LocalContext.NetId = owner.NetId;
        var run = RunState.CreateForTest(players: [owner], seed: "SHOPNOREADS");
        run.PushRoom(new MerchantRoom());
        NativeConsoleFixture.Relic<AThousandCuts>(owner);
        NativeConsoleFixture.Relic<MembershipCard>(owner);
        for (int i = 0; i < 3; i++) owner.Deck.AddInternal(run.CreateCard<StrikeIronclad>(owner));
        var service = new NetSingleplayerGameService();
        using var sync = new OneOffSynchronizer(new RunLocationTargetedMessageBuffer(service), service, run, owner.NetId);
        NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.OneOffSynchronizer), sync);
        var entry = new MerchantCardRemovalEntry(owner);
        // Deliberately invalidate the constructor's cached base, then perform public purchases
        // without reading Cost/Used/EnoughGold or invoking any UI update between them.
        owner.ExtraFields.CardShopRemovalsUsed = 4;
        int[] expected = [37, 37, 37];
        for (int i = 0; i < expected.Length; i++)
        {
            int gold = owner.Gold;
            Require(entry.OnTryPurchaseWrapper(null, false, true).GetAwaiter().GetResult(), "Purchase without UI refresh failed");
            Require(sent.Count == i + 1 && sent[^1] == expected[i] && gold - owner.Gold == expected[i],
                $"Public purchase depends on prior UI price read: n={4 + i}, expected={expected[i]}, sent={sent.LastOrDefault()}, debit={gold - owner.Gold}");
            Require(owner.ExtraFields.CardShopRemovalsUsed == 5 + i && owner.Deck.Cards.Count == 2 - i,
                "Public purchase without UI refresh lost counter or card");
        }
        Console.WriteLine("PASS: public native purchase alone refreshes stale cached price at prior counter 4; 3 consecutive removals charge 37/37/37 with Membership Card, without intervening UI/price/Used reads.");
    }
}
