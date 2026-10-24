using System.Runtime.CompilerServices;
using System.Text.Json;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

internal static class ShopRemovalChecks
{
    private static bool inflation, cancel, blocked;
    private static int charged, removed;
    private static void Require(bool condition, string reason) { if (!condition) throw new Exception(reason); }
    private static bool Ascension(AscensionLevel level, ref bool __result)
    { __result = inflation && level == AscensionLevel.Inflation; return false; }
    private static bool AllowRemoval(ref bool __result)
    { if (!blocked) return true; __result = false; return false; }
    private static bool SelectCard(ref Task<IEnumerable<CardModel>> __result)
    { __result = Task.FromResult<IEnumerable<CardModel>>(cancel ? [] : [ModelDb.Card<LaRata.Cleanse>().ToMutable()]); return false; }
    private static bool SpendGold(decimal amount, Player player, ref Task __result)
    { charged += (int)amount; player.Gold -= (int)amount; __result = Task.CompletedTask; return false; }
    private static bool RemoveCard(ref Task __result)
    { removed++; __result = Task.CompletedTask; return false; }

    internal static void Run()
    {
        ModelDb.Inject(typeof(AThousandCuts));
        var harmony = new Harmony("LaRata.ShopRemovalChecks");
        harmony.CreateClassProcessor(typeof(RepeatRemovalPatch)).Patch();
        harmony.CreateClassProcessor(typeof(RemovalAvailablePatch)).Patch();
        harmony.CreateClassProcessor(typeof(RepeatRemovalPricePatch)).Patch();
        // Use the real synchronized completion/counter path; replace only UI,
        // audiovisual gold/deck commands and the current run's ascension context.
        harmony.Patch(AccessTools.Method(typeof(RunManager), nameof(RunManager.HasAscension)), prefix: new(typeof(ShopRemovalChecks), nameof(Ascension)));
        harmony.Patch(AccessTools.Method(typeof(Hook), nameof(Hook.ShouldAllowMerchantCardRemoval)), prefix: new(typeof(ShopRemovalChecks), nameof(AllowRemoval)));
        harmony.Patch(AccessTools.Method(typeof(CardSelectCmd), nameof(CardSelectCmd.FromDeckForRemoval)), prefix: new(typeof(ShopRemovalChecks), nameof(SelectCard)));
        harmony.Patch(AccessTools.Method(typeof(PlayerCmd), nameof(PlayerCmd.LoseGold)), prefix: new(typeof(ShopRemovalChecks), nameof(SpendGold)));
        harmony.Patch(AccessTools.Method(typeof(CardPileCmd), nameof(CardPileCmd.RemoveFromDeck), [typeof(CardModel), typeof(bool)]), prefix: new(typeof(ShopRemovalChecks), nameof(RemoveCard)));
        try
        {
            foreach (bool highAscension in new[] { false, true })
            {
                inflation = highAscension; cancel = blocked = false; charged = removed = 0;
                var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
                var relics = new List<RelicModel>();
                AccessTools.Field(typeof(Player), "_relics").SetValue(player, relics);
                AccessTools.Field(typeof(Player), "_runState").SetValue(player, NullRunState.Instance);
                AccessTools.PropertySetter(typeof(Player), nameof(Player.ExtraFields)).Invoke(player, [new ExtraPlayerFields { CardShopRemovalsUsed = 4 }]);
                player.Gold = 10000;
                var entry = new MerchantCardRemovalEntry(player);
                int initial = highAscension ? 300 : 175;
                int step = highAscension ? 50 : 25;
                Require(entry.Cost == initial, "Initial native removal price differs");
                entry.SetUsed();
                Require(entry.Used && !entry.IsStocked, "Non-owner shop was reopened");
                relics.Add(ModelDb.Relic<AThousandCuts>().ToMutable());
                Require(!entry.Used && entry.IsStocked && entry.Cost == initial, "Mid-shop acquisition resets the price or fails to reopen");
                var synchronizer = (OneOffSynchronizer)RuntimeHelpers.GetUninitializedObject(typeof(OneOffSynchronizer));
                var complete = AccessTools.Method(typeof(OneOffSynchronizer), "DoMerchantCardRemoval");
                int expectedSpent = 0;
                for (int purchase = 0; purchase < 3; purchase++)
                {
                    int price = entry.Cost;
                    Require(price == initial + purchase * step, "Repeated removal used a stale/reset price");
                    bool success = ((Task<bool>)complete.Invoke(synchronizer, [player, price, true])!).GetAwaiter().GetResult();
                    expectedSpent += price;
                    Require(success && player.ExtraFields.CardShopRemovalsUsed == 5 + purchase, "Native completion did not advance exactly once");
                    // Read price before Used/SetUsed/UI callbacks: this is the regression.
                    Require(entry.Cost == initial + (purchase + 1) * step, "Price needs a Used getter/UI callback to advance");
                    entry.SetUsed();
                    Require(!entry.Used && charged == expectedSpent && removed == purchase + 1, "Slot/payment/removal lifecycle differs");
                }
                int nextPrice = entry.Cost;
                cancel = true;
                Require(!((Task<bool>)complete.Invoke(synchronizer, [player, nextPrice, true])!).GetAwaiter().GetResult(), "Cancelled removal succeeded");
                Require(entry.Cost == nextPrice && player.ExtraFields.CardShopRemovalsUsed == 7 && charged == expectedSpent && removed == 3, "Cancellation changed price, count or payment");
                player.Gold = nextPrice - 1;
                Require(!entry.EnoughGold, "Affordability used an older price");
                var saved = player.ExtraFields.ToSerializable();
                var restored = JsonSerializer.Deserialize(JsonSerializer.Serialize(saved, JsonSerializationUtility.GetTypeInfo<SerializableExtraPlayerFields>()), JsonSerializationUtility.GetTypeInfo<SerializableExtraPlayerFields>())!;
                AccessTools.PropertySetter(typeof(Player), nameof(Player.ExtraFields)).Invoke(player, [ExtraPlayerFields.FromSerializable(restored)]);
                Require(new MerchantCardRemovalEntry(player).Cost == nextPrice, "Next shop/reloaded native counter resets price");
                blocked = true;
                entry.SetUsed();
                Require(entry.Used && !entry.IsStocked, "Repeat removal bypasses a native removal restriction");
            }
        }
        finally { harmony.UnpatchAll(harmony.Id); }
        Console.WriteLine("PASS: native removal counter advances across repeated purchases at both inflation tiers; prior removals, mid-shop relic acquisition, cancellation, affordability, restrictions and saved-counter reload. UI/payment commands simulated; not a live shop/network test.");
    }
}
