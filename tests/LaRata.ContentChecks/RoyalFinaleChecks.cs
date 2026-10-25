using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

internal static class RoyalFinaleChecks
{
    private static void Require(bool value, string why) { if (!value) throw new Exception(why); }
    internal static void Run()
    {
        ModelDb.Inject(typeof(RatKingEncounter));
        ModelDb.Inject(typeof(RoyalRegaliaPower));
        var canonical = ModelDb.Encounter<RatKingEncounter>();
        var seenPairs = new HashSet<string>();
        for (uint seed = 0; seed < 128; seed++)
        {
            var snapshots = new List<string>();
            for (int peer = 0; peer < 4; peer++)
            {
                var encounter = canonical.ToMutable();
                // Supply the same native encounter RNG that a saved seed/floor creates.
                AccessTools.Field(typeof(EncounterModel), "_rng").SetValue(encounter, new Rng(seed));
                encounter.GenerateMonstersWithSlots(NullRunState.Instance);
                var roster = encounter.MonstersWithSlots;
                Require(roster.Count == 3 && roster[0].Item1 is RatKing, "King must lead the formation");
                Require(roster.Skip(1).All(p => p.Item1 is KingsguardKnight), "Old rat returned to finale");
                Require(roster.Select(p => p.Item1.Id).Distinct().Count() == 3, "Duplicate bodyguards");
                Require(roster.Select(p => p.Item2).SequenceEqual(new[] { "king", "court_left", "court_right" }), "Formation slots changed");
                Require(roster.All(p => canonical.AllPossibleMonsters.Any(m => m.Id == p.Item1.Id)), "Missing preload model");
                snapshots.Add(string.Join(',', roster.Select(p => p.Item1.Id.Entry)));
                seenPairs.Add(string.Join(',', roster.Skip(1).Select(p => p.Item1.Id.Entry).Order()));
            }
            Require(snapshots.Distinct().Count() == 1, "Four identical inputs choose different guards");
        }
        Require(seenPairs.Count == 6, "Not all six bodyguard pairs are reachable");
        var hp = new List<int>();
        for (int players = 1; players <= 4; players++)
        {
            var body = new Creature(ModelDb.Monster<RatKing>().ToMutable(), CombatSide.Enemy, "king");
            Require(body.MaxHp == 1500, "Base HP is not 1500");
            // The existing bonus-act patch maps act index 3 to this native final-act table.
            body.ScaleMonsterHpForMultiplayer(canonical, players, 2);
            hp.Add(body.MaxHp);
        }
        Require(hp.SequenceEqual(new[] { 1500, 3900, 5850, 7800 }), "Native boss HP scaling changed");
        Console.WriteLine("PASS: 128 encounter seeds on four simulated peers; six unique guard pairs; native HP for 1/2/3/4 players = " + string.Join('/', hp));

        var combat = new CombatState(canonical.ToMutable());
        var king = (RatKing)ModelDb.Monster<RatKing>().ToMutable();
        var kingBody = new Creature(king, CombatSide.Enemy, "king") { CombatState = combat };
        combat.AddCreature(kingBody);
        foreach (var source in RatKingEncounter.Guards)
        {
            var guard = (KingsguardKnight)source.ToMutable();
            guard.SetUpForCombat();
            // Native Bestiary calls with no Creature remain safe.
            var moves = guard.MoveStateMachine!.States.Values.OfType<MoveState>();
            var attacks = moves.SelectMany(m => m.Intents).OfType<AttackIntent>().ToArray();
            var before = attacks.Select(a => a.DamageCalc!()).ToArray();
            var guardBody = new Creature(guard, CombatSide.Enemy, "court_left") { CombatState = combat };
            combat.AddCreature(guardBody);
            guard.AfterAddedToRoom().GetAwaiter().GetResult();
            king.PendingStrength = 5; // Blood Contract + Kneel in the announced hand.
            Require(attacks.Select(a => a.DamageCalc!()).SequenceEqual(before.Select(d => d + 5)), "Guard intent misses pending royal Strength");
            var court = (Creature[])AccessTools.PropertyGetter(typeof(KingsguardKnight), "Court").Invoke(guard, null)!;
            Require(court.Contains(kingBody), "Guard support excludes La Rata");
            king.PendingStrength = 0;
            Require(attacks.Select(a => a.DamageCalc!()).SequenceEqual(before), "Guard intent double counts already-applied Strength");
        }
        for (uint seed = 0; seed < 128; seed++)
        {
            int mask = RoyalRegaliaPower.Choose(new Rng(seed));
            Require(System.Numerics.BitOperations.PopCount((uint)mask) == 10, "Regalia count differs");
            Require(mask == RoyalRegaliaPower.Choose(new Rng(seed)), "Regalia reload differs");
            var power = (RoyalRegaliaPower)ModelDb.Power<RoyalRegaliaPower>().ToMutable();
            power.ApplyInternal(kingBody, mask, true);
            Require(power.DisplayAmount == 10, "Regalia display exposes bitmask");
            power.RemoveInternal();
        }
        var rod = (RoyalRegaliaPower)ModelDb.Power<RoyalRegaliaPower>().ToMutable();
        rod.ApplyInternal(kingBody, 1 << 12, true);
        Require(rod.ModifyHpLostAfterOsty(kingBody, 8, ValueProp.Unpowered, null, null) == 7, "Rod misses owner damage");
        Require(rod.ModifyHpLostAfterOsty(kingBody, 0, ValueProp.Unpowered, null, null) == 0, "Rod produces negative HP loss");
        Require(rod.ModifyHpLostAfterOsty(combat.Enemies[1], 8, ValueProp.Unpowered, null, null) == 8, "Rod changes someone else's damage");
        Console.WriteLine("PASS: guard support includes king; six attack intents track 5 pending Strength without Bestiary Creature access; ten-relic replay/display and owner-scoped Tungsten Rod hook.");

        Require(RoyalCard.All.Length == 25 && RemixCard.BossPrizes.Length == 10, "Royal pools have incorrect size");
        Require(RoyalDeck.ActiveCards.Length == 46 && !RoyalDeck.ActiveCards.Contains(3) && !RoyalDeck.ActiveCards.Contains(21), "Retired poison cards remain in the active boss deck");
        Require(CameoSchedule.RoyalPool.All(i => RoyalDeck.ActiveCards.Contains(i)), "Royal cameo uses a retired boss card");
        Require(CameoSchedule.RoyalPool.Select(i => RoyalCard.All[i].GetType()).SequenceEqual(new[]
        {
            typeof(RoyalFlush), typeof(StolenAegis), typeof(ExposedCrown),
            typeof(StolenHour), typeof(RoyalPardon), typeof(NightfallDecree)
        }), "Royal intervention IDs resolve to the wrong card models");
        ModelDb.Inject(typeof(RoyalInterventionPower));
        foreach (int card in CameoSchedule.RoyalPool)
        {
            var marker = (RoyalInterventionPower)ModelDb.Power<RoyalInterventionPower>().ToMutable();
            marker.ApplyInternal(kingBody, CameoSchedule.Encode(3, card), true);
            Require(CameoSchedule.Card(marker.Amount) == card && CameoSchedule.Round(marker.Amount) == 3 && marker.DisplayAmount == 0,
                "Native power changes or exposes the extended hidden intervention state");
            marker.RemoveInternal();
        }
        Console.WriteLine("PASS: six intervention IDs resolve to approved native card models; native power amount preserves extended card/round state without displaying it.");
        foreach (var (card, cost, target, expected) in new (RemixCard, int, TargetType, (string Key, int Base, int Up)[])[]
        {
            (ModelDb.Card<RoyalTithePrize>(),1,TargetType.AnyEnemy,[("Damage",10,16)]),
            (ModelDb.Card<RoyalPlaguePrize>(),2,TargetType.AllEnemies,[("PoisonPower",6,9),("DoomPower",12,18)]),
            (ModelDb.Card<RoyalKneelPrize>(),2,TargetType.AllAllies,[("Block",12,18),("StrengthPower",2,3)]),
            (ModelDb.Card<RoyalForeclosurePrize>(),3,TargetType.AllEnemies,[("Damage",24,32),("Heal",6,9)]),
            (ModelDb.Card<RoyalNightfallPrize>(),2,TargetType.AllAllies,[("RitualPower",1,2)])
        })
        {
            Require(card.EnergyCost.Canonical == cost && card.TargetType == target && card.Rarity == CardRarity.Rare, "New prize cost/target/rarity differs");
            Require(expected.All(e => card.DynamicVars[e.Key].BaseValue == e.Base), "New prize base values differ");
            var upgraded = card.ToMutable(); upgraded.UpgradeInternal(); upgraded.FinalizeUpgradeInternal();
            Require(expected.All(e => upgraded.DynamicVars[e.Key].BaseValue == e.Up), "New prize upgrade values differ");
            Require(RoyalCard.All.Any(c => c.CustomPortraitPath == card.CustomPortraitPath), "Prize does not share its boss artwork");
            Require(!RemixCard.Neutrals.Contains(card), "Exclusive prize leaked into neutral pack slots");
        }
        var tithe = ModelDb.Card<BlackTithe>();
        Require(tithe.GetIntents(king).Single() is RoyalEdictIntent, "Block-removal warning is missing");
        Require(ModelDb.Card<PlagueOfCrowns>().GetIntents(king).Any(i => i is RoyalEdictIntent), "Plague warning is missing");
        Console.WriteLine("PASS: five new rare boss prizes have explicit base/upgrade values, targets and shared artwork; dangerous boss debuffs use dedicated intent descriptions. No gameplay or network test.");
    }
}
