using System.Text.Json;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

internal static class RoyalPotionRampChecks
{
    private static int checks;
    private static readonly PlayerChoiceContext Context = new ThrowingPlayerChoiceContext();
    private static void Check(bool value, string reason) { checks++; if (!value) throw new Exception(reason); }
    private static (RunState run, CombatState combat, Player a, Player b) Setup(bool royal = false)
    {
        var a = NativeConsoleFixture.Player(901); var b = NativeConsoleFixture.Player(902);
        var run = RunState.CreateForTest(players: [a,b], seed: "ROYALPOTIONS");
        NativeConsoleFixture.Set(RunManager.Instance, "State", run);
        NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.AscensionManager), new MegaCrit.Sts2.Core.Entities.Ascension.AscensionManager(0));
        var encounter = royal ? (EncounterModel)ModelDb.Encounter<RatKingEncounter>().ToMutable() : ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable();
        var combat = new CombatState(encounter, run);
        AccessTools.Field(typeof(CombatManager), "_state").SetValue(CombatManager.Instance, combat);
        NativeConsoleFixture.Set(CombatManager.Instance, nameof(CombatManager.IsInProgress), true);
        foreach (var p in new[] { a,b })
        {
            var body = new Creature(p, 5000, 5000) { CombatState = combat };
            NativeConsoleFixture.Set(p, nameof(p.Creature), body); p.ResetCombatState(); combat.AddCreature(body);
            p.PlayerCombatState!.Energy = 3;
        }
        Enemy(combat, ModelDb.Monster<TwoTailedRat>().ToMutable());
        return (run,combat,a,b);
    }
    private static Creature Enemy(CombatState combat, MonsterModel model)
    {
        var enemy = new Creature(model, CombatSide.Enemy, model.Id.Entry) { CombatState = combat };
        combat.AddCreature(enemy); return enemy;
    }
    private static T Card<T>(CombatState combat, Player p) where T:CardModel,new()
    { var c = combat.CreateCard<T>(p); p.PlayerCombatState!.Hand.AddInternal(c); return c; }
    private static void Use<T>(Player p, Creature? target = null, CardModel? selected = null) where T:RoyalPotion,new()
    {
        var potion = (T)ModelDb.Potion<T>().ToMutable(); potion.Owner = p;
        var selector = new TestCardSelector(); if (selected != null) selector.PrepareToSelect(new[] { selected });
        using var scope = CardSelectCmd.PushSelector(selector);
        ((Task)AccessTools.Method(typeof(T), "OnUse").Invoke(potion, [Context,target])!).GetAwaiter().GetResult();
    }
    private static void Potions()
    {
        foreach (bool upgraded in new[] { false,true })
        foreach (bool discounted in new[] { false,true })
        {
            var (_,combat,a,b) = Setup();
            var fuel = Card<WrathOfTheBigO>(combat,a); Card<StrikeIronclad>(combat,a);
            if (upgraded) { fuel.UpgradeInternal(); fuel.FinalizeUpgradeInternal(); }
            if (discounted) fuel.EnergyCost.SetThisTurn(0);
            var foreign = Card<Bash>(combat,b);
            Use<Ashbrand>(a,selected:fuel);
            Check(fuel.Pile?.Type == PileType.Exhaust && a.PlayerCombatState!.Energy == (upgraded ? 5 : 6), "Ashbrand base/upgrade/discount energy or exhaust wrong");
            Check(b.PlayerCombatState!.Energy == 3 && foreign.Pile?.Type == PileType.Hand, "Ashbrand changed partner");
        }
        {
            var (_,combat,a,b) = Setup(); var x = Card<Whirlwind>(combat,a);
            Check(!Ashbrand.Eligible(x), "X card eligible");
            var starX = typeof(CardModel).Assembly.GetTypes().Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(CardModel)))
                .Select(t => ModelDb.GetById<CardModel>(ModelDb.GetId(t))).First(c => c.HasStarCostX);
            Check(!Ashbrand.Eligible(starX), "Star-X card eligible");
            Use<Ashbrand>(a); Check(x.Pile?.Type == PileType.Hand && a.PlayerCombatState!.Energy == 3, "No valid fuel changed state");
            var zero = Card<Anger>(combat,a); Use<Ashbrand>(a,selected:zero);
            Check(zero.Pile?.Type == PileType.Exhaust && a.PlayerCombatState!.Energy == 3, "Zero-cost fuel wrong");
        }
        {
            var (_,combat,a,b) = Setup(); var enemy = combat.Enemies.First();
            enemy.GainBlockInternal(137); b.Creature.GainBlockInternal(11); int hp = enemy.CurrentHp;
            Use<Rustwater>(a,enemy);
            Check(enemy.Block == 0 && enemy.CurrentHp == hp && enemy.GetPowerAmount<VulnerablePower>() == 0, "Rustwater must remove only Block");
            Check(b.Creature.Block == 11, "Rustwater changed partner Block");
            Use<Rustwater>(a,enemy); Check(enemy.Block == 0 && enemy.CurrentHp == hp, "Zero-Block Rustwater wrong");
        }
        {
            var (run,combat,a,b) = Setup();
            var original = run.CreateCard<WrathOfTheBigO>(a); a.Deck.AddInternal(original);
            original.UpgradeInternal(); original.FinalizeUpgradeInternal();
            var chosen = combat.CloneCard(original); a.PlayerCombatState!.Hand.AddInternal(chosen);
            var other = Card<WrathOfTheBigO>(combat,a); var partner = Card<WrathOfTheBigO>(combat,b);
            chosen.AddKeyword(CardKeyword.Retain);
            Use<CrownLacquer>(a,selected:chosen);
            Check(!chosen.Keywords.Contains(CardKeyword.Exhaust) && chosen.Keywords.Contains(CardKeyword.Retain) && chosen.CurrentUpgradeLevel == 1, "Crown Lacquer changed wrong keywords or upgrade");
            Check(original.Keywords.Contains(CardKeyword.Exhaust) && other.Keywords.Contains(CardKeyword.Exhaust) && partner.Keywords.Contains(CardKeyword.Exhaust), "Crown Lacquer changed deck or unselected cards");
            CardPileCmd.Add(chosen,PileType.Discard,CardPilePosition.Bottom,null,true).GetAwaiter().GetResult();
            CardPileCmd.Add(chosen,PileType.Hand,CardPilePosition.Bottom,null,true).GetAwaiter().GetResult();
            Check(!chosen.Keywords.Contains(CardKeyword.Exhaust) && combat.CloneCard(original).Keywords.Contains(CardKeyword.Exhaust), "Lacquer failed combat-instance lifetime");
            Check(!CrownLacquer.Eligible(chosen), "Already lacquered card still eligible");
        }
        var models = new RoyalPotion[] { ModelDb.Potion<Ashbrand>(),ModelDb.Potion<Rustwater>(),ModelDb.Potion<CrownLacquer>() };
        var pool = ModelDb.PotionPool<SharedPotionPool>().AllPotions.ToArray();
        foreach (var potion in models)
        {
            Check(pool.Count(p => p.Id == potion.Id) == 1, "Missing/duplicate shared potion: " + potion.Id);
            var saved = potion.ToMutable().ToSerializable(0);
            var loaded = PotionModel.FromSerializable(JsonSerializer.Deserialize(JsonSerializer.Serialize(saved,JsonSerializationUtility.GetTypeInfo<SerializablePotion>()),JsonSerializationUtility.GetTypeInfo<SerializablePotion>())!);
            Check(loaded.Id == potion.Id, "Native potion JSON identity changed");
        }
    }
    private static void Ramp()
    {
        var (run,combat,a,_) = Setup(true);
        var models = new MonsterModel[] { ModelDb.Monster<RatKing>(), ModelDb.Monster<CrownBulwark>(), ModelDb.Monster<CrownHeadsman>(), ModelDb.Monster<CrownArmorer>(), ModelDb.Monster<CrownHerald>() };
        var enemies = models.Select(m => Enemy(combat,m.ToMutable())).ToArray();
        foreach (var dealer in enemies)
        {
            ModelDb.Power<StrengthPower>().ToMutable().ApplyInternal(dealer,5,true);
            ModelDb.Power<VigorPower>().ToMutable().ApplyInternal(dealer,5,true);
            for (int round=1; round<=9; round++)
            {
                combat.RoundNumber=round;
                int expected = Math.Min(4+round-1,10)*3;
                int preview = RoyalForecastAttackIntent.Forecast(null,dealer,a.Creature,20,-1,true);
                Check(preview==expected,$"Forecast {dealer.Monster!.Id} round {round}: {preview} != {expected}");
                int before=a.Creature.CurrentHp;
                CreatureCmd.Damage(Context,a.Creature,20,ValueProp.Move,dealer).GetAwaiter().GetResult();
                Check(before-a.Creature.CurrentHp==preview,"Native damage differs from intent");
            }
        }
        combat.RoundNumber=1;
        var king=enemies[0];
        int hp=a.Creature.CurrentHp;
        CreatureCmd.Damage(Context,a.Creature,20,ValueProp.Unpowered|ValueProp.Move,king).GetAwaiter().GetResult();
        Check(hp-a.Creature.CurrentHp==20,"Unpowered damage scaled");
        Check(RoyalForecastAttackIntent.Forecast(null,combat.Enemies.First(),a.Creature,20,-1,true)==20,"Other enemy scaled");
        ModelDb.Power<WeakPower>().ToMutable().ApplyInternal(king,1,true);
        ModelDb.Power<VulnerablePower>().ToMutable().ApplyInternal(a.Creature,1,true);
        int calculated=RoyalForecastAttackIntent.Forecast(null,king,a.Creature,20,-1,true);
        hp=a.Creature.CurrentHp;
        CreatureCmd.Damage(Context,a.Creature,20,ValueProp.Move,king).GetAwaiter().GetResult();
        Check(calculated==13 && hp-a.Creature.CurrentHp==13,"Weak/Vulnerable/rounding pipeline differs");
        ModelDb.Power<IntangiblePower>().ToMutable().ApplyInternal(a.Creature,1,true);
        Check(RoyalForecastAttackIntent.Forecast(null,king,a.Creature,20,-1,true)==1,"Native damage cap scaled below one");
        var (_,normal,p,_) = Setup();
        var guard=Enemy(normal,ModelDb.Monster<CrownHeadsman>().ToMutable());
        Check(RoyalForecastAttackIntent.Forecast(null,guard,p.Creature,20,-1,true)==20,"Guard outside La Rata encounter scaled");
    }
    internal static void Run()
    {
        var harmony=new Harmony("LaRata.ContentChecks.RoyalPotionRamp"); NativeConsoleFixture.PatchDesktop(harmony);
        var oldRun=RunManager.Instance.DebugOnlyGetState(); var oldAscension=RunManager.Instance.AscensionManager;
        var state=AccessTools.Field(typeof(CombatManager),"_state"); var oldState=state.GetValue(CombatManager.Instance);
        bool oldProgress=CombatManager.Instance.IsInProgress;
        try { Potions(); Ramp(); RoyalIntentWaveChecks.Run(); Console.WriteLine($"PASS: {checks} potion/ramp checks using native selection, commands, damage hooks and potion JSON. Predetermined test selector; no game, UI or live multiplayer test."); }
        finally { NativeConsoleFixture.Set(RunManager.Instance,"State",oldRun); NativeConsoleFixture.Set(RunManager.Instance,nameof(RunManager.AscensionManager),oldAscension); state.SetValue(CombatManager.Instance,oldState); NativeConsoleFixture.Set(CombatManager.Instance,nameof(CombatManager.IsInProgress),oldProgress); harmony.UnpatchAll(harmony.Id); }
    }
}
