using System.Text.Json;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace LaRata;

internal sealed record JournalEntry(int Round, string Kind, string Model, ulong? Recipient);
internal sealed record JournalSnapshot(string Combat, JournalEntry[] Entries);

// Presentation-only data. BaseLib carries it with native run saves/serialization;
// entries never consume RNG, drive actions, or reveal a planned royal card.
internal static class CombatJournal
{
    internal static readonly SavedSpireField<IRunState, string> Saved = new(() => "", "LaRataCombatJournalV1");
    internal static event Action? Changed;
    internal static void Register() => _ = Saved;
    internal static string Key(ICombatState combat) =>
        $"{combat.RunState.CurrentActIndex}/{combat.RunState.TotalFloor}/{combat.Encounter?.Id}/{combat.RunState.MapPointHistory.SelectMany(a => a).Sum(p => p.Rooms.Count)}";

    internal static JournalSnapshot Decode(string? json, string key)
    {
        if (string.IsNullOrEmpty(json)) return new(key, []);
        try
        {
            var snapshot = JsonSerializer.Deserialize<JournalSnapshot>(json);
            if (snapshot?.Combat == key && snapshot.Entries != null) return snapshot;
        }
        catch (JsonException) { /* Old/malformed optional presentation data is disposable. */ }
        return new(key, []);
    }

    internal static JournalSnapshot Read(ICombatState combat) => Decode(Saved.Get(combat.RunState), Key(combat));
    internal static void Record(ICombatState combat, string kind, AbstractModel model, ulong? recipient = null)
    {
        if (CombatManager.Instance.IsEnding) return;
        var snapshot = Read(combat);
        var entry = new JournalEntry(combat.RoundNumber, kind, model.Id.ToString(), recipient);
        Saved.Set(combat.RunState, JsonSerializer.Serialize(snapshot with { Entries = [.. snapshot.Entries, entry] }));
        Changed?.Invoke();
    }

    internal static void Clear(IRunState run)
    {
        Saved.Set(run, "");
        Changed?.Invoke();
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
    private static class Begin
    {
        // Native save/load restarts an unfinished fight from its entry checkpoint.
        // Replay regenerates the log through the same actions; do not carry later
        // turns from an abandoned attempt into round one or leak future cards.
        [HarmonyPrefix] private static void Prefix(ICombatState? combatState)
        {
            if (combatState != null) Clear(combatState.RunState);
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
    private static class End
    {
        [HarmonyPrefix] private static void Prefix(IRunState runState) => Clear(runState);
    }
}
