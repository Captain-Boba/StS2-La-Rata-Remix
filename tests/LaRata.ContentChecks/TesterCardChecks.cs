using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

internal static class TesterCardChecks
{
    private static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }
    private static bool Active(ref bool __result) { __result = false; return false; }
    private static void Power<T>(Creature target, int amount) where T : PowerModel, new()
    { if (amount != 0) ModelDb.Power<T>().ToMutable().ApplyInternal(target, amount, true); }
    internal static CardPlay Play(CardModel card, Creature? target = null) => new()
    { Card = card, Target = target, ResultPile = PileType.Exhaust, Resources = default, IsAutoPlay = false, PlayIndex = 0, PlayCount = 1 };
    private static (RunState run, Player[] owners, Creature[] enemies) Setup(int players)
    {
        var owners = Enumerable.Range(0, players).Select(i => NativeConsoleFixture.Player((ulong)(401 + i))).ToArray();
        var run = RunState.CreateForTest(players: owners, seed: "TESTERCARDS");
        var combat = new CombatState(ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable(), run);
        foreach (var owner in owners)
        {
            var body = new Creature(owner, 50, 50) { CombatState = combat };
            NativeConsoleFixture.Set(owner, nameof(owner.Creature), body);
            owner.ResetCombatState(); combat.AddCreature(body);
        }
        var enemies = Enumerable.Range(0, 3).Select(i => new Creature(ModelDb.Monster<TwoTailedRat>().ToMutable(), CombatSide.Enemy, i.ToString()) { CombatState = combat }).ToArray();
        foreach (var enemy in enemies) combat.AddCreature(enemy);
        return (run, owners, enemies);
    }
    private static void Resolve(CardModel card, Creature? target = null)
    {
        card.Owner.PlayerCombatState!.PlayPile.AddInternal(card);
        ((Task)AccessTools.Method(card.GetType(), "OnPlay").Invoke(card, [new BlockingPlayerChoiceContext(), Play(card, target)])!).GetAwaiter().GetResult();
    }
    internal static void Run()
    {
        var harmony = new Harmony("LaRata.TesterCards");
        NativeConsoleFixture.PatchDesktop(harmony);
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsOverOrEnding)), prefix: new(typeof(TesterCardChecks), nameof(Active)));
        try
        {
            var big = ModelDb.Card<WrathOfTheBigO>(); var kiki = ModelDb.Card<TheKikilyst>();
            // Constructors already execute BaseLib CustomContentDictionary.AddModel.
            // These are the native concatenated pools, with no test-side pool insertion.
            var iron = ModelDb.CardPool<IroncladCardPool>().AllCards.ToArray();
            var silent = ModelDb.CardPool<SilentCardPool>().AllCards.ToArray();
            Require(iron.Count(c => c.Id == big.Id) == 1 && iron.All(c => c.Id != kiki.Id) &&
                silent.Count(c => c.Id == kiki.Id) == 1 && silent.All(c => c.Id != big.Id), "Native class pool registration missing/duplicated/leaking");
            foreach (var card in new ComboCard[] { big, kiki })
            {
                Require(card.Type == CardType.Skill && card.Rarity == CardRarity.Uncommon && card.Keywords.Contains(CardKeyword.Exhaust), "Tester classification differs");
                Require(card.CreateCustomFrameMaterial == null && card.CreateCustomBannerMaterial == null, "Tester overrides native class presentation");
                var entry = LaRataCardCatalog.Entries.Single(e => e.ModelId == card.Id);
                Require(entry.BossId == null && !entry.BossEligible && !LaRataCardCatalog.Neutrals.Any(c => c.Id == card.Id), "Tester leaked into boss or neutral pool");
            }
            int cases = 0;
            foreach (int count in new[] { 1, 2, 3, 4 })
            foreach (bool upgraded in new[] { false, true })
            {
                foreach (int strength in new[] { -3, 0, 4 })
                foreach (bool artifact in new[] { false, true })
                {
                    var (run, owners, _) = Setup(count);
                    var owner = owners[^1];
                    foreach (var partner in owners.Where(p => p != owner)) Power<StrengthPower>(partner.Creature, 7);
                    Power<StrengthPower>(owner.Creature, strength);
                    if (artifact) Power<ArtifactPower>(owner.Creature, 1);
                    var card = run.CreateCard<WrathOfTheBigO>(owner);
                    if (upgraded) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
                    int rng = run.Rng.CombatCardSelection.Counter;
                    Resolve(card);
                    Require(owner.Creature.GetPowerAmount<StrengthPower>() == (strength < 0 && artifact ? strength : strength * 2), "Signed Strength or Artifact behavior differs");
                    Require(owner.Creature.GetPowerAmount<ArtifactPower>() == (artifact && strength >= 0 ? 1 : 0), "Strength consumed wrong Artifact");
                    Require(owners.Where(p => p != owner).All(p => p.Creature.GetPowerAmount<StrengthPower>() == 7) && run.Rng.CombatCardSelection.Counter == rng, "Strength affected partner or consumed selection RNG");
                    cases++;
                }
                foreach (int poison in new[] { 0, 1, 5, 40 })
                foreach (bool primaryArtifact in new[] { false, true })
                {
                    var (run, owners, enemies) = Setup(count); var owner = owners[^1];
                    Power<PoisonPower>(enemies[0], poison);
                    if (primaryArtifact) Power<ArtifactPower>(enemies[0], 1);
                    Power<ArtifactPower>(enemies[2], 1);
                    var card = run.CreateCard<TheKikilyst>(owner);
                    if (upgraded) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
                    int rng = run.Rng.CombatCardSelection.Counter;
                    Resolve(card, enemies[0]);
                    int result = primaryArtifact ? poison : 2 * poison;
                    int spread = (int)decimal.Floor(result * (upgraded ? .5m : .25m));
                    Require(enemies[0].GetPowerAmount<PoisonPower>() == result && enemies[1].GetPowerAmount<PoisonPower>() == spread && enemies[2].GetPowerAmount<PoisonPower>() == 0, "Poison doubling/floor/distribution/Artifact differs");
                    Require(enemies[0].GetPowerAmount<ArtifactPower>() == (primaryArtifact && poison == 0 ? 1 : 0) && enemies[2].GetPowerAmount<ArtifactPower>() == (spread == 0 ? 1 : 0), "Zero spread consumed Artifact or wrong target consumed");
                    Require(owners.All(p => p.Creature.GetPowerAmount<PoisonPower>() == 0) && run.Rng.CombatCardSelection.Counter == rng, "Poison leaked onto players or used selection RNG");
                    cases++;
                }
                {
                    var (run, owners, enemies) = Setup(count);
                    Power<PoisonPower>(enemies[0], 40);
                    AccessTools.PropertySetter(typeof(Creature), nameof(Creature.CurrentHp)).Invoke(enemies[0], [0]);
                    Resolve(run.CreateCard<TheKikilyst>(owners[^1]), enemies[0]);
                    Require(enemies[0].GetPowerAmount<PoisonPower>() == 40 && enemies.Skip(1).All(c => c.GetPowerAmount<PoisonPower>() == 0), "Dead primary target resolved poison");
                    cases++;
                }
            }
            Console.WriteLine($"PASS: native BaseLib constructor registration into class pools, class frame/banner fallbacks; {cases} native OnPlay/PowerCmd cases across 1/2/3/4 modeled owners and upgrades, signed Strength, poison floor, Artifact, zero/dead target, partner isolation and unchanged selection RNG. Direct OnPlay and substituted combat guard; no full play/exhaust lifecycle, engine, save or real network.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }
}
