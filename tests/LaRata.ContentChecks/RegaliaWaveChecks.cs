using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

internal static class RegaliaWaveChecks
{
    private static readonly List<(int Relic, int Cards, int Energy)> Reserves = [];
    private static int damageCalls;
    private static bool Reserve(int relic, int cards, int energy, ref Task __result)
    { Reserves.Add((relic, cards, energy)); __result = Task.CompletedTask; return false; }
    private static bool Damage(decimal amount, ValueProp props, ref Task<IEnumerable<DamageResult>> __result)
    {
        if (amount != 5 || props != ValueProp.Unpowered) throw new Exception("Letter Opener damage command differs");
        damageCalls++; __result = Task.FromResult<IEnumerable<DamageResult>>([]); return false;
    }
    private static bool InProgress(ref bool __result) { __result = true; return false; }
    private static bool NotEnding(ref bool __result) { __result = false; return false; }
    private static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }
    internal static void Run()
    {
        var harmony = new Harmony("LaRata.ContentChecks.RegaliaWave");
        NativeConsoleFixture.PatchDesktop(harmony);
        harmony.Patch(AccessTools.Method(typeof(RatKing), "ReserveRelic"), prefix: new(typeof(RegaliaWaveChecks), nameof(Reserve)));
        harmony.Patch(AccessTools.Method(typeof(CreatureCmd), nameof(CreatureCmd.Damage), [typeof(PlayerChoiceContext), typeof(IEnumerable<Creature>), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel)]), prefix: new(typeof(RegaliaWaveChecks), nameof(Damage)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsInProgress)), prefix: new(typeof(RegaliaWaveChecks), nameof(InProgress)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsEnding)), prefix: new(typeof(RegaliaWaveChecks), nameof(NotEnding)));
        try
        {
            var king = (RatKing)ModelDb.Monster<RatKing>().ToMutable();
            var combat = new CombatState(ModelDb.Encounter<RatKingEncounter>().ToMutable());
            var body = new Creature(king, CombatSide.Enemy, "king") { CombatState = combat }; combat.AddCreature(body);
            var power = (RoyalRegaliaPower)ModelDb.Power<RoyalRegaliaPower>().ToMutable();
            power.ApplyInternal(body, (1 << 14) | (1 << 15) | (1 << 16) | (1 << 17), true);
            Reserves.Clear(); damageCalls = 0;
            power.BeginRoyalHand();
            power.AfterRoyalCard(34).GetAwaiter().GetResult(); // Power is not a Skill.
            for (int i = 0; i < 2; i++) power.AfterRoyalCard(5).GetAwaiter().GetResult();
            Require(damageCalls == 0, "Letter Opener fired before three Skills or counted a Power");
            power.AfterRoyalCard(5).GetAwaiter().GetResult();
            for (int i = 0; i < 4; i++) power.AfterRoyalCard(5).GetAwaiter().GetResult();
            Require(damageCalls == 1, "Letter Opener did not fire once per hand");
            power.BeginRoyalHand(); for (int i = 0; i < 3; i++) power.AfterRoyalCard(5).GetAwaiter().GetResult();
            Require(damageCalls == 2, "Letter Opener did not reset for next hand");
            power.EndRoyalHand(true).GetAwaiter().GetResult(); Require(Reserves.Count == 0, "Art of War triggers after an Attack card");
            power.EndRoyalHand(false).GetAwaiter().GetResult(); Require(Reserves.SequenceEqual(new[] { (15, 0, 1) }), "Art of War next-hand energy differs");
            Reserves.Clear();
            for (int i = 0; i < 9; i++) power.AfterRoyalCard(1).GetAwaiter().GetResult(); // Scepter Storm has three hits.
            Require(Reserves.Count == 0, "Nunchaku counts hits or triggers before ten Attack cards");
            power.AfterRoyalCard(1).GetAwaiter().GetResult();
            for (int i = 0; i < 10; i++) power.AfterRoyalCard(1).GetAwaiter().GetResult();
            Require(Reserves.SequenceEqual(new[] { (17, 0, 1), (17, 0, 1) }), "Nunchaku does not repeat at each tenth Attack card");
            Reserves.Clear();
            var ctx = new ThrowingPlayerChoiceContext();
            power.AfterDamageReceived(ctx, body, new DamageResult(body, ValueProp.Unpowered) { BlockedDamage = 10 }, ValueProp.Unpowered, null, null).GetAwaiter().GetResult();
            Require(Reserves.Count == 0, "Puzzle triggers on fully blocked damage");
            for (int i = 0; i < 2; i++) power.AfterDamageReceived(ctx, body, new DamageResult(body, ValueProp.Unpowered) { UnblockedDamage = 1 }, ValueProp.Unpowered, body, null).GetAwaiter().GetResult();
            Require(Reserves.SequenceEqual(new[] { (16, 3, 0) }), "Puzzle misses self-damage or triggers more than once");
        }
        finally { Reserves.Clear(); harmony.UnpatchAll(harmony.Id); }
        Console.WriteLine("PASS: four new boss-Regalia trigger methods at skill/hand, no-Attack, first-unblocked/self-damage and tenth-Attack boundaries. Native power instances; damage and reservation sinks observed, combat-active guard substituted. No actual attack/power lifecycle or combination/balance acceptance.");
    }
}
