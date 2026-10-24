using System.Runtime.CompilerServices;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

internal static class FinalRewardChecks
{
    private static void Require(bool value, string why) { if (!value) throw new Exception(why); }
    private static bool NoDesktopModDiscovery(ref Type[] __result) { __result = []; return false; }
    private static bool NoDesktopArguments(ref string[] __result) { __result = []; return false; }
    private static bool NoEditor(ref bool __result) { __result = false; return false; }
    private static bool NoGodotConsole() => false;
    private static bool NoControllerButtons(ref IReadOnlyList<CardRewardAlternative> __result) { __result = []; return false; }
    private static void Set(Player player, string property, object value) =>
        AccessTools.Field(typeof(Player), $"<{property}>k__BackingField").SetValue(player, value);

    private static Player MakePlayer(uint seed, bool statue)
    {
        // Avoid the desktop progress-save singleton in the native Player constructor.
        // Reward objects, RNG, odds, relic hooks and run state remain native.
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        AccessTools.Field(typeof(Player), "_runState").SetValue(player, NullRunState.Instance);
        var relics = new List<RelicModel>();
        AccessTools.Field(typeof(Player), "_relics").SetValue(player, relics);
        AccessTools.Field(typeof(Player), "_potionSlots").SetValue(player, new List<PotionModel?>());
        Set(player, nameof(Player.Character), ModelDb.Character<Ironclad>());
        Set(player, nameof(Player.NetId), (ulong)seed);
        Set(player, nameof(Player.Deck), new CardPile(PileType.Deck));
        Set(player, nameof(Player.UnlockState), UnlockState.all);
        Set(player, nameof(Player.IsActiveForHooks), true);
        var rng = new PlayerRngSet(seed);
        Set(player, nameof(Player.PlayerRng), rng);
        Set(player, nameof(Player.PlayerOdds), new PlayerOddsSet(rng));
        if (statue)
        {
            var relic = ModelDb.Relic<WhiteBeastStatue>().ToMutable();
            relic.Owner = player;
            relics.Add(relic);
        }
        return player;
    }

    internal static void Run()
    {
        var harmony = new Harmony("LaRata.FinalRewardChecks");
        harmony.Patch(AccessTools.Method(typeof(Godot.OS), nameof(Godot.OS.GetCmdlineArgs)),
            prefix: new(typeof(FinalRewardChecks), nameof(NoDesktopArguments)));
        harmony.Patch(AccessTools.Method(typeof(Godot.OS), nameof(Godot.OS.HasFeature)),
            prefix: new(typeof(FinalRewardChecks), nameof(NoEditor)));
        harmony.Patch(AccessTools.Method(typeof(MegaCrit.Sts2.Core.Logging.ConsoleLogPrinter), "Print"),
            prefix: new(typeof(FinalRewardChecks), nameof(NoGodotConsole)));
        harmony.Patch(AccessTools.Method(typeof(CardRewardAlternative), nameof(CardRewardAlternative.Generate)),
            prefix: new(typeof(FinalRewardChecks), nameof(NoControllerButtons)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(MegaCrit.Sts2.Core.Helpers.ReflectionHelper), "ModTypes"),
            prefix: new(typeof(FinalRewardChecks), nameof(NoDesktopModDiscovery)));
        harmony.CreateClassProcessor(typeof(FinalBossRewardsPatch)).Patch();
        harmony.CreateClassProcessor(typeof(BarricadeRarityPatch)).Patch();
        try
        {
            var barricade = ModelDb.Card<Barricade>();
            Require(barricade.Rarity == CardRarity.Uncommon && barricade.ToMutable().Rarity == CardRarity.Uncommon,
                "Barricade rarity differs between canonical and mutable model");
            Require(ModelDb.Card<Impervious>().Rarity == CardRarity.Rare, "Other native rare cards changed");
            var upgraded = barricade.ToMutable();
            upgraded.UpgradeInternal();
            upgraded.FinalizeUpgradeInternal();
            Require(upgraded.Rarity == CardRarity.Uncommon && upgraded.EnergyCost.Canonical == 3 &&
                upgraded.EnergyCost.GetWithModifiers(CostModifiers.None) == 2, "Barricade upgrade changed");

            foreach (int party in new[] { 1, 2, 3, 4 })
            foreach (bool duo in new[] { false, true })
            foreach (bool statue in new[] { false, true })
            {
                var players = Enumerable.Range(1, party).Select(i => MakePlayer((uint)i, statue)).ToArray();
                var run = RunState.CreateForTest(players: players, seed: "FINALREWARDS");
                run.CurrentActIndex = 2;
                run.Act.SetBossEncounter(ModelDb.Encounter<QueenBoss>());
                var room = new CombatRoom(ModelDb.Encounter<QueenBoss>().ToMutable(), run);
                if (duo)
                    EncounterDirector.Rules.Add(room.Encounter, new EncounterRules(new(0, 0, 0, []))
                    { BossDuo = true, BossSource = ModelDb.Encounter<VantomBoss>() });
                foreach (var player in players)
                {
                    var savedRng = player.PlayerRng.ToSerializable();
                    var savedOdds = player.PlayerOdds.ToSerializable();
                    var extra = new GoldReward(7, player);
                    room.AddExtraReward(player, extra);
                    string Generate()
                    {
                        var rewards = new RewardsSet(player).WithRewardsFromRoom(room);
                        rewards.GenerateWithoutOffering().GetAwaiter().GetResult();
                        int generatedCounter = player.PlayerRng.Rewards.Counter;
                        rewards.GenerateWithoutOffering().GetAwaiter().GetResult();
                        Require(player.PlayerRng.Rewards.Counter == generatedCounter,
                            "Offering an already-generated reward set consumes additional RNG");
                        Require(rewards.Rewards.All(r => ReferenceEquals(r.Player, player)), "Rewards belong to a different player");
                        int count = duo ? 2 : 1;
                        Require(ReferenceEquals(rewards.Room, room), "Rewards lost the real terminal combat room");
                        Require(rewards.Rewards.Count(r => ReferenceEquals(r, extra)) == 1, "Extra reward missing or duplicated");
                        var gold = rewards.Rewards.OfType<GoldReward>().Where(r => !ReferenceEquals(r, extra)).ToArray();
                        Require(gold.Length == count && gold.Sum(g => g.Amount) == 100 * count, "Boss gold is missing or duplicated");
                        var cards = rewards.Rewards.OfType<CardReward>().ToArray();
                        Require(cards.Length == count, "Wrong number of independent card choices");
                        foreach (var card in cards)
                        {
                            Require(card.Cards.Count() == 3 && card.Cards.All(c => c.Rarity == CardRarity.Rare),
                                "Native boss card population did not produce three rare options");
                            var options = (CardCreationOptions)AccessTools.PropertyGetter(typeof(CardReward), "Options").Invoke(card, null)!;
                            Require(options.Source == CardCreationSource.Encounter && options.RarityOdds == CardRarityOddsType.BossEncounter &&
                                (int)AccessTools.PropertyGetter(typeof(CardReward), "OptionCount").Invoke(card, null)! == 3,
                                "Native three-card boss reward options changed");
                        }
                        int potions = rewards.Rewards.OfType<PotionReward>().Count();
                        Require(!statue || potions == count, "Native White Beast Statue hook misses a boss potion");
                        Require(potions <= count, "Too many potion rolls");
                        var identities = string.Join(',', cards.SelectMany(c => c.Cards).Select(c => c.Id.Entry)) +
                            "/" + string.Join(',', rewards.Rewards.OfType<PotionReward>().Select(p => p.Potion!.Id.Entry));
                        return $"{gold.Sum(g => g.Amount)}/{cards.Length}/{potions}/{identities}/{player.PlayerRng.Rewards.Counter}/{player.PlayerOdds.PotionReward.CurrentValue}";
                    }
                    var before = Generate();
                    var restored = PlayerRngSet.FromSerializable(savedRng);
                    Set(player, nameof(Player.PlayerRng), restored);
                    Set(player, nameof(Player.PlayerOdds), PlayerOddsSet.FromSerializable(savedOdds, restored));
                    Require(Generate() == before, "Native saved RNG/odds replay differs");
                }
                // The bonus finale still uses the game's empty final-boss reward path.
                var king = new CombatRoom(ModelDb.Encounter<RatKingEncounter>().ToMutable(), run);
                Require(new RewardsSet(run.Players[0]).WithRewardsFromRoom(king).Rewards.Count == 0,
                    "Rat King unexpectedly received another reward screen");
            }
        }
        finally { harmony.UnpatchAll(harmony.Id); }
        Console.WriteLine("PASS: Barricade is Uncommon with its native upgrade; final boss fully populates one/two native reward batches for 1-4 player fixtures, three rare options per card reward, native Statue potion hooks, player ownership, one copy of extras, idempotent population, terminal room association and saved RNG/odds replay including card/potion IDs. Rat King unchanged. Desktop logging, mod discovery and controller buttons stubbed. No live UI/network test.");
    }
}
