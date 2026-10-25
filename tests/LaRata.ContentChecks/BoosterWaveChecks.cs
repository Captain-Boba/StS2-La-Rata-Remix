using HarmonyLib;
using System.Text.Json;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using LaRata;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

internal static class BoosterWaveChecks
{
    private static int loadedSavedCards;
    private static bool EndingCombat(ref bool __result) { __result = true; return false; }
    private static void ObserveSavedLoad() => loadedSavedCards++;
    private static void RejectReroll() => throw new Exception("Saved pack attempted to reroll contents");
    private static Rng? controlled;
    private static Func<int, int>? choose;
    private static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }
    private static bool ControlledRoll(Rng __instance, int maxExclusive, ref int __result)
    {
        if (!ReferenceEquals(__instance, controlled)) return true;
        __result = choose!(maxExclusive);
        Require(__result >= 0 && __result < maxExclusive, "Fixture roll is out of range");
        return false;
    }
    private static bool ControlledRange(Rng __instance, int minInclusive, int maxExclusive, ref int __result)
    {
        if (!ReferenceEquals(__instance, controlled)) return true;
        int value = 0;
        ControlledRoll(__instance, maxExclusive - minInclusive, ref value);
        __result = minInclusive + value;
        return false;
    }

    internal static void Run()
    {
        var harmony = new Harmony("LaRata.BoosterWaveChecks");
        NativeConsoleFixture.PatchDesktop(harmony);
        harmony.Patch(AccessTools.Method(typeof(Rng), nameof(Rng.NextInt), [typeof(int)]), prefix: new(typeof(BoosterWaveChecks), nameof(ControlledRoll)));
        harmony.Patch(AccessTools.Method(typeof(Rng), nameof(Rng.NextInt), [typeof(int), typeof(int)]), prefix: new(typeof(BoosterWaveChecks), nameof(ControlledRange)));
        try
        {
            var neutral = LaRataCardCatalog.Neutrals;
            // The selector supports Common class cards even though La Rata neutrals no longer do.
            // This fixture documents the current algorithm; its weights are not new design approval.
            CardModel[] samples = [ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Anger>(),
                neutral.First(c => c.Rarity == CardRarity.Uncommon), neutral.First(c => c.Rarity == CardRarity.Rare)];
            int[] observed = new int[3];
            // Exhaustively enumerate the category selector's 100 equally likely integer slots.
            for (int roll = 0; roll < 100; roll++)
            {
                controlled = new Rng(0); int calls = 0;
                choose = bound => ++calls == 1 ? (bound == 100 ? roll : throw new Exception("Expected total rarity weight 100"))
                    : bound == 1 ? 0 : throw new Exception("Expected one card inside selected rarity");
                var actual = BoosterRelic.SelectWeighted(samples, 1, CardMultiplayerConstraint.None, controlled, BoosterRarityPolicy.ClassMain).Single();
                observed[Array.IndexOf(samples, actual)]++;
                Require(calls == 2, "Unexpected selector RNG consumption");
            }
            Require(observed.SequenceEqual(new[] { 60, 30, 10 }), "Category selector is not exactly 60/30/10");
            // Common absent: surviving 30:10 weights must become 75:25, not fallback to Common.
            observed = new int[2];
            for (int roll = 0; roll < 40; roll++)
            {
                controlled = new Rng(0); int calls = 0;
                choose = bound => ++calls == 1 ? (bound == 40 ? roll : throw new Exception("Empty category was not removed")) : 0;
                var actual = BoosterRelic.SelectWeighted(samples.Skip(1), 1, CardMultiplayerConstraint.None, controlled, BoosterRarityPolicy.ClassMain).Single();
                observed[actual.Rarity == CardRarity.Uncommon ? 0 : 1]++;
            }
            Require(observed.SequenceEqual(new[] { 30, 10 }), "Missing-Common renormalization differs");
            // Explicit neutral policy must ignore an injected Common, not merely benefit from an empty category.
            observed = new int[2];
            for (int roll = 0; roll < 100; roll++)
            {
                controlled = new Rng(0); int calls = 0;
                choose = bound => ++calls == 1 ? (bound == 100 ? roll : throw new Exception("Neutral policy total differs")) : 0;
                var actual = BoosterRelic.SelectWeighted(samples, 1, CardMultiplayerConstraint.None, controlled, BoosterRarityPolicy.Neutral).Single();
                Require(actual.Rarity != CardRarity.Common, "Neutral policy admitted an injected Common");
                observed[actual.Rarity == CardRarity.Uncommon ? 0 : 1]++;
            }
            Require(observed.SequenceEqual(new[] { 75, 25 }), "Explicit neutral selector is not 75/25");
            controlled = null;
            bool neutralShort = false;
            try { BoosterRelic.SelectWeighted(samples, 3, CardMultiplayerConstraint.None, new Rng(1), BoosterRarityPolicy.Neutral); }
            catch (InvalidOperationException) { neutralShort = true; }
            Require(neutralShort, "Unsupported Common satisfied neutral pool size");
            foreach (var only in samples.Skip(1))
            {
                var one = BoosterRelic.SelectWeighted([only], 1, CardMultiplayerConstraint.None, new Rng(1), BoosterRarityPolicy.Neutral);
                Require(one.Single().Id == only.Id, "Single surviving neutral rarity did not renormalize");
            }
            var all = BoosterRelic.SelectWeighted(samples.Concat(samples), 3, CardMultiplayerConstraint.None, new Rng(3), BoosterRarityPolicy.ClassMain);
            Require(all.Select(c => c.Id).Distinct().Count() == 3, "Selection duplicated IDs or failed after category exhaustion");
            bool exhausted = false;
            try { BoosterRelic.SelectWeighted(samples, 4, CardMultiplayerConstraint.None, new Rng(0), BoosterRarityPolicy.ClassMain); }
            catch (InvalidOperationException) { exhausted = true; }
            Require(exhausted, "Undersized pool did not fail explicitly");
            Require(BoosterRelic.SelectWeighted(samples, 0, CardMultiplayerConstraint.None, new Rng(0), BoosterRarityPolicy.ClassMain).Count == 0, "Zero draw produces cards");

            var owner = NativeConsoleFixture.Player(1);
            var run = RunState.CreateForTest(players: [owner], seed: "BOOSTERWAVE");
            var prizes = LaRataCardCatalog.BossPrizes.Select(c => c.Id).ToHashSet();
            var enemyCards = LaRataCardCatalog.Entries.Where(e => e.Role == LaRataCardRole.BossExclusive).Select(e => e.ModelId).ToHashSet();
            // Exercise the actual generator, forcing only raw integer draws. Not a probability sample.
            for (int gate = 0; gate < 5; gate++)
            {
                int calls = 0;
                controlled = new Rng(0);
                choose = bound => calls++ == 10 ? (bound == 5 ? gate : throw new Exception("Expected one five-way boss gate after five main selections")) : 0;
                var offer = BoosterRelic.GenerateContents(owner, 5, controlled).Select(r => r.Card).ToArray();
                Require(offer.Count(c => prizes.Contains(c.Id)) == (gate == 0 ? 1 : 0), "Royal boss gate differs from exactly one successful integer out of five");
                Require(!offer.Skip(5).Any(c => prizes.Contains(c.Id)), "Royal prize leaked into a bonus slot");
            }
            // Every prize is reachable in each of the five replacement slots, outside bonus slots.
            var sortedPrizes = LaRataCardCatalog.BossPrizes.OrderBy(c => c.Id.Entry, StringComparer.Ordinal).ToArray();
            for (int slot = 0; slot < 5; slot++)
            for (int prize = 0; prize < 10; prize++)
            {
                int calls = 0;
                controlled = new Rng(0);
                choose = bound => calls++ switch { 10 => bound == 5 ? 0 : throw new Exception("Boss gate bound"), 11 => bound == 5 ? slot : throw new Exception("Replacement bound"), 12 => bound == 10 ? prize : throw new Exception("Prize uniform bound"), _ => 0 };
                var offer = BoosterRelic.GenerateContents(owner, 5, controlled).Select(c => c.Card).ToArray();
                Require(offer[slot].Id == sortedPrizes[prize].Id && offer.Count(c => prizes.Contains(c.Id)) == 1, "Uniform prize/slot routing differs");
                Require(!offer.Skip(5).Any(c => prizes.Contains(c.Id)), "Prize entered bonus");
            }
            // Production routing boundaries, not just tests of isolated weight arrays.
            for (int pack = 0; pack < 6; pack++)
            foreach (int mainRoll in (pack == 5 ? new[] { 0, 74, 75, 99 } : new[] { 0, 59, 60, 89, 90, 99 }))
            foreach (int bonusRoll in new[] { 0, 74, 75, 99 })
            {
                int calls = 0, firstBonus = pack == 5 ? 11 : 10;
                controlled = new Rng(0);
                choose = bound =>
                {
                    int call = calls++;
                    if (call == 0) { Require(bound == 100, "Main policy total differs"); return mainRoll; }
                    if (pack == 5 && call == 10) return 1; // The separate royal gate fails.
                    if (call == firstBonus || call == firstBonus + 2) { Require(bound == 100, "Bonus policy total differs"); return bonusRoll; }
                    return 0;
                };
                var offer = BoosterRelic.GenerateContents(owner, pack, controlled).Select(c => c.Card).ToArray();
                var expectedMain = pack == 5 ? (mainRoll < 75 ? CardRarity.Uncommon : CardRarity.Rare) : mainRoll < 60 ? CardRarity.Common : mainRoll < 90 ? CardRarity.Uncommon : CardRarity.Rare;
                var expectedBonus = bonusRoll < 75 ? CardRarity.Uncommon : CardRarity.Rare;
                Require(offer[0].Rarity == expectedMain && offer.Skip(5).All(c => c.Rarity == expectedBonus), "Production routed the wrong rarity policy");
            }
            controlled = null;
            CheckSavedContents(owner, run);
            CheckUnlocksAndPartyPools();
            for (uint seed = 0; seed < 16; seed++)
            for (int pack = 0; pack < 6; pack++)
            {
                var rng = new Rng(seed);
                var result = BoosterRelic.GenerateContents(owner, pack, rng).Select(r => r.Card).ToArray();
                var replayRng = new Rng(seed);
                var replay = BoosterRelic.GenerateContents(owner, pack, replayRng).Select(r => r.Card).ToArray();
                Require(result.Length == 7 && result.Select(c => c.Id).Distinct().Count() == 7, "Pack does not contain seven distinct cards");
                Require(result.All(c => c.Owner == owner && !enemyCards.Contains(c.Id)), "Invalid owner or boss-exclusive pack card");
                Require(result.Count(c => prizes.Contains(c.Id)) <= (pack == 5 ? 1 : 0), "Unexpected royal prize count");
                Require(!result.Skip(5).Any(c => prizes.Contains(c.Id)), "Bonus slot contains royal prize");
                Require(result.Select(c => c.Id).SequenceEqual(replay.Select(c => c.Id)) && rng.Counter == replayRng.Counter, "Production pack replay changes IDs/order/RNG consumption");
                Require(result.All(c => c.CurrentUpgradeLevel == 0), "New pack performed an unexpected upgrade roll");
            }
        }
        finally { controlled = null; choose = null; harmony.UnpatchAll(harmony.Id); }
        Console.WriteLine("PASS: production class selector has exact 60/30/10 and explicit neutral 75/25 slots; 136 production routing boundaries, 50 prize/slot boundaries, unchanged saved 5/7-card contents, no duplicate IDs, explicit insufficient-pool failure, five-way royal gate, 96 six-pack seed cases with exact native-Rng replay/ownership. Controlled boundary draws are not a statistical estimate; no UI/network/save-session test.");
    }
    private static void CheckSavedContents(MegaCrit.Sts2.Core.Entities.Players.Player owner, RunState run)
    {
        var savedPath = new Harmony("LaRata.SavedBoosterPath");
        savedPath.Patch(AccessTools.PropertyGetter(typeof(MegaCrit.Sts2.Core.Combat.CombatManager), "IsEnding"), prefix: new(typeof(BoosterWaveChecks), nameof(EndingCombat)));
        savedPath.Patch(AccessTools.Method(typeof(BoosterRelic), nameof(BoosterRelic.GenerateContents)), prefix: new(typeof(BoosterWaveChecks), nameof(RejectReroll)));
        savedPath.Patch(AccessTools.Method(typeof(RunState), nameof(RunState.LoadCard)), prefix: new(typeof(BoosterWaveChecks), nameof(ObserveSavedLoad)));
        try
        {
        foreach (Type type in new[] { typeof(IroncladBooster), typeof(SilentBooster), typeof(DefectBooster), typeof(RegentBooster), typeof(NecrobinderBooster), typeof(RoyalBooster) })
        foreach (int count in new[] { 5, 7 })
        {
            var canonical = ModelDb.GetById<RelicModel>(ModelDb.GetId(type));
            var pack = (BoosterRelic)canonical.ToMutable();
            var cards = LaRataCardCatalog.Neutrals.Take(count).Select(c => run.CreateCard(c, owner)).ToArray();
            cards[0].UpgradeInternal(); cards[0].FinalizeUpgradeInternal();
            pack.Contents = cards.Select(c => c.ToSerializable()).Reverse().ToList();
            pack.StarterPending = true;
            var info = JsonSerializationUtility.GetTypeInfo<SavedProperties>();
            var json = JsonSerializer.Serialize(SavedProperties.From(pack)!, info);
            var restored = (BoosterRelic)canonical.ToMutable();
            JsonSerializer.Deserialize(json, info)!.Fill(restored);
            Require(restored.StarterPending && !restored.Opened && JsonSerializer.Serialize(SavedProperties.From(restored)!, info) == json, "Pending pack saved fields/order/upgrade changed");
            var loaded = restored.Contents.Select(c => run.LoadCard(c, owner)).ToArray();
            Require(loaded.Select(c => c.Id).SequenceEqual(cards.Reverse().Select(c => c.Id)) && loaded[^1].CurrentUpgradeLevel == 1 && loaded.All(c => c.Owner == owner), "Native load changed pending IDs/order/upgrades/owner");
            restored.Owner = owner;
            loadedSavedCards = 0;
            ((Task)AccessTools.Method(typeof(BoosterRelic), "Open").Invoke(restored, null)!).GetAwaiter().GetResult();
            Require(loadedSavedCards == count && JsonSerializer.Serialize(SavedProperties.From(restored)!, info) == json, "Actual saved-open path rerolled/changed saved contents before combat-end deferral");
        }
        }
        finally { savedPath.UnpatchAll(savedPath.Id); }
    }

    private static void CheckUnlocksAndPartyPools()
    {
        CardPoolModel[] pools = [ModelDb.CardPool<MegaCrit.Sts2.Core.Models.CardPools.IroncladCardPool>(), ModelDb.CardPool<MegaCrit.Sts2.Core.Models.CardPools.SilentCardPool>(), ModelDb.CardPool<MegaCrit.Sts2.Core.Models.CardPools.DefectCardPool>(), ModelDb.CardPool<MegaCrit.Sts2.Core.Models.CardPools.RegentCardPool>(), ModelDb.CardPool<MegaCrit.Sts2.Core.Models.CardPools.NecrobinderCardPool>()];
        foreach (int count in new[] { 1, 2, 3, 4 })
        foreach (bool unlocked in new[] { false, true })
        {
            var players = Enumerable.Range(1, count).Select(i => NativeConsoleFixture.Player((ulong)i)).ToArray();
            var owner = players[^1];
            NativeConsoleFixture.Set(owner, nameof(owner.UnlockState), unlocked ? MegaCrit.Sts2.Core.Unlocks.UnlockState.all : MegaCrit.Sts2.Core.Unlocks.UnlockState.none);
            _ = RunState.CreateForTest(players: players, seed: "BOOSTERPARTY");
            var constraint = owner.RunState.CardMultiplayerConstraint;
            for (int pack = 0; pack < 6; pack++)
            {
                var rng = new Rng(117);
                var cards = BoosterRelic.GenerateContents(owner, pack, rng).Select(r => r.Card).ToArray();
                var replayRng = new Rng(117);
                var replay = BoosterRelic.GenerateContents(owner, pack, replayRng).Select(r => r.Card.Id).ToArray();
                Require(cards.Select(c => c.Id).SequenceEqual(replay) && rng.Counter == replayRng.Counter, "Party/unlock replay differs");
                Require(cards.Length == 7 && cards.Select(c => c.Id).Distinct().Count() == 7 && cards.All(c => c.Owner == owner), "Party pack size/ownership/dedupe differs");
                Require(cards.All(c => constraint == CardMultiplayerConstraint.None || c.MultiplayerConstraint == CardMultiplayerConstraint.None || c.MultiplayerConstraint == constraint), "Wrong SP/MP constraint");
                if (pack < 5)
                {
                    var allowed = pools[pack].GetUnlockedCards(owner.UnlockState, constraint).Select(c => c.Id).ToHashSet();
                    Require(cards.Take(5).All(c => allowed.Contains(c.Id)), "Locked or foreign class card entered main slots");
                }
                var native = ModelDb.CardPool<MegaCrit.Sts2.Core.Models.CardPools.ColorlessCardPool>().GetUnlockedCards(owner.UnlockState, constraint);
                var bonus = BoosterRelic.NeutralBonusPool(native, cards.Take(5)).Select(c => c.Id).ToHashSet();
                Require(cards.Skip(5).All(c => bonus.Contains(c.Id) && c.Rarity is CardRarity.Uncommon or CardRarity.Rare), "Locked/foreign/unsupported bonus card");
            }
        }
        Console.WriteLine("PASS: 48 six-pack owner/unlock/SP-MP model cases (1-4 players, none/all unlocks), seven distinct eligible IDs and deterministic RNG replay. Not real network clients.");
    }
}
