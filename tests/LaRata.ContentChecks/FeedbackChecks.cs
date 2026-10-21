using System.Text.Json;
using BaseLib.Utils;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

internal static class FeedbackChecks
{
    private static void Require(bool value, string why) { if (!value) throw new Exception(why); }
    internal static void Run()
    {
        ModelDb.Inject(typeof(RatKing));
        var harmony = new Harmony("LaRata.FeedbackChecks");
        harmony.CreateClassProcessor(typeof(HexTelegraphPatch)).Patch();
        harmony.CreateClassProcessor(typeof(TwistRegenScaling)).Patch();
        try
        {
            // Execute the patched native method in exactly the Bestiary order.
            foreach (var canonical in new MonsterModel[] { ModelDb.Monster<Inklet>(), ModelDb.Monster<CorpseSlug>(),
                ModelDb.Monster<TwoTailedRat>(), ModelDb.Monster<RatKing>(), ModelDb.Monster<CrownArmorer>() })
            {
                var monster = canonical.ToMutable();
                monster.SetUpForCombat(); // no Creature exists yet
                Require(monster.MoveStateMachine != null, "Bestiary setup lost move machine");
                var preview = canonical.ToMutable();
                _ = new Creature(preview, CombatSide.Enemy, null) { CombatState = new NullCombatState() };
                preview.SetUpForCombat(); // preview Creature, no encounter
            }
            var encounter = ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable();
            var enemy = ModelDb.Monster<TwoTailedRat>().ToMutable();
            var combat = new CombatState(encounter);
            _ = new Creature(enemy, CombatSide.Enemy, null) { CombatState = combat };
            var rules = new EncounterRules(new(64, 64, 0, [new(TwistKind.Crossfire, 0, 1)])) { Roster = [enemy] };
            EncounterDirector.Rules.Add(encounter, rules);
            enemy.SetUpForCombat();
            var decorated = enemy.MoveStateMachine!.States.Values.OfType<MoveState>().Where(m => HexTelegraphPatch.Hexes.TryGetValue(m, out _)).ToArray();
            Require(decorated.Length == 1 && decorated[0].Intents.Any(i => i is DebuffIntent), "Live hex no longer has a native debuff intent");
            var inklet = ModelDb.Monster<Inklet>().ToMutable();
            var inkletBody = new Creature(inklet, CombatSide.Enemy, null) { CombatState = combat };
            var twist = new EnemyTwist(TwistKind.Regenerating, 0, 1, 3);
            Require(StartingBuffsPatch.RegenCap(inkletBody, twist) == 1, "Inklet regen must be capped at one");
            var normalBody = new Creature(ModelDb.Monster<CorpseSlug>().ToMutable(), CombatSide.Enemy, null) { CombatState = combat };
            Require(StartingBuffsPatch.RegenCap(normalBody, twist) <= Math.Max(1, normalBody.MaxHp / 10), "Regen exceeds body cap");
            var regen = ModelDb.Power<MegaCrit.Sts2.Core.Models.Powers.RegenPower>().ToMutable();
            TwistRegenScaling.Unscaled.Add(regen, new object());
            Require(regen.GetScaledAmountForMultiplayer(new NullCombatState(), null, 1, inkletBody, null) == 1, "Tagged Regen must retain exact integer amount");
            TwistRegenScaling.Unscaled.Remove(regen);
            Require(regen.GetScaledAmountForMultiplayer(new NullCombatState(), null, 1, inkletBody, null) == 0, "Unmarked Regen must use native scaling (zero players in NullCombatState)");

            rules.Reinforcements.Add(inklet);
            var damageHook = AccessTools.Method(typeof(DamagePatch), "Prefix");
            object[] arguments = [combat, inkletBody, MegaCrit.Sts2.Core.ValueProps.ValueProp.Move,
                MegaCrit.Sts2.Core.Hooks.ModifyDamageHookType.Multiplicative, 3m];
            damageHook.Invoke(null, arguments);
            Require((decimal)arguments[4] == 3m, "Inklet helper attack was reduced to one again");
            var scoutRules = new EncounterRules(new(60, 56, 1, []) { ScoutReinforcement = true });
            scoutRules.Reinforcements.Add(normalBody.Monster!);
            EncounterDirector.Rules.Remove(encounter);
            EncounterDirector.Rules.Add(encounter, scoutRules);
            object[] scoutAttack = [combat, normalBody, MegaCrit.Sts2.Core.ValueProps.ValueProp.Move,
                MegaCrit.Sts2.Core.Hooks.ModifyDamageHookType.Multiplicative, 10m];
            damageHook.Invoke(null, scoutAttack);
            Require((decimal)scoutAttack[4] == 5m && scoutRules.ExtraHpFactor == .35m, "Scout must use reduced attack/HP factors");
            foreach (var kind in EncounterPlan.ActiveKinds.Where(EncounterPlan.HasLesser))
            {
                var lesser = new EnemyTwist(kind, 0, 1, 0, true);
                Require(EncounterPlan.Cost(lesser) * 2 == EncounterPlan.Cost(kind), "Lesser price is not halved");
                Require(lesser.Magnitude(2) == 1 && lesser.Magnitude(3) == 1 && lesser.Magnitude(4) == 2,
                    "Lesser runtime grant is not reduced");
                Require(lesser.Defense(12) == 6 && lesser.Description.StartsWith("Lesser "), "Lesser defense/description mismatch");
            }
        }
        finally { harmony.UnpatchAll(harmony.Id); }
        Console.WriteLine("PASS: actual Harmony-patched Bestiary setup without Creature and with NullCombatState; live encounter still telegraphs its hex; capped instance-scoped Regen and full Inklet helper damage.");

        var native = ModelDb.CardPool<ColorlessCardPool>().AllCards.ToArray();
        var offered = RemixCard.Neutrals.Take(5).ToArray();
        var pool = BoosterRelic.NeutralBonusPool(native, offered);
        Require(pool.Any(c => c is RemixCard) && pool.Any(c => c is not RemixCard), "Bonus pool must contain both neutral sources");
        Require(pool.All(c => offered.All(o => o.Id != c.Id)) && pool.Select(c => c.Id).Distinct().Count() == pool.Length, "Bonus pool duplicates an original offer");
        Require(pool.SequenceEqual(BoosterRelic.NeutralBonusPool(native.Reverse(), offered.Reverse())), "Pool ordering depends on input enumeration");
        foreach (var pack in new BoosterRelic[] { ModelDb.Relic<IroncladBooster>(), ModelDb.Relic<SilentBooster>(), ModelDb.Relic<DefectBooster>(), ModelDb.Relic<RegentBooster>(), ModelDb.Relic<NecrobinderBooster>(), ModelDb.Relic<RoyalBooster>() })
            Require(pack.Rarity == RelicRarity.Common, "Booster rarity must be Common");
        Console.WriteLine("PASS: Common boosters; mixed neutral bonus pool excludes the original offers and has stable ordering.");

        foreach (var (character, expected) in new (CharacterModel, BoosterRelic)[]
        {
            (ModelDb.Character<Ironclad>(), ModelDb.Relic<IroncladBooster>()),
            (ModelDb.Character<Silent>(), ModelDb.Relic<SilentBooster>()),
            (ModelDb.Character<Defect>(), ModelDb.Relic<DefectBooster>()),
            (ModelDb.Character<Regent>(), ModelDb.Relic<RegentBooster>()),
            (ModelDb.Character<Necrobinder>(), ModelDb.Relic<NecrobinderBooster>())
        })
        {
            Require(StartingRelicsPatch.ClassBooster(character).Id == expected.Id, "Starting pack does not match character");
            var pending = (BoosterRelic)expected.ToMutable();
            pending.StarterPending = true;
            pending.AfterObtained().GetAwaiter().GetResult(); // no UI or Owner yet
            Require(!pending.Opened && pending.Contents.Count == 0, "Starting pack opened before the run UI exists");
        }
        Console.WriteLine("PASS: all five native characters receive their own pack; initial pickup defers opening until the room transition finishes.");

        var snapshot = new JournalSnapshot("combat-a", [new(2, "card", ModelDb.Card<RoyalTax>().Id.ToString(), null),
            new(3, "potion", ModelDb.Potion<StrengthPotion>().Id.ToString(), 44)]);
        string json = JsonSerializer.Serialize(snapshot);
        Require(CombatJournal.Decode(json, "combat-a").Entries.SequenceEqual(snapshot.Entries), "Journal JSON loses played card/recipient/round");
        Require(CombatJournal.Decode(json, "combat-b").Entries.Length == 0, "Previous combat leaked into new combat");
        Require(CombatJournal.Decode("not json", "combat-a").Entries.Length == 0, "Malformed optional history must not break saves");
        var field = CombatJournal.Saved;
        var run = NullRunState.Instance;
        field.Set(run, json);
        var saved = new SavedProperties();
        field.Export(run, saved);
        field.Set(run, "");
        field.Import(run, saved);
        Require(field.Get(run) == json, "BaseLib SavedSpireField import/export lost history");
        CombatJournal.Clear(run);
        Require(field.Get(run) == "", "Combat reset failed");
        Console.WriteLine("PASS: combat journal JSON and BaseLib save-field roundtrip; new combat isolation, corrupt-data fallback and reset.");
    }
}
