using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Runs;

internal static class RarityPoolChecks
{
    private static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }

    internal static void Run()
    {
        var harmony = new Harmony("LaRata.RarityPoolChecks");
        NativeConsoleFixture.PatchDesktop(harmony);
        try
        {
            // Reuse existing identity/base/upgrade assertions without invoking combat or RNG fixtures.
            CardWaveChecks.Run();
            var neutrals = LaRataCardCatalog.Neutrals;
            Require(neutrals.Length == 23 && neutrals.All(c => c.Pool is ColorlessCardPool), "Neutral count or pool changed");
            Type[] moved = [typeof(UmbralStitch), typeof(CrownExchange), typeof(MidnightRehearsal),
                typeof(GutterFeint), typeof(RagpickersGuard), typeof(CinderReceipt), typeof(VenomousEncore), typeof(GraveyardShift)];
            foreach (var type in moved)
            {
                var canonical = neutrals.Single(c => c.GetType() == type);
                var card = canonical.ToMutable();
                Require(card.Rarity == CardRarity.Uncommon, "Former Common was not moved to Uncommon: " + card.Id);
                card.UpgradeInternal(); card.FinalizeUpgradeInternal();
                Require(card.Rarity == CardRarity.Uncommon, "Upgrade changed rarity: " + card.Id);
                Console.WriteLine($"RARITY {card.Id} Common->Uncommon base+upgrade");
            }
            var prizes = LaRataCardCatalog.BossPrizes;
            Require(prizes.Length == 10 && prizes.All(c => c.Rarity == CardRarity.Rare), "Royal prize count/rarity changed");
            Require(!neutrals.Select(c => c.Id).Intersect(prizes.Select(c => c.Id)).Any(), "Royal prizes entered regular neutral pool");
            var classes = LaRataCardCatalog.Entries.Where(e => e.BoosterEligibility == LaRataBoosterEligibility.ClassPool).ToArray();
            Require(classes.Length == 2 && classes.All(e => !neutrals.Any(c => c.Id == e.ModelId)), "Class cards entered neutral pool");
            for (int count = 1; count <= 4; count++)
            {
                var players = Enumerable.Range(1, count).Select(i => NativeConsoleFixture.Player((ulong)i)).ToArray();
                var run = RunState.CreateForTest(players: players, seed: "RARITYPOOL");
                var owner = players[^1];
                var native = ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint);
                var offered = neutrals.Take(5).ToArray();
                var pool = BoosterRelic.NeutralBonusPool(native, offered);
                Require(pool.Select(c => c.Id).Distinct().Count() == pool.Length, "Duplicate IDs in combined neutral pool");
                Require(!pool.Any(c => offered.Any(o => o.Id == c.Id) || prizes.Any(p => p.Id == c.Id)), "Offered/prize card leaked into bonus pool");
                Require(neutrals.Skip(5).All(c => pool.Any(p => p.Id == c.Id)), "Regular neutral missing from bonus pool");
                Require(pool.All(c => c.Rarity is CardRarity.Uncommon or CardRarity.Rare), "Common remains in current combined neutral pool");
                Console.WriteLine($"POOL modeledPlayers={count} owner={owner.NetId} remainingU={pool.Count(c => c.Rarity == CardRarity.Uncommon)} remainingR={pool.Count(c => c.Rarity == CardRarity.Rare)}");
            }
            foreach (Type type in new[] { typeof(RelicRemover), typeof(SootboundTicket), typeof(StairwellKey), typeof(MockingBell), typeof(BrassScavenger), typeof(MoonlitStopper) })
            {
                var relic = ModelDb.GetById<RelicModel>(ModelDb.GetId(type));
                Require(relic.PackedIconPath.StartsWith("res://LaRata/images/relics/"), "Unexpected relic asset binding: " + relic.Id);
                Console.WriteLine($"RELIC_ASSET {relic.Id} {relic.PackedIconPath}");
            }
            Console.WriteLine("PASS: 60 catalog IDs, 23 regular neutrals at 0/16/7, eight Common->Uncommon base/upgrade mappings, 10 separate Rare prizes, two separate class cards, native U/R banners and 1-4-player modeled pool membership/deduplication. No booster draws/statistics, gameplay, UI, save/load, network or final weight approval.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }
}
