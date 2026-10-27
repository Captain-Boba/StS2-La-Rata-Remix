using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

internal static class RoyalIntentWaveChecks
{
    private static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }
    internal static void Run()
    {
        var harmony = new Harmony("LaRata.ContentChecks.RoyalIntentWave");
        NativeConsoleFixture.PatchDesktop(harmony);
        try
        {
            var player = NativeConsoleFixture.Player(71);
            var run = RunState.CreateForTest(players: [player], seed: "INTENTWAVE");
            var combat = new CombatState(ModelDb.Encounter<RatKingEncounter>().ToMutable(), run);
            combat.RoundNumber = 7; // This suite isolates the fully-ramped forecast/Artifact rules.
            var body = new Creature(player, 50, 50) { CombatState = combat };
            NativeConsoleFixture.Set(player, nameof(player.Creature), body);
            player.ResetCombatState(); combat.AddCreature(body);
            var king = (RatKing)ModelDb.Monster<RatKing>().ToMutable();
            var secondKing = (RatKing)ModelDb.Monster<RatKing>().ToMutable();
            foreach (string name in new[] { "resolvedCards", "targetIds" })
            {
                var field = AccessTools.Field(typeof(RatKing), name);
                Require(!ReferenceEquals(field.GetValue(king), field.GetValue(secondKing)) &&
                    !ReferenceEquals(field.GetValue(king), field.GetValue(ModelDb.Monster<RatKing>())), "Boss clones share mutable " + name);
            }
            var enemy = new Creature(king, CombatSide.Enemy, "king") { CombatState = combat };
            combat.AddCreature(enemy);
            var deck = new RoyalDeck(_ => 0);
            AccessTools.Field(typeof(RatKing), "deck").SetValue(king, deck);
            var strength = ModelDb.Power<StrengthPower>().ToMutable(); strength.ApplyInternal(enemy, 2, true);
            foreach (var (plan, artifact, expected) in new (int[], int, int)[]
            {
                ([35, 1], 0, 21), ([35, 1], 1, 21), ([35, 1], 2, 14),
                ([4, 35, 1], 3, 21), ([4, 35, 1], 4, 14)
            })
            {
                NativeConsoleFixture.Set(deck, nameof(deck.Planned), plan);
                var oldArtifact = body.GetPower<ArtifactPower>(); oldArtifact?.RemoveInternal();
                if (artifact > 0) ModelDb.Power<ArtifactPower>().ToMutable().ApplyInternal(body, artifact, true);
                string before = deck.Snapshot;
                int actual = RoyalForecastAttackIntent.Forecast(king, enemy, body, 12, 1, false);
                Require(actual == expected, $"Forecast with Artifact {artifact}, plan {string.Join(',', plan)} expected {expected}, got {actual}");
                Require(deck.Snapshot == before && body.GetPowerAmount<ArtifactPower>() == artifact &&
                    body.GetPowerAmount<VulnerablePower>() == 0 && enemy.GetPowerAmount<StrengthPower>() == 2,
                    "Intent query mutated powers or deck state");
            }
            body.GetPower<ArtifactPower>()?.RemoveInternal();
            NativeConsoleFixture.Set(deck, nameof(deck.Planned), new[] { 35, 1 });
            var guard = new Creature(ModelDb.Monster<CrownHeadsman>().ToMutable(), CombatSide.Enemy, "guard") { CombatState = combat };
            combat.AddCreature(guard);
            Require(RoyalForecastAttackIntent.Forecast(king, guard, body, 12, -1, true) == 18, "Alive king's announced vulnerability is not projected for guard");
            enemy.SetCurrentHpInternal(0);
            Require(RoyalForecastAttackIntent.Forecast(king, guard, body, 12, -1, true) == 12, "Dead king's future effect remains in guard forecast");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
        Console.WriteLine("PASS: five directed native-hook intent forecasts respect Artifact applications/order and do not mutate live power/deck state. Announced plans supplied by fixture; no actual attack execution or rendered intent acceptance.");
    }
}
