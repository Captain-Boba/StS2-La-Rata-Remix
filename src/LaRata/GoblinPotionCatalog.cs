using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Random;

namespace LaRata;

// Audited against v0.107.1. Use the real potion effects, including native card
// choices, forging, pets and orbs. Do not automatically execute third-party OnUse.
internal static class GoblinPotionCatalog
{
    internal static readonly Type[] Supported =
    [
        typeof(Ashwater), typeof(AttackPotion), typeof(BeetleJuice), typeof(BlessingOfTheForge),
        typeof(BlockPotion), typeof(BloodPotion), typeof(BoneBrew), typeof(BottledPotential),
        typeof(Clarity), typeof(ColorlessPotion), typeof(CosmicConcoction), typeof(CunningPotion),
        typeof(CureAll), typeof(DexterityPotion), typeof(DistilledChaos), typeof(DropletOfPrecognition),
        typeof(Duplicator), typeof(EnergyPotion), typeof(EntropicBrew), typeof(EssenceOfDarkness),
        typeof(FirePotion), typeof(FlexPotion), typeof(FocusPotion), typeof(Fortifier),
        typeof(FruitJuice), typeof(FyshOil), typeof(GamblersBrew), typeof(GhostInAJar),
        typeof(GigantificationPotion), typeof(GlowwaterPotion), typeof(HeartOfIron), typeof(KingsCourage),
        typeof(LiquidBronze), typeof(LiquidMemories), typeof(LuckyTonic), typeof(MazalethsGift),
        typeof(OrobicAcid), typeof(PoisonPotion), typeof(PotionOfCapacity), typeof(PotionOfDoom),
        typeof(PotionShapedRock), typeof(PotOfGhouls), typeof(PowderedDemise), typeof(PowerPotion),
        typeof(RadiantTincture), typeof(RegenPotion), typeof(ShipInABottle), typeof(SkillPotion),
        typeof(SneckoOil), typeof(SoldiersStew), typeof(SpeedPotion), typeof(StableSerum),
        typeof(StarPotion), typeof(StrengthPotion), typeof(SwiftPotion), typeof(TouchOfInsanity),
        typeof(VulnerablePotion), typeof(WeakPotion)
    ];

    // These do not implement an individual splash on the recipient. The revival
    // potion is explicitly excluded; the deprecated model has no effect.
    internal static readonly Type[] Excluded =
    [
        typeof(FairyInABottle), typeof(DeprecatedPotion), typeof(ExplosiveAmpoule),
        typeof(PotionOfBinding), typeof(ShacklingPotion), typeof(FoulPotion)
    ];

    internal static bool CanSplash(Type type, Player player)
    {
        var state = player.PlayerCombatState;
        if (state == null || !player.Creature.IsAlive) return false;
        if (type == typeof(EntropicBrew)) return player.HasOpenPotionSlots;
        if (type == typeof(EssenceOfDarkness) || type == typeof(FocusPotion)) return state.OrbQueue.Capacity > 0;
        if (type == typeof(Fortifier)) return player.Creature.Block > 0;
        if (type == typeof(LiquidMemories)) return !state.DiscardPile.IsEmpty;
        if (type == typeof(DropletOfPrecognition)) return !state.DrawPile.IsEmpty;
        if (type == typeof(Ashwater) || type == typeof(GamblersBrew)) return !state.Hand.IsEmpty;
        if (type == typeof(BlessingOfTheForge)) return state.Hand.UpgradableCardCount > 0;
        if (type == typeof(SoldiersStew)) return state.AllCards.Any(c => c.Tags.Contains(CardTag.Strike));
        if (type == typeof(TouchOfInsanity)) return state.Hand.Cards.Any(c => c.CostsEnergyOrStars(false) || c.CostsEnergyOrStars(true));
        return true;
    }

    internal static PotionModel[] Select(IReadOnlyList<Player> players, Rng rng)
    {
        // Caller supplies native NetId order. Freeze choices before presentation
        // or potion effects; every recipient gets a different eligible bottle.
        var used = new HashSet<Type>();
        return players.Select(player =>
        {
            var available = Supported.Where(t => !used.Contains(t) && CanSplash(t, player)).ToArray();
            var type = available[rng.NextInt(available.Length)];
            used.Add(type);
            return ModelDb.GetById<PotionModel>(ModelDb.GetId(type));
        }).ToArray();
    }
}
