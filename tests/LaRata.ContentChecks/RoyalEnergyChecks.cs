using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Runs;

internal static class RoyalEnergyChecks
{
    private static void Require(bool value, string reason)
    {
        if (!value) throw new Exception(reason);
    }

    internal static void Run()
    {
        var harmony = new Harmony("LaRata.ContentChecks.RoyalEnergy");
        NativeConsoleFixture.PatchDesktop(harmony);
        try
        {
            using var localization = new NativeFixtureLocalization();
            int hands = 0;
            foreach (int count in new[] { 1, 2, 3, 4 })
            foreach (var (mask, bank) in new[]
            {
                (0, false), (1 << 8, false), (1 << 10, false),
                ((1 << 8) | (1 << 9) | (1 << 10), true)
            })
            {
                var players = Enumerable.Range(0, count).Select(i => NativeConsoleFixture.Player((ulong)(1101 + i))).ToArray();
                var run = RunState.CreateForTest(players: players, seed: "ROYALENERGY");
                var encounter = ModelDb.Encounter<RatKingEncounter>().ToMutable();
                var combat = new CombatState(encounter, run);
                foreach (var player in players)
                {
                    NativeConsoleFixture.Set(player, nameof(player.Creature), new Creature(player, 100, 100) { CombatState = combat });
                    player.ResetCombatState();
                    combat.AddCreature(player.Creature);
                }
                var king = (RatKing)ModelDb.Monster<RatKing>().ToMutable();
                var body = new Creature(king, CombatSide.Enemy, "king") { CombatState = combat };
                body.ScaleMonsterHpForMultiplayer(encounter, count, 2);
                combat.AddCreature(body);
                // Supply directed relic masks only; use the real deck factory, RNG,
                // PrepareHand, target planning and native MoveState intent assignment.
                AccessTools.Field(typeof(RatKing), "regaliaMask").SetValue(king, (int?)mask);
                var move = new MoveState("ROYAL_HAND", _ => Task.CompletedTask);
                int turn = 0;
                foreach (int offset in new[] { 1, 0, -1, 1, 0, -1 })
                {
                    turn++;
                    body.SetCurrentHpInternal(body.MaxHp / 2 + offset);
                    if (bank)
                    {
                        var deck = (RoyalDeck)AccessTools.Method(typeof(RatKing), "GetOrCreateDeck").Invoke(king, null)!;
                        Require(deck.ReserveNext(1, 2) == (1, 2), "Card bonus reservation changed");
                        Require(deck.ReserveNext(1, 3, relic: true) == (1, 3), "Separate relic bonus reservation changed");
                    }
                    var result = AccessTools.Method(typeof(RatKing), "PrepareHand").Invoke(king, [move]);
                    int expected = offset > 0 ? 5 : 7;
                    if (bank) expected += 5;
                    int drawn = 5 + (turn == 1 && (mask & (1 << 9)) != 0 ? 2 : 0) + (bank ? 2 : 0);
                    var actual = king.Deck!;
                    Require((string?)result == move.Id && actual.Energy == expected && actual.Capacity == expected,
                        $"PrepareHand count={count} turn={turn} hp={body.CurrentHp}/{body.MaxHp} mask={mask} bank={bank}: expected {expected}, got {actual.Energy}/{actual.Capacity}");
                    Require(actual.DrawnThisTurn == drawn && actual.NextEnergy == 0 && actual.NextDraw == 0,
                        "Draw 5, additive draw bonuses or one-shot next-hand bonus consumption changed");
                    Require(actual.Planned.Sum(i => RoyalDeck.Costs[i]) <= expected && move.Intents.Any(),
                        "Real planned hand overspends or has no intents");
                    actual.Finish(); // No card execution/full fight is claimed by this preparation fixture.
                    hands++;
                }
            }
            Console.WriteLine($"PASS: {hands} real RatKing.PrepareHand calls for 1/2/3/4 modeled players, one HP above/exactly at/one HP below half; base 5/7 with NO Lantern/Happy Flower bonuses from replacement bits 8/10, additive card/relic next-hand banks, Draw 5 and bonus consumption. Supplied relic masks; no live game, disk save or networking.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    }
}
