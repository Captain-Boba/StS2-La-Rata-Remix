using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

internal static class CardWaveChecks
{
    private static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }
    private static int bossBannerRequests;

    // Observe the requested native material without entering Godot's resource
    // loader in this console fixture. This proves routing, not rendered color.
    private static bool ObserveBossBanner(CardModel __instance, ref Godot.Material? __result)
    {
        Require(ReferenceEquals(__instance, ModelDb.Card<ScepterStorm>()), "Prize requested a different boss material");
        bossBannerRequests++;
        __result = null;
        return false;
    }

    private static void CheckBannerBindings(IReadOnlyList<LaRataCardEntry> entries)
    {
        var harmony = new Harmony("LaRata.ContentChecks.BannerRouting");
        harmony.Patch(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.BannerMaterial)),
            prefix: new(typeof(CardWaveChecks), nameof(ObserveBossBanner)));
        int prizes = 0, ordinary = 0;
        try
        {
            foreach (var entry in entries.Where(e => e.Model.Rarity is CardRarity.Uncommon or CardRarity.Rare))
            foreach (bool upgraded in new[] { false, true })
            {
                var card = entry.Model.ToMutable();
                if (upgraded) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
                bool prize = entry.Role == LaRataCardRole.PlayableBossVariant;
                Require(card.Rarity == entry.Model.Rarity && (!prize || card.Rarity == CardRarity.Rare), "Style changed actual rarity: " + card.Id);
                bossBannerRequests = 0;
                var material = ((BaseLib.Abstracts.CustomCardModel)card).CreateCustomBannerMaterial;
                Require(material == null && bossBannerRequests == (prize ? 1 : 0), "Boss banner/ordinary fallback routing: " + card.Id);
                if (prize) prizes++; else ordinary++;
            }
            Require(prizes == 20 && ordinary == 72, "Banner base/upgrade control coverage changed");
            Console.WriteLine("PASS: 10 Rare prize banner routes and 36 ordinary rarity fallbacks, each base/upgraded; exact ScepterStorm material getter observed. Resource return substituted; no Godot/UI/color acceptance.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }

    internal static void Run()
    {
        var entries = LaRataCardCatalog.Entries;
        Require(entries.Count == 71 && entries.Select(e => e.ModelId).Distinct().Count() == 71, "Catalog is incomplete or repeats model IDs");
        Require(entries.Count(e => e.Role == LaRataCardRole.Player) == 36 && entries.Count(e => e.Role == LaRataCardRole.PlayableBossVariant) == 10 && entries.Count(e => e.Role == LaRataCardRole.BossExclusive) == 25, "Catalog roles differ");
        Require(entries.Where(e => e.BossId.HasValue).Select(e => e.BossId!.Value).Order().SequenceEqual(Enumerable.Range(0, 48)), "Boss identity slots changed");
        Require(entries.Where(e => e.BossId.HasValue && !e.BossEligible).Select(e => e.BossId!.Value).Order().SequenceEqual(new[] { 3, 20, 21, 25, 26, 28, 47 }), "Retired boss cards returned or other cards disappeared");
        Require(entries.Where(e => e.Role == LaRataCardRole.PlayableBossVariant).All(e => e.BossId == null), "Prize alias creates duplicate boss execution");
        RoyalBossEffects.ValidateCatalog();
        var reset = ModelDb.Card<LordTibosReset>().ToMutable();
        Require(reset.Type == CardType.Skill && reset.Rarity == CardRarity.Rare && reset.EnergyCost.Canonical == 2 && reset.Keywords.Contains(CardKeyword.Exhaust), "Reset base contract");
        reset.UpgradeInternal(); reset.FinalizeUpgradeInternal();
        Require(reset.EnergyCost.GetWithModifiers(CostModifiers.None) == 1 && reset.Keywords.Contains(CardKeyword.Exhaust), "Reset upgrade costs 1 and retains Exhaust");
        CheckBannerBindings(entries);
        foreach (decimal raw in new decimal[] { 1, 5, 12, 25 })
        {
            Require(RoyalBossEffects.CardBlock(ModelDb.Monster<RatKing>(), raw) == raw / 2 &&
                RoyalBossEffects.CardBlock(ModelDb.Monster<TwoTailedRat>(), raw) == raw, "Boss adapter Block scope differs");
            Require(RoyalBossEffects.CardHealing(ModelDb.Monster<RatKing>(), raw) == Math.Max(1, Math.Floor(raw / 4)) &&
                RoyalBossEffects.CardHealing(ModelDb.Monster<TwoTailedRat>(), raw) == raw, "Boss adapter healing scope differs");
        }
        Require(LaRataCardCatalog.Neutrals.Count(c => c.Rarity == CardRarity.Common) == 0 &&
            LaRataCardCatalog.Neutrals.Count(c => c.Rarity == CardRarity.Uncommon) == 21 &&
            LaRataCardCatalog.Neutrals.Count(c => c.Rarity == CardRarity.Rare) == 13, "Neutral rarities differ from 0/21/13 contract; booster weights remain unchanged");
        var cases = new (CardModel Card, int Cost, int UpCost, bool Exhaust, (string Key, decimal Base, decimal Up)[] Values)[]
        {
            (ModelDb.Card<SerKevsThornBuster>(), 1, 1, true, [("Cards",1,2),("VulnerablePower",2,3)]),
            (ModelDb.Card<WrathOfTheBigO>(), 3, 2, true, []),
            (ModelDb.Card<TheKikilyst>(), 2, 2, true, [("Spread",25,50)]),
            (ModelDb.Card<GutterFeint>(), 0, 0, false, [("Damage",4,6),("Block",3,5)]),
            (ModelDb.Card<RagpickersGuard>(), 1, 1, false, [("Block",9,12)]),
            (ModelDb.Card<CinderReceipt>(), 1, 1, false, [("VigorPower",10,15)]),
            (ModelDb.Card<VenomousEncore>(), 1, 1, false, [("Damage",5,7),("DoomPower",5,10)]),
            (ModelDb.Card<GraveyardShift>(), 1, 1, true, [("CalculationExtra",6,9)]),
            (ModelDb.Card<ScrapCoronation>(), 1, 1, true, [("Forge",8,12),("Cards",1,1)]),
            (ModelDb.Card<ColdCircuit>(), 1, 1, false, [("Cards",1,2)]),
            (ModelDb.Card<BorrowedBackbone>(), 1, 1, false, [("Block",8,11),("DoomPower",5,8)]),
            (ModelDb.Card<SmugglersDividend>(), 1, 1, false, [("VigorPower",3,5)]),
            (ModelDb.Card<CourtOfCinders>(), 1, 1, false, [("PoisonPower",5,8)]),
            (ModelDb.Card<SecondShadow>(), 2, 2, false, [("AfterimagePower",1,2)])
        };
        foreach (var row in cases)
        {
            var card = row.Card.ToMutable();
            Require(card.EnergyCost.Canonical == row.Cost && card.Keywords.Contains(CardKeyword.Exhaust) == row.Exhaust, $"Base cost/Exhaust differs: {card.Id}");
            foreach (var v in row.Values) Require(card.DynamicVars[v.Key].BaseValue == v.Base, $"Base {v.Key} differs: {card.Id}");
            card.UpgradeInternal(); card.FinalizeUpgradeInternal();
            Require(card.EnergyCost.GetWithModifiers(CostModifiers.None) == row.UpCost, $"Upgrade cost differs: {card.Id}");
            foreach (var v in row.Values) Require(card.DynamicVars[v.Key].BaseValue == v.Up && row.Card.DynamicVars[v.Key].BaseValue == v.Base, $"Upgrade/canonical {v.Key} differs: {card.Id}");
        }
        // Native effect coverage lives in --followup-cards; do not retain the obsolete substituted draw fixture.
        Console.WriteLine($"PASS: catalog identities/roles and {cases.Length} exact base/upgrade contracts. Metadata only; native effects are checked separately by --followup-cards.");
    }
}
