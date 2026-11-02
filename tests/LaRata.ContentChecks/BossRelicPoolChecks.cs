using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

internal static class BossRelicPoolChecks
{
    private static readonly PlayerChoiceContext Context = new ThrowingPlayerChoiceContext();
    private static readonly int[] NewIds = [8, 10, 18, 19, 20, 21, 22, 23, 24];
    private static void Check(bool value, string why) { if (!value) throw new Exception(why); }
    private static void Await(Task task) => task.GetAwaiter().GetResult();
    private static bool Active(ref bool __result) { __result = false; return false; }
    private static int Mask(params int[] ids) => ids.Aggregate(0, (mask, i) => mask | (1 << i));
    private static string NativeRoot(string language) => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.research/native-reference-0.107.1-20261025/localization/", language));

    private static (CombatState Combat, RatKing King, Creature[] Targets) Setup(int count, int mask)
    {
        var players = Enumerable.Range(0, count).Select(i => NativeConsoleFixture.Player((ulong)(2201 + i))).ToArray();
        var run = RunState.CreateForTest(players: players, seed: "REGALIA25"); run.CurrentActIndex = 2;
        NativeConsoleFixture.Set(RunManager.Instance, "State", run);
        var combat = new CombatState(ModelDb.Encounter<RatKingEncounter>().ToMutable(), run);
        AccessTools.Field(typeof(CombatManager), "_state").SetValue(CombatManager.Instance, combat);
        NativeConsoleFixture.Set(CombatManager.Instance, nameof(CombatManager.IsInProgress), true);
        foreach (var player in players)
        {
            NativeConsoleFixture.Set(player, nameof(player.Creature), new Creature(player, 50000, 50000) { CombatState = combat });
            player.ResetCombatState(); combat.AddCreature(player.Creature);
        }
        var king = (RatKing)ModelDb.Monster<RatKing>().ToMutable();
        var body = new Creature(king, CombatSide.Enemy, "king") { CombatState = combat }; combat.AddCreature(body);
        AccessTools.Field(typeof(RatKing), "regaliaMask").SetValue(king, (int?)mask);
        AccessTools.Field(typeof(RatKing), "deck").SetValue(king, new RoyalDeck(_ => 0));
        return (combat, king, players.Select(p => p.Creature).ToArray());
    }

    internal static void Run()
    {
        var harmony = new Harmony("LaRata.BossRelicPoolChecks"); NativeConsoleFixture.PatchDesktop(harmony);
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsOverOrEnding)), prefix: new(typeof(BossRelicPoolChecks), nameof(Active)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsEnding)), prefix: new(typeof(BossRelicPoolChecks), nameof(Active)));
        try
        {
            Catalog();
            using var localization = new NativeFixtureLocalization(NativeRoot("eng"));
            for (int players = 1; players <= 4; players++)
            {
                OpeningAndDamage(players);
                Check(Triggers(players) == Triggers(players), "Relic counters/command results differ on fresh replay");
                CombinedOffense(players);
            }
            foreach (int round in new[] { 1, 7 })
            {
                var (combat, king, _) = Setup(1, 0); combat.RoundNumber = round;
                RoyalFinaleChecks.CheckGuardIntents(combat, king, king.Creature);
            }
            Console.WriteLine("PASS corrected Guard fixture: six attack intents at rounds 1/7 use GetTotalDamage, native ramp and repeats; pending +5 becomes applied +5 without double counting. No product forecast change.");
            Console.WriteLine("PASS new regalia: 1-4 modeled players, native startup/Artifact, actual multihit Vigor consumption and ramp-aware attack forecast, Fan/Kunai/Shuriken/Pocketwatch/Helmet/Bandages/Sai triggers and caps; no damage/block/power command sinks replaced.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
        RoyalEnergyChecks.Run();
        RegaliaWaveChecks.Run();
        Console.WriteLine("PASS focused boss relic pool. Explicit hook/card triggers and native commands; no complete rendered boss-hand loop, live clients, save/load or game start.");
    }

    private static void Catalog()
    {
        Type[] expected = [typeof(Vajra), typeof(OddlySmoothStone), typeof(Anchor), typeof(BronzeScales), typeof(HornCleat), typeof(CaptainsWheel), typeof(Orichalcum), typeof(MercuryHourglass), typeof(Akabeko), typeof(BagOfPreparation), typeof(RedMask), typeof(BagOfMarbles), typeof(TungstenRod), typeof(SelfFormingClay), typeof(LetterOpener), typeof(ArtOfWar), typeof(CentennialPuzzle), typeof(Nunchaku), typeof(OrnamentalFan), typeof(Kunai), typeof(Shuriken), typeof(ToughBandages), typeof(Pocketwatch), typeof(IntimidatingHelmet), typeof(Sai)];
        Check(RoyalRegaliaPower.Relics.Select(r => r.GetType()).SequenceEqual(expected) && RoyalRegaliaPower.EnglishEffects.Length == 25, "Relic pool/retained index mismatch");
        Check(RoyalRegaliaPower.Relics.All(r => r is not Lantern and not HappyFlower), "Removed energy relic still in boss pool");
        int seen = 0;
        for (uint seed = 0; seed < 1024; seed++)
        {
            var ar = new Rng(seed); var br = new Rng(seed);
            int a = RoyalRegaliaPower.Choose(ar), b = RoyalRegaliaPower.Choose(br); seen |= a;
            Check(a == b && ar.Counter == br.Counter && a >= 0 && (a >> 25) == 0 && System.Numerics.BitOperations.PopCount((uint)a) == 10, "Mask/replay/ten distinct relics");
        }
        Check(seen == (1 << 25) - 1, "Some relics unreachable");
        foreach (string language in new[] { "eng", "deu", "zhs" })
        {
            using var localization = new NativeFixtureLocalization(NativeRoot(language), language);
            var effects = RoyalRegaliaPower.Effects;
            Check(effects.Length == 25 && effects.All(s => !s.Contains("LARATA-") && !s.Contains('{')), "Missing/unformatted effect localization");
            foreach (int i in NewIds) Check(effects[i].StartsWith(RoyalRegaliaPower.Relics[i].Title.GetFormattedText() + ":") || language == "zhs" && effects[i].StartsWith(RoyalRegaliaPower.Relics[i].Title.GetFormattedText() + "："), "Native relic title mismatch " + language + "/" + i);
            Check(RoyalRegaliaPower.IntentText(NewIds).GetFormattedText().Contains(effects[8]), "Native intent localization omitted effects");
            var (_, king, _) = Setup(1, Mask(NewIds));
            var power = (RoyalRegaliaPower)ModelDb.Power<RoyalRegaliaPower>().ToMutable(); power.ApplyInternal(king.Creature, (1 << 25) - 1, true);
            Check(power.Amount == (1 << 25) - 1 && power.DisplayAmount == 25 && power.Description.GetFormattedText().Contains(effects[24]), "Native Amount/display/description loses high bit");
        }
        Console.WriteLine("PASS catalog: 25 exact native relic identities, 16 retained indices, 1024 seeded selections/replays of 10 distinct relics; all 25 reachable; EN/deu/zhs native names, status/intent formatting and high mask bit.");
    }

    private static void OpeningAndDamage(int count)
    {
        var (combat, king, targets) = Setup(count, Mask(8, 10));
        Await(PowerCmd.Apply<ArtifactPower>(Context, targets[0], 1, null, null));
        Await(king.BeforeCombatStart());
        var power = king.Creature.GetPower<RoyalRegaliaPower>()!; Await(power.ApplyOpening());
        Check(king.Creature.GetPowerAmount<VigorPower>() == 4 && !targets[0].HasPower<WeakPower>() && !targets[0].HasPower<ArtifactPower>() && targets.Skip(1).All(t => t.GetPowerAmount<WeakPower>() == 1), "Opening repeats, scales or bypasses Artifact");
        NativeConsoleFixture.Set(king.Deck!, nameof(RoyalDeck.Planned), new[] { 1, 0 });
        int forecast = RoyalForecastAttackIntent.Forecast(king, king.Creature, targets[0], 20, 1, false);
        int hp = targets[0].CurrentHp;
        Await(DamageCmd.Attack(20).FromMonster(king).WithHitCount(3).Execute(Context));
        Check(targets[0].CurrentHp == hp - 3 * forecast && !king.Creature.HasPower<VigorPower>(), "Akabeko multihit/intent/native consumption differs");
        int following = RoyalForecastAttackIntent.Forecast(king, king.Creature, targets[0], 20, 0, false);
        hp = targets[0].CurrentHp; Await(DamageCmd.Attack(20).FromMonster(king).Execute(Context));
        Check(targets[0].CurrentHp == hp - following && forecast == 9 && following == 8, "Royal round-one ramp or one-shot vigor differs");
        // A fallback attack also consumes the same native Vigor, without an Attack-card trigger.
        Await(PowerCmd.Apply<VigorPower>(Context, king.Creature, 4, king.Creature, null));
        NativeConsoleFixture.Set(king.Deck!, nameof(RoyalDeck.Planned), Array.Empty<int>());
        int fallback = RoyalForecastAttackIntent.Forecast(king, king.Creature, targets[0], 12, -1, false);
        hp = targets[0].CurrentHp; Await(DamageCmd.Attack(12).FromMonster(king).Execute(Context));
        Check(targets[0].CurrentHp == hp - fallback && !king.Creature.HasPower<VigorPower>(), "Fallback forecast/consumption");
    }

    private static string Triggers(int count)
    {
        var (combat, king, targets) = Setup(count, Mask(18, 19, 20, 21, 22, 23, 24));
        Await(king.BeforeCombatStart()); var power = king.Creature.GetPower<RoyalRegaliaPower>()!;
        for (int hand = 0; hand < 4; hand++)
        {
            power.BeginRoyalHand(); int block = king.Creature.Block, strength = king.Creature.GetPowerAmount<StrengthPower>();
            Await(power.AfterRoyalCard(34)); // Power, not an Attack.
            for (int i = 0; i < 2; i++) Await(power.AfterRoyalCard(1));
            Check(king.Creature.Block == block && king.Creature.GetPowerAmount<StrengthPower>() == strength, "Early third-card trigger");
            Await(power.AfterRoyalCard(1));
            Check(king.Creature.Block == block + 4 && king.Creature.GetPowerAmount<DexterityPower>() == Math.Min(3, hand + 1), "Fan/Kunai third Attack cap or native unpowered Block");
            for (int i = 0; i < 3; i++) Await(power.AfterRoyalCard(1));
            Check(king.Creature.Block == block + 4 && king.Creature.GetPowerAmount<StrengthPower>() == strength, "Fan repeats or Shuriken buffs an already-announced later attack");
            Await(power.EndRoyalHand(true)); Await(power.EndRoyalHand(true));
            Check(king.Creature.GetPowerAmount<StrengthPower>() == Math.Min(2, hand + 1) && king.Deck!.NextDraw == 0, "Shuriken combat cap/end idempotence or Pocketwatch threshold");
        }
        power.BeginRoyalHand(); int before = king.Creature.Block;
        Await(power.BeforeRoyalCard(0)); Check(king.Creature.Block == before, "Helmet triggered on one Energy");
        for (int i = 0; i < 4; i++) Await(power.BeforeRoyalCard(4));
        Check(king.Creature.Block == before + 6, "Helmet cap/Block scales with Dexterity/party");
        before = king.Creature.Block; for (int i = 0; i < 4; i++) Await(king.AfterPileAction(false));
        Check(king.Creature.Block == before + 6, "Bandage discard route/cap");
        Await(king.AfterPileAction(true)); Check(king.Creature.Block == before + 6, "Bandages triggered on Exhaust");
        power.BeginRoyalHand(); before = king.Creature.Block; Await(king.AfterPileAction(false));
        Check(king.Creature.Block == before + 3, "Bandages did not reset");
        before = king.Creature.Block; Await(power.AfterSideTurnStart(CombatSide.Player, targets, combat));
        Await(power.AfterSideTurnStart(CombatSide.Enemy, [], combat)); Check(king.Creature.Block == before, "Sai triggered for wrong side/participants");
        Await(power.AfterSideTurnStart(CombatSide.Enemy, [king.Creature], combat)); Check(king.Creature.Block == before + 3, "Sai amount scaled");
        for (int i = 0; i < 3; i++) Await(power.AfterRoyalCard(0));
        Await(power.EndRoyalHand(true)); Await(power.EndRoyalHand(true)); Check(king.Deck!.NextDraw == 1, "Pocketwatch threshold/repeat");
        king.Deck.ReserveNext(3, 0); king.Deck.ReserveNext(9, 0, relic: true); king.Deck.Prepare(5);
        Check(king.Deck.Hand.Count == 10 && king.Deck.NextDraw == 0, "Combined relic draw exceeds native royal hand cap"); king.Deck.Finish();
        int[] plan = [0, 1, 2]; NativeConsoleFixture.Set(king.Deck, nameof(RoyalDeck.Planned), plan);
        var intents = RoyalRegaliaPower.HandIntents(king, plan).ToArray();
        Check(intents.Any(i => i is RoyalRelicDefenseIntent) && intents.Any(i => i is RoyalRelicBuffIntent), "Missing concrete relic intents");
        foreach (var intent in intents)
        {
            var loc = (MegaCrit.Sts2.Core.Localization.LocString)AccessTools.Method(intent.GetType(), "GetIntentDescription").Invoke(intent, [targets, king.Creature])!;
            string text = loc.GetFormattedText(); Check(text.Contains("Ornamental Fan") || text.Contains("Pocketwatch"), "Unexplained relic intent");
            Check(!text.Contains("Kunai:") && !text.Contains("Shuriken:"), "Capped combat bonus still announced");
        }
        // Later hands see existing Strength through the native preview hook; allies do not inherit it.
        NativeConsoleFixture.Set(combat, nameof(combat.RoundNumber), 7);
        int forecast = RoyalForecastAttackIntent.Forecast(king, king.Creature, targets[0], 20, 0, false);
        int hp = targets[0].CurrentHp; Await(DamageCmd.Attack(20).FromMonster(king).Execute(Context));
        Check(targets[0].CurrentHp == hp - forecast && forecast == 22, "Capped Shuriken later-hand forecast/ramp");
        return power.Progress + ";" + king.Deck.Snapshot + ";" + king.Creature.Block + ";" + string.Join(',', targets.Select(t => t.CurrentHp));
    }

    private static void CombinedOffense(int count)
    {
        foreach (int round in new[] { 1, 4, 7 })
        foreach (bool debuffs in new[] { false, true })
        {
            var (combat, king, targets) = Setup(count, Mask(0, 8, 20));
            Await(king.BeforeCombatStart()); var power = king.Creature.GetPower<RoyalRegaliaPower>()!;
            // Reach Shuriken's cap through its actual card/end hooks. Vigor remains
            // pending because these supplied notifications do not perform attacks.
            for (int hand = 0; hand < 2; hand++)
            {
                power.BeginRoyalHand(); for (int i = 0; i < 3; i++) Await(power.AfterRoyalCard(0));
                Await(power.EndRoyalHand(true));
            }
            Check(king.Creature.GetPowerAmount<StrengthPower>() == 3 && king.Creature.GetPowerAmount<VigorPower>() == 4, "Vajra/Shuriken/Akabeko combination setup");
            NativeConsoleFixture.Set(combat, nameof(combat.RoundNumber), round);
            NativeConsoleFixture.Set(king.Deck!, nameof(RoyalDeck.Planned), new[] { 1 });
            if (debuffs)
            {
                Await(PowerCmd.Apply<WeakPower>(Context, king.Creature, 1, targets[0], null));
                foreach (var target in targets) Await(PowerCmd.Apply<VulnerablePower>(Context, target, 1, king.Creature, null));
            }
            int forecast = RoyalForecastAttackIntent.Forecast(king, king.Creature, targets[0], 20, 1, false);
            int[] hp = targets.Select(t => t.CurrentHp).ToArray();
            Await(DamageCmd.Attack(20).FromMonster(king).WithHitCount(3).Execute(Context));
            Check(targets.Select((t, i) => t.CurrentHp == hp[i] - 3 * forecast).All(x => x), "Combined buffs/debuffs/ramp forecast differs from actual multihit");
            Check(!king.Creature.HasPower<VigorPower>() && king.Creature.GetPowerAmount<StrengthPower>() == 3, "Combined attack consumes permanent Strength or retains Vigor");
        }
        Console.WriteLine($"PASS offensive combinations: party {count}, rounds 1/4/7, Vajra+Akabeko+capped Shuriken with/without Weak+Vulnerable; actual 3-hit attacks match native forecasts. Fresh command/counter replay also matches.");
    }
}
