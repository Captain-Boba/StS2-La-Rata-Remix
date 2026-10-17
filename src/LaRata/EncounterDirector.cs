using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace LaRata;

internal sealed class EncounterRules(EncounterPlan plan, MonsterModel leader)
{
    internal EncounterPlan Plan { get; } = plan;
    internal MonsterModel Leader { get; } = leader;
    internal MonsterModel? Extra { get; set; }
    internal bool BossDuo { get; set; }
    internal EncounterModel? BossSource { get; set; }
    internal decimal ExtraHpFactor { get; set; } = .60m;
    internal bool Started { get; set; }
}

internal static class EncounterDirector
{
    internal static readonly ConditionalWeakTable<EncounterModel, EncounterRules> Rules = new();
    internal static Rng Random(IRunState run, EncounterModel encounter, string purpose) =>
        new(unchecked(run.Rng.Seed + (uint)(run.TotalFloor * 7919 + run.CurrentActIndex * 104729)),
            $"LaRata/v2/{encounter.Id.Entry}/{run.Players.Count}/{purpose}");

    internal static EncounterRules? Get(EncounterModel? encounter) =>
        encounter != null && Rules.TryGetValue(encounter, out var rules) ? rules : null;

    internal static bool IsFinalBoss(IRunState run, EncounterModel encounter) =>
        run.CurrentActIndex == Endgame.RegularFinalIndex(run) && run.CurrentActIndex > 0 &&
        encounter.RoomType == RoomType.Boss &&
        encounter.Id == (run.Act.SecondBossEncounter ?? run.Act.BossEncounter).Id;

    internal static IReadOnlyList<MonsterModel> BossCandidates(IRunState run)
    {
        // Use the native, saved room/monster history. Also excludes an earlier reinforcement on double-boss runs.
        var seen = run.MapPointHistory.SelectMany(x => x)
            .Where(x => !ReferenceEquals(x, run.CurrentMapPointHistoryEntry)).SelectMany(x => x.Rooms).ToArray();
        var seenEncounters = seen.Where(x => x.ModelId != null).Select(x => x.ModelId!).ToHashSet();
        var seenMonsters = seen.SelectMany(x => x.MonsterIds).ToHashSet();
        return run.Acts.Take(run.CurrentActIndex).SelectMany(x => x.AllBossEncounters)
            .Where(x => !seenEncounters.Contains(x.Id))
            .SelectMany(x => x.AllPossibleMonsters)
            // Ceremonial Beast's absolute phase HP threshold is incompatible with reduced starting HP.
            .Where(x => x is Vantom or LagavulinMatriarch or WaterfallGiant or KnowledgeDemon)
            .Where(x => !seenMonsters.Contains(x.Id)).DistinctBy(x => x.Id)
            .OrderBy(x => x.Id.Entry, StringComparer.Ordinal).ToArray();
    }

    internal static bool CanAddNormal(EncounterModel encounter, IReadOnlyList<(MonsterModel, string?)> monsters)
    {
        if (encounter is TwoTailedRatsNormal) return monsters.Count == 3;
        // Only autonomous, small creatures with no encounter/slot-specific setup.
        return encounter.RoomType == RoomType.Monster && !encounter.IsWeak && !encounter.HasScene &&
            !encounter.Slots.Any() && monsters.Count is >= 2 and <= 3 &&
            monsters.Any(x => x.Item1 is Inklet or LeafSlimeS or Nibbit or Flyconid or CorpseSlug or FuzzyWurmCrawler);
    }
}

[HarmonyPatch(typeof(EncounterModel), nameof(EncounterModel.GenerateMonstersWithSlots))]
internal static class GenerateEncounterPatch
{
    [HarmonyPostfix]
    private static void Postfix(EncounterModel __instance, IRunState runState,
        ref IReadOnlyList<(MonsterModel, string?)>? ____monstersWithSlots)
    {
        var monsters = ____monstersWithSlots!;
        if (__instance is RatKingEncounter || __instance.IsDebugEncounter || !__instance.ShouldGiveRewards || monsters.Count == 0 ||
            runState.Players.Count == 0 || EncounterDirector.Get(__instance) != null) return;
        var rng = EncounterDirector.Random(runState, __instance, "encounter");
        var leader = monsters[rng.NextInt(monsters.Count)].Item1;
        if (EncounterDirector.IsFinalBoss(runState, __instance))
        {
            var candidates = EncounterDirector.BossCandidates(runState);
            if (candidates.Count > 0 && __instance is QueenBoss or TestSubjectBoss or AeonglassBoss)
            {
                var extra = candidates[rng.NextInt(candidates.Count)].ToMutable();
                var rules = new EncounterRules(EncounterPlan.BossDuo, leader)
                {
                    Extra = extra, BossDuo = true, ExtraHpFactor = .45m + .05m * Math.Clamp(runState.Players.Count - 1, 0, 3),
                    BossSource = runState.Acts.Take(runState.CurrentActIndex).SelectMany(x => x.AllBossEncounters)
                        .First(x => x.AllPossibleMonsters.Any(m => m.Id == extra.Id))
                };
                EncounterDirector.Rules.Add(__instance, rules);
                ____monstersWithSlots = [.. monsters, (extra, __instance.HasScene ? "larata_ally" : null)];
                ModEntry.Log.Info($"DIRECTOR boss-duo main={__instance.Id.Entry} ally={extra.Id.Entry} hp={rules.ExtraHpFactor}");
                return;
            }
            ModEntry.Log.Warn("DIRECTOR no compatible unseen earlier boss; keeping original finale.");
            return;
        }
        int tier = __instance.RoomType == RoomType.Boss ? 2 : __instance.RoomType == RoomType.Elite ? 1 : 0;
        var plan = EncounterPlan.Roll(n => rng.NextInt(n), runState.Players.Count, tier,
            EncounterDirector.CanAddNormal(__instance, monsters), __instance is TwoTailedRatsNormal);
        var state = new EncounterRules(plan, leader);
        if (plan.Reinforcement)
        {
            MonsterModel extra;
            string? slot = null;
            if (__instance is TwoTailedRatsNormal)
            {
                if (monsters.Any(x => x.Item2 == "second") || !__instance.Slots.Contains("second"))
                    throw new InvalidOperationException("LaRata: rat formation incompatible with another mod.");
                extra = ModelDb.Monster<TwoTailedRat>().ToMutable();
                ((TwoTailedRat)extra).StarterMoveIndex = 0;
                slot = "second"; // First stays free for native summons.
            }
            else
            {
                var pool = monsters.Where(x => x.Item1 is Inklet or LeafSlimeS or Nibbit or Flyconid or CorpseSlug or FuzzyWurmCrawler).ToArray();
                extra = pool[rng.NextInt(pool.Length)].Item1.CanonicalInstance.ToMutable();
            }
            state.Extra = extra;
            ____monstersWithSlots = [.. monsters, (extra, slot)];
        }
        EncounterDirector.Rules.Add(__instance, state);
        ModEntry.Log.Info($"DIRECTOR floor={runState.TotalFloor} encounter={__instance.Id.Entry} players={runState.Players.Count} budget={plan.Spent}/{plan.Budget} leader={leader.Id.Entry} rules={string.Join("; ", plan.Affixes)}");
    }
}

// Native console 'fight' otherwise seeds from wall-clock seconds, independently on every peer.
// Keeping its encounter RNG unset lets the normal run-seeded path handle debug fights as well.
[HarmonyPatch(typeof(EncounterModel), nameof(EncounterModel.DebugRandomizeRng))]
internal static class DeterministicDebugFightPatch
{
    [HarmonyPrefix] private static bool Prefix() => false;
}
