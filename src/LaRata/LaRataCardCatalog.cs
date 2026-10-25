using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace LaRata;

internal enum LaRataCardRole { Player, PlayableBossVariant, BossExclusive }
internal enum LaRataBoosterEligibility { None, Neutral, RoyalPrize, ClassPool }

// Metadata only: listing a model here never registers it in a reward pool or unlocks it.
internal sealed record LaRataCardEntry(
    Func<CardModel> GetModel, string FamilyId, LaRataCardRole Role, string ArtKey,
    int? BossId, bool BossEligible, bool GambitEligible = false,
    LaRataBoosterEligibility BoosterEligibility = LaRataBoosterEligibility.None)
{
    internal CardModel Model => GetModel();
    internal ModelId ModelId => Model.Id;
    internal string BossExecutionKey => FamilyId;
    internal int? LegacyBossIndex => BossId is >= 0 and < 25 ? BossId : null;
    internal bool HasUpgrade => Model.MaxUpgradeLevel > 0;
    internal bool PlayerPlayable => Role != LaRataCardRole.BossExclusive;
    internal string UnlockPolicy => "AlwaysAvailable";
}

internal static class LaRataCardCatalog
{
    // Boss IDs 0..24 are the original serialized/history identities. Append, never reorder.
    internal static IReadOnlyList<LaRataCardEntry> Entries { get; } = Array.AsReadOnly<LaRataCardEntry>([
        Boss<RoyalTax>(0, "royal_tax"),
        Boss<ScepterStorm>(1, "scepter_storm"),
        Boss<OffWithTheirHeads>(2, "off_with_their_heads"),
        Boss<SewerAlchemy>(3, "sewer_alchemy", active: false),
        Boss<BorrowedTomorrow>(4, "borrowed_tomorrow"),
        Boss<RoyalFlush>(5, "royal_flush", gambit: true),
        Boss<StolenAegis>(6, "stolen_aegis", gambit: true),
        Boss<BloodContract>(7, "blood_contract"),
        Boss<ExposedCrown>(8, "exposed_crown", gambit: true),
        Boss<EmeraldTempest>(9, "emerald_tempest"),
        Boss<GraveLevy>(10, "grave_levy"),
        Boss<FuneralProcession>(11, "funeral_procession"),
        Boss<BoneTreasury>(12, "bone_treasury"),
        Boss<CrownOfNight>(13, "crown_of_night"),
        Boss<RottingTribute>(14, "rotting_tribute"),
        Boss<StolenHour>(15, "stolen_hour", gambit: true),
        Boss<DrownedCourt>(16, "drowned_court"),
        Boss<GreenGuillotine>(17, "green_guillotine"),
        Boss<RattlingArsenal>(18, "rattling_arsenal"),
        Boss<RoyalPardon>(19, "royal_pardon", gambit: true),
        Boss<BlackTithe>(20, "black_tithe"),
        Boss<PlagueOfCrowns>(21, "plague_of_crowns", active: false),
        Boss<KneelBeforeTheCrown>(22, "kneel_before_the_crown"),
        Boss<SoulForeclosure>(23, "soul_foreclosure"),
        Boss<NightfallDecree>(24, "nightfall_decree", gambit: true),
        Player<Cleanse>(25, "cleanse"),
        Player<GraveSpark>(26, "grave_spark"),
        Player<UmbralStitch>(27, "umbral_stitch"),
        Player<SewerPact>(28, "sewer_pact"),
        Player<PhantomParade>(29, "phantom_parade"),
        Player<BorrowedBreath>(30, "borrowed_breath"),
        Player<CrownExchange>(31, "crown_exchange"),
        Player<MidnightRehearsal>(32, "midnight_rehearsal"),
        Player<EmeraldWard>(33, "emerald_ward"),
        Player<SecondShadow>(34, "second_shadow"),
        Player<OpenTheGates>(35, "open_the_gates"),
        Player<SerKevsThornBuster>(36, "ser_kevs_thorn_buster"),
        Player<GutterFeint>(37, "gutter_feint"),
        Player<RagpickersGuard>(38, "ragpickers_guard"),
        Player<CinderReceipt>(39, "cinder_receipt"),
        Player<VenomousEncore>(40, "venomous_encore"),
        Player<GraveyardShift>(41, "graveyard_shift"),
        Player<ScrapCoronation>(42, "scrap_coronation"),
        Player<ColdCircuit>(43, "cold_circuit"),
        Player<BorrowedBackbone>(44, "borrowed_backbone"),
        Player<SmugglersDividend>(45, "smugglers_dividend"),
        Player<CourtOfCinders>(46, "court_of_cinders"),
        Player<LordTibosReset>(47, "lord_tibos_reset"),
        Prize<RoyalScepterPrize>("scepter_storm"),
        Prize<RoyalAlchemyPrize>("sewer_alchemy"),
        Prize<RoyalAegisPrize>("stolen_aegis"),
        Prize<RoyalContractPrize>("blood_contract"),
        Prize<RoyalTempestPrize>("emerald_tempest"),
        Prize<RoyalTithePrize>("black_tithe"),
        Prize<RoyalPlaguePrize>("plague_of_crowns"),
        Prize<RoyalKneelPrize>("kneel_before_the_crown"),
        Prize<RoyalForeclosurePrize>("soul_foreclosure"),
        Prize<RoyalNightfallPrize>("nightfall_decree"),
        ClassPlayer<WrathOfTheBigO>("wrath_of_the_big_o"),
        ClassPlayer<TheKikilyst>("the_kikilyst")
    ]);

    internal static CardModel[] Neutrals => Entries.Where(e => e.BoosterEligibility == LaRataBoosterEligibility.Neutral).Select(e => e.Model).ToArray();
    internal static CardModel[] BossPrizes => Entries.Where(e => e.BoosterEligibility == LaRataBoosterEligibility.RoyalPrize).Select(e => e.Model).ToArray();
    internal static IReadOnlyList<CardModel> BossCards => Entries.Where(e => e.BossId.HasValue).OrderBy(e => e.BossId).Select(e => e.Model).ToArray();
    internal static LaRataCardEntry GetBossEntry(int id) => Entries.Single(e => e.BossId == id);
    internal static CardModel GetBossModel(int id) => GetBossEntry(id).Model;
    internal static int BossCost(int id) => GetBossModel(id).EnergyCost.Canonical;
    internal static bool BossIsAttack(int id) => GetBossModel(id).Type == CardType.Attack;
    internal static bool BossExhausts(int id) => GetBossModel(id).CanonicalKeywords.Contains(CardKeyword.Exhaust);
    internal static bool BossIsPersistent(int id) => id >= 25 && GetBossModel(id).Type == CardType.Power;

    private static LaRataCardEntry Boss<T>(int id, string key, bool active = true, bool gambit = false) where T : CardModel =>
        new(() => ModelDb.Card<T>(), key, LaRataCardRole.BossExclusive, key, id, active, gambit);
    private static LaRataCardEntry Player<T>(int id, string key) where T : CardModel =>
        new(() => ModelDb.Card<T>(), key, LaRataCardRole.Player, key, id, true,
            BoosterEligibility: LaRataBoosterEligibility.Neutral);
    private static LaRataCardEntry Prize<T>(string key) where T : CardModel =>
        new(() => ModelDb.Card<T>(), key, LaRataCardRole.PlayableBossVariant, key, null, false,
            BoosterEligibility: LaRataBoosterEligibility.RoyalPrize);
    // Native Pool registration controls class rewards and the matching class booster.
    // A player role alone must never opt a card into neutral bonuses or boss execution.
    private static LaRataCardEntry ClassPlayer<T>(string key) where T : CardModel =>
        new(() => ModelDb.Card<T>(), key, LaRataCardRole.Player, key, null, false,
            BoosterEligibility: LaRataBoosterEligibility.ClassPool);
}
