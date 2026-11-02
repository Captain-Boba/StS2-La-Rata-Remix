using BaseLib.Patches.Content;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

// Runs the real shop -> Obtain -> booster Open -> synchronized native reward
// choice -> deck path. No generator/RNG/selection/pile operation is replaced.
// Only transport is captured and replayed; there is no engine UI or live peer.
internal static class RoyalBoosterPathChecks
{
    private static readonly List<byte[]> packets = [];
    private static readonly List<ModelId[]> offers = [];
    private static int checks;
    private static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
    private static void Await(Task task) => task.WaitAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
    private static void Capture(RewardObtainedMessage message)
    {
        var writer = new PacketWriter(); message.Serialize(writer); writer.ZeroByteRemainder();
        packets.Add(writer.Buffer.AsSpan(0, writer.BytePosition).ToArray());
    }
    private static void Observe(List<CardCreationResult> cards, Player player)
    {
        Check(cards.Count == 7 && cards.All(c => c.Card.Owner == player), "Native reward entry lost cards/owner");
        offers.Add(cards.Select(c => c.Card.Id).ToArray());
    }
    private static RewardObtainedMessage Decode(byte[] bytes)
    {
        var reader = new PacketReader(); reader.Reset(bytes); RewardObtainedMessage result = default;
        result.Deserialize(reader); return result;
    }
    private static RunState Run(int count, string seed, bool unlocked)
    {
        var run = DisconnectRegressionChecks.NewRun(count, seed);
        if (!unlocked) foreach (var player in run.Players) NativeConsoleFixture.Set(player, nameof(player.UnlockState), UnlockState.none);
        run.PushRoom(new MerchantRoom()); return run;
    }
    private static void Bind(RunState run, INetGameService service, PlayerChoiceSynchronizer choices, RewardSynchronizer rewards)
    {
        var manager = RunManager.Instance;
        NativeConsoleFixture.Set(manager, "State", run);
        NativeConsoleFixture.Set(manager, nameof(manager.NetService), service);
        NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), choices);
        NativeConsoleFixture.Set(manager, nameof(manager.RewardSynchronizer), rewards);
    }
    private static void Resolve(PlayerChoiceSynchronizer sync, Player owner, int index)
        => sync.ReceiveReplayChoice(owner, 0, PlayerChoiceResult.FromIndexes([index]).ToNetData());

    private static void Case(int count, bool unlocked, string seed, ModelId expected)
    {
        packets.Clear(); offers.Clear();
        var left = Run(count, seed, unlocked); var right = Run(count, seed, unlocked);
        var owner = left.Players[^1]; var peer = right.Players[^1];
        var service = new NetSingleplayerGameService(); var remoteService = new NetSingleplayerGameService();
        using var choices = new PlayerChoiceSynchronizer(service, left);
        using var remoteChoices = new PlayerChoiceSynchronizer(remoteService, right);
        using var rewards = new RewardSynchronizer(new RunLocationTargetedMessageBuffer(service), service, left, owner.NetId);
        using var remoteRewards = new RewardSynchronizer(new RunLocationTargetedMessageBuffer(remoteService), remoteService, right, peer.NetId);
        Bind(left, service, choices, rewards);
        var booster = (RoyalBooster)ModelDb.Relic<RoyalBooster>().ToMutable();
        var entry = new MerchantRelicEntry(booster, owner); int price = entry.Cost;
        var purchase = entry.OnTryPurchaseWrapper(null);
        Check(!purchase.IsCompleted && packets.Count == 1 && offers.Count == 1, "Royal shop must transmit unopened relic and then wait for native choice");
        int index = Array.IndexOf(offers[0], expected);
        Check(index is >= 0 and < 5 && offers[0].Count(id => LaRataCardCatalog.BossPrizes.Any(p => p.Id == id)) == 1, "Expected prize missing from native reward options");
        Check(booster.Contents.Count == 7 && !booster.Opened, "Pending offer must be retained");
        var pendingSave = booster.ToSerializable();
        var remote = (RoyalBooster)Decode(packets.Single()).relicModel!;
        Check(!remote.Opened && remote.Contents.Count == 0, "Shop packet was sent after opening");
        Bind(right, remoteService, remoteChoices, remoteRewards);
        var received = RelicCmd.Obtain(remote, peer);
        Check(!received.IsCompleted && offers.Count == 2 && offers[0].SequenceEqual(offers[1]), "Native peer generation/reward options diverged");
        Resolve(remoteChoices, peer, index); Await(received);
        Bind(left, service, choices, rewards); Resolve(choices, owner, index); Await(purchase);
        Check(purchase.Result && booster.Opened && remote.Opened && booster.Contents.Count == 0 && remote.Contents.Count == 0, "Royal opening completion/cleanup");
        Check(owner.Deck.Cards.Single().Id == expected && peer.Deck.Cards.Single().Id == expected && owner.Deck.Cards.Single().Owner == owner, "Native reward choice did not retain Royal prize");
        Check(left.Players.Take(count - 1).All(p => p.Deck.Cards.Count == 0), "Royal prize entered another player's deck");
        Await(PlayerCmd.LoseGold(price, peer));
        Check(DisconnectRegressionChecks.Snapshot(left, choices) == DisconnectRegressionChecks.Snapshot(right, remoteChoices), "Native final peer checksum/state mismatch");
        var choiceIds = choices.ChoiceIds.ToArray(); Await(booster.AfterObtained());
        Check(choices.ChoiceIds.SequenceEqual(choiceIds) && owner.Deck.Cards.Count == 1, "Opened booster re-opened");

        // Restore the still-pending native serialized relic into a fresh run;
        // Open must use the saved seven cards, not generate another offer.
        var resumed = Run(count, seed + "-DIFFERENT-SEED", unlocked); var resumedOwner = resumed.Players[^1];
        using var resumedChoices = new PlayerChoiceSynchronizer(service, resumed);
        using var resumedRewards = new RewardSynchronizer(new RunLocationTargetedMessageBuffer(service), service, resumed, resumedOwner.NetId);
        Bind(resumed, service, resumedChoices, resumedRewards);
        var saved = (RoyalBooster)RelicModel.FromSerializable(pendingSave);
        var reopen = RelicCmd.Obtain(saved, resumedOwner);
        Check(!reopen.IsCompleted && offers.Count == 3 && offers[2].SequenceEqual(offers[0]), "Pending saved offer changed with run seed");
        Resolve(resumedChoices, resumedOwner, index); Await(reopen);
        Check(saved.Opened && resumedOwner.Deck.Cards.Single().Id == expected, "Saved Royal prize did not reach deck");
        Console.WriteLine($"ROYAL_PATH players={count} unlocks={(unlocked ? "all" : "none")} seed={seed} prize={expected.Entry} slot={index} native-choice/deck/packet/save PASS");
    }

    internal static void Run(Harmony harmony)
    {
        checks = 0;
        var binary = typeof(BoosterRelic).Assembly;
        Console.WriteLine($"ROYAL_BINARY path={binary.Location} version={binary.GetName().Version} sha256={Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(binary.Location))).ToLowerInvariant()}");
        var manager = RunManager.Instance; var oldRun = manager.DebugOnlyGetState(); var oldNet = manager.NetService;
        var oldChoices = manager.PlayerChoiceSynchronizer; var oldRewards = manager.RewardSynchronizer; var oldLocal = LocalContext.NetId;
        LocalContext.NetId = 999999; // Actual remote-choice reservation/waiter, not TestCardSelector.
        var send = typeof(NetSingleplayerGameService).GetMethods().Single(m => m.Name == "SendMessage" && m.GetParameters().Length == 1).MakeGenericMethod(typeof(RewardObtainedMessage));
        harmony.Patch(send, prefix: new(typeof(RoyalBoosterPathChecks), nameof(Capture)));
        harmony.Patch(AccessTools.Method(typeof(CardSelectCmd), nameof(CardSelectCmd.FromSimpleGridForRewards)), prefix: new(typeof(RoyalBoosterPathChecks), nameof(Observe)));
        try
        {
            var prizeIds = LaRataCardCatalog.BossPrizes.Select(c => c.Id).ToHashSet();
            Check(prizeIds.Count == 10, "Royal prize catalog count");
            foreach (var prize in LaRataCardCatalog.BossPrizes)
            {
                Check(CustomContentDictionary.RegisteredTypes.Contains(prize.GetType()), "Prize missing BaseLib registration " + prize.Id);
                Check(prize.Rarity == CardRarity.Rare && prize.MultiplayerConstraint == CardMultiplayerConstraint.None && !prize.Keywords.Contains(CardKeyword.Unplayable), "Prize SP/MP/playability " + prize.Id);
                Check(prize.Pool is ColorlessCardPool && !ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.None).Any(c => c.Id == prize.Id), "Prize must bind a usable pool without leaking into ordinary neutral rewards");
            }
            foreach (int count in new[] { 1, 2, 3, 4 }) foreach (bool unlocked in new[] { false, true })
            {
                var witnesses = new Dictionary<ModelId, string>(); int gateHits = 0;
                // Natural native RNG, no controlled roll. Fixed seed corpus is
                // a reproducible reachability witness, not the chance formula.
                for (int i = 0; i < 512; i++)
                {
                    string seed = $"ROYALPATH{i}"; var run = Run(count, seed, unlocked); var owner = run.Players[^1];
                    var rng = new Rng(run.Rng.Seed, $"LaRata/booster/v2/{owner.NetId}/{ModelDb.Relic<RoyalBooster>().Id.Entry}/{run.TotalFloor}");
                    var offer = BoosterRelic.GenerateContents(owner, 5, rng);
                    var prize = offer.SingleOrDefault(c => prizeIds.Contains(c.Card.Id));
                    if (prize != null) { gateHits++; witnesses.TryAdd(prize.Card.Id, seed); }
                }
                Check(witnesses.Count == 10, "Natural seed corpus failed to reach all ten prizes");
                Console.WriteLine($"ROYAL_CORPUS players={count} unlocks={(unlocked ? "all" : "none")} hits={gateHits}/512 unique={witnesses.Count}");
                foreach (var witness in witnesses.OrderBy(x => x.Key.Entry, StringComparer.Ordinal)) Case(count, unlocked, witness.Value, witness.Key);
            }
            foreach (string seed in new[] { "ROYALPATH0", "ROYALPATH12" }) FreshSameFloor(seed);
            CourierRestock();
            Console.WriteLine($"PASS: {checks} Royal booster path assertions; all ten prizes through fresh shop/native choice/deck, native packet replay, pending save restoration, 1/2/3/4 players and none/all unlocks. Fresh same-floor duplicate-offer and native Courier restock examined. Exact binary identified above; no live UI/network claim.");
        }
        finally
        {
            LocalContext.NetId = oldLocal;
            NativeConsoleFixture.Set(manager, "State", oldRun); NativeConsoleFixture.Set(manager, nameof(manager.NetService), oldNet);
            NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), oldChoices); NativeConsoleFixture.Set(manager, nameof(manager.RewardSynchronizer), oldRewards);
        }
    }

    private static void FreshSameFloor(string seed)
    {
        packets.Clear(); offers.Clear();
        var run = Run(1, seed, true); var owner = run.Players[0];
        var service = new NetSingleplayerGameService();
        using var choices = new PlayerChoiceSynchronizer(service, run);
        using var rewards = new RewardSynchronizer(new RunLocationTargetedMessageBuffer(service), service, run, owner.NetId);
        Bind(run, service, choices, rewards);
        var packs = new List<RoyalBooster>();
        // Explicitly supplied duplicate shop stock is a conditional diagnostic,
        // not evidence that Courier can naturally supply a second Royal pack.
        for (uint i = 0; i < 2; i++)
        {
            var pack = (RoyalBooster)ModelDb.Relic<RoyalBooster>().ToMutable(); packs.Add(pack);
            Check(!pack.Opened && pack.Contents.Count == 0, "Duplicate diagnostic must start with a genuinely fresh pack");
            var entry = new MerchantRelicEntry(pack, owner); var buy = entry.OnTryPurchaseWrapper(null);
            Check(!buy.IsCompleted && offers.Count == i + 1, "Fresh purchase did not reach reward choice");
            choices.ReceiveReplayChoice(owner, i, PlayerChoiceResult.FromIndexes(Enumerable.Range(0, 7).ToList()).ToNetData());
            Await(buy); Check(buy.Result && pack.Opened, "Fresh duplicate purchase failed");
        }
        Check(!ReferenceEquals(packs[0], packs[1]) && packs[0].FloorAddedToDeck == packs[1].FloorAddedToDeck && offers[0].SequenceEqual(offers[1]), "Same-floor seed correlation changed");
        Check(owner.Deck.Cards.Count == 14 && owner.Deck.Cards.Take(7).Select(c => c.Id).SequenceEqual(owner.Deck.Cards.Skip(7).Select(c => c.Id)), "Repeated offer did not reach native deck twice");
        var prize = offers[0].Where(id => LaRataCardCatalog.BossPrizes.Any(c => c.Id == id)).Select(id => id.Entry);
        Console.WriteLine($"ROYAL_DUPLICATE seed={seed} owner={owner.NetId} floor={run.TotalFloor} fresh=2 sameOffer=true kept=14 prizes=[{string.Join(',', prize)}] offered=[{string.Join(',', offers[0].Select(id => id.Entry))}] scope=explicit-duplicate-stock-not-natural-restock");
    }

    private static void CourierRestock()
    {
        packets.Clear(); offers.Clear();
        var run = Run(1, "ROYALPATH12", true); var owner = run.Players[0];
        var service = new NetSingleplayerGameService();
        using var choices = new PlayerChoiceSynchronizer(service, run);
        using var rewards = new RewardSynchronizer(new RunLocationTargetedMessageBuffer(service), service, run, owner.NetId);
        Bind(run, service, choices, rewards);
        var royal = ModelDb.Relic<RoyalBooster>();
        // Legal exhausted-bag boundary: one Royal remains. Use native bag pulls,
        // rarity rolls, Courier hook and same-entry restock without replacements.
        owner.RelicGrabBag.Populate([royal], new Rng(17));
        owner.AddRelicInternal(ModelDb.Relic<TheCourier>().ToMutable(), silent: true);
        Check(!royal.IsStackable && owner.RelicGrabBag.Contains(royal), "Royal non-stackable/bag precondition");
        var entry = new MerchantRelicEntry(RelicRarity.Common, owner);
        Check(entry.Model is RoyalBooster && !owner.RelicGrabBag.Contains(royal), "Native shop pull did not consume the only Royal");
        var buy = entry.OnTryPurchaseWrapper(null);
        Check(!buy.IsCompleted && offers.Count == 1, "Courier Royal opening missing");
        Resolve(choices, owner, 4); Await(buy);
        Check(buy.Result && entry.IsStocked && entry.Model is Circlet && !owner.RelicGrabBag.Contains(royal), "Courier unexpectedly resupplied consumed Royal");
        for (int i = 0; i < 8; i++)
        {
            var repeat = entry.OnTryPurchaseWrapper(null); Await(repeat);
            Check(repeat.Result && entry.Model is Circlet && !owner.RelicGrabBag.Contains(royal), "Courier exhausted-bag fallback reintroduced Royal");
        }
        Check(offers.Count == 1 && owner.Relics.Count(r => r is RoyalBooster) == 1 && choices.ChoiceIds.Single() == 1, "Courier created a second Royal offer");
        Console.WriteLine("ROYAL_COURIER native-inventory-pull=Royal native-restock=Circlet repeated-restocks=8 Royal-reintroduced=false. Player grab bag does not refresh; duplicate-stock diagnostic is not a natural Courier path.");
    }
}
