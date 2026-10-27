using System.Text.Json;
using BaseLib.Patches.Saves;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace LaRata;

internal sealed record RelicRunMetric(string Key, decimal Value, string Unit, string Attribution);
internal sealed record RelicRunStats(ulong? OwnerId, int? AcquisitionOrdinal, string ModelId,
    string Coverage, IReadOnlyList<RelicRunMetric> Metrics, IReadOnlyList<string> Gaps, bool Removed);

// Observations belong to the native relic instance and therefore to the same save/checkpoint.
// No profile-wide sidecar, local GUID, UI-owned counter or second network update exists.
internal static class RelicMetrics
{
    // Disabled for every peer: UI-local calculations must not mutate synchronized stats.
    // Retain saved fields for existing runs and instance IDs used by Relic Remover.
    internal static bool Enabled => false;
    internal const string PatchCategory = "LaRataRelicStatistics";
    private sealed record Ledger(int Version, string Coverage, RelicRunMetric[] Metrics, string[]? Deferred = null);
    internal static readonly SavedSpireField<RelicModel, int> Acquisition = new(() => 0, "LaRataRelicAcquisitionV1");
    private static readonly SavedSpireField<RelicModel, string> Values = new(() => "", "LaRataRelicValuesV1");
    private static readonly SavedSpireField<Player, int> NextAcquisition = new(() => 0, "LaRataRelicSequenceV1");
    private static readonly SavedSpireField<IRunState, string> Removed = new(() => "", "LaRataRemovedRelicValuesV1");
    internal static void Register()
    { _ = Acquisition; _ = Values; _ = NextAcquisition; _ = Removed; RelicDeferredMetrics.Register(); }

    internal static int Identity(RelicModel relic)
    {
        // Observing a preview, tooltip, clone or checksum must not allocate IDs.
        return relic.IsCanonical ? 0 : Acquisition.Get(relic);
    }

    private static void AdvanceSequence(Player player)
    {
        int high = NextAcquisition.Get(player);
        foreach (var owned in player.Relics) high = Math.Max(high, Acquisition.Get(owned));
        if (player.RunState is not NullRunState)
            foreach (var archived in ReadRemoved(player.RunState).Where(r => r.OwnerId == player.NetId))
                high = Math.Max(high, archived.AcquisitionOrdinal ?? 0);
        NextAcquisition.Set(player, high);
    }

    private static void AssignCommittedIdentity(RelicModel relic)
    {
        var player = relic.Owner;
        AdvanceSequence(player);
        if (Identity(relic) > 0) return; // Imported reward IDs also advance the owner sequence.
        int next = checked(NextAcquisition.Get(player) + 1);
        NextAcquisition.Set(player, next);
        Acquisition.Set(relic, next);
    }

    // Native combat sync replaces an existing Player's inventory without calling
    // Player.FromSerializable, the only player-load entry patched by BaseLib.
    // Reuse this field's existing BaseLib payload; do not add a second save format.
    internal static void RestorePlayer(Player player, SerializablePlayer save)
    {
        var fields = ExtendedSaveHandlers<Player, SerializablePlayer>.ExtendedData[save].DictForType<int>();
        NextAcquisition.Set(player, fields.GetValueOrDefault("spirefield_" + NextAcquisition.Name));
        AdvanceSequence(player);
        // Legacy inventories without IDs are initialized in native inventory order,
        // at this load boundary. Existing IDs and their ledgers are never renumbered.
        foreach (var relic in player.Relics) AssignCommittedIdentity(relic);
    }

    internal static void Obtained(RelicModel relic)
    {
        if (relic.IsCanonical || relic.Owner?.RunState is null or NullRunState ||
            !relic.Owner.Relics.Any(r => ReferenceEquals(r, relic))) return;
        AssignCommittedIdentity(relic);
        if (!Enabled) return;
        // Called only for a new native Obtain, never for loading an existing inventory.
        Values.Set(relic, JsonSerializer.Serialize(new Ledger(1, "Complete", [])));
    }

    internal static RelicRunStats Read(RelicModel relic)
    {
        if (relic.IsCanonical || relic.Owner == null)
            return new(null, null, relic.Id.ToString(), "Unavailable", [], [], false);
        var ledger = Decode(Values.Get(relic));
        return new(relic.Owner.NetId, Acquisition.Get(relic) is var id && id > 0 ? id : null,
            relic.Id.ToString(), ledger?.Coverage ?? "Unavailable", ledger?.Metrics ?? [],
            RelicMetricCoverage.Gaps(relic).Concat(ledger?.Deferred ?? []).Distinct(StringComparer.Ordinal)
                .OrderBy(g => g, StringComparer.Ordinal).ToArray(), relic.HasBeenRemovedFromState);
    }

    internal static void Record(RelicModel? relic, string key, decimal value, string unit,
        string attribution = "Exact")
        => RecordValue(relic, key, value, unit, attribution, false);

    internal static void RecordCommittedRemoval(RelicModel? source)
        => RecordValue(source, "relics_removed", 1, "Relics", "Exact", true);

    private static void RecordValue(RelicModel? relic, string key, decimal value, string unit,
        string attribution, bool allowJustRemoved)
    {
        if (!Enabled) return;
        if (relic == null && RelicMetricSource.Attribution is { Live: null } archived)
        { RecordTarget(archived, key, value, unit, attribution); return; }
        if (relic == null || value == 0 || relic.IsCanonical || (relic.IsMelted && !allowJustRemoved) ||
            (relic.HasBeenRemovedFromState && !allowJustRemoved && !ReferenceEquals(RelicMetricSource.Relic, relic)) || Identity(relic) == 0) return;
        var ledger = Decode(Values.Get(relic)) ?? new Ledger(1, "SinceTrackingStarted", []);
        var metrics = ledger.Metrics.ToDictionary(m => (m.Key, m.Unit, m.Attribution));
        var metricKey = (key, unit, attribution);
        metrics.TryGetValue(metricKey, out var old);
        metrics[metricKey] = new(key, (old?.Value ?? 0) + value, unit, attribution);
        Values.Set(relic, JsonSerializer.Serialize(ledger with
        {
            Metrics = metrics.Values.OrderBy(m => m.Key, StringComparer.Ordinal)
                .ThenBy(m => m.Unit, StringComparer.Ordinal).ThenBy(m => m.Attribution, StringComparer.Ordinal).ToArray()
        }));
        // A replacement can remove its own source before granting the replacement.
        // Finish attribution for that already-running effect, but Enter never
        // accepts a removed relic as the source of a newly invoked effect.
        if (relic.HasBeenRemovedFromState) Archive(relic);
    }

    // Deferred rewards can outlive their source relic and survive a checkpoint.
    // Update its existing archive entry; never fabricate a new relic or identity.
    internal static void RecordTarget(RelicMetricSource.Target? target, string key, decimal value,
        string unit, string attribution = "Exact")
    {
        if (!Enabled || target == null || value == 0) return;
        if (target.Live is { } live)
        { RecordValue(live, key, value, unit, attribution, true); return; }
        var entries = DecodeArchive(Removed.Get(target.Owner.RunState));
        int index = entries.FindIndex(e => e.OwnerId == target.Owner.NetId &&
            e.AcquisitionOrdinal == target.Acquisition && e.ModelId == target.ModelId);
        if (index < 0) return;
        var snapshot = entries[index];
        var metrics = snapshot.Metrics.ToDictionary(m => (m.Key, m.Unit, m.Attribution));
        var metricKey = (key, unit, attribution);
        metrics.TryGetValue(metricKey, out var previous);
        metrics[metricKey] = new(key, (previous?.Value ?? 0) + value, unit, attribution);
        entries[index] = snapshot with { Metrics = metrics.Values.OrderBy(m => m.Key, StringComparer.Ordinal)
            .ThenBy(m => m.Unit, StringComparer.Ordinal).ThenBy(m => m.Attribution, StringComparer.Ordinal).ToArray() };
        Removed.Set(target.Owner.RunState, JsonSerializer.Serialize(entries.OrderBy(e => e.OwnerId)
            .ThenBy(e => e.AcquisitionOrdinal).ToArray()));
    }

    // An explicitly deferred outcome is a coverage flag, never a made-up zero
    // metric. It shares the existing native ledger/checkpoint and archive.
    internal static void MarkDeferred(RelicMetricSource.Target? target, string key)
    {
        if (!Enabled || target == null) return;
        if (target.Live is { } live)
        {
            var ledger = Decode(Values.Get(live)) ?? new Ledger(1, "SinceTrackingStarted", []);
            Values.Set(live, JsonSerializer.Serialize(ledger with
            {
                Deferred = (ledger.Deferred ?? []).Append(key).Distinct(StringComparer.Ordinal)
                    .OrderBy(g => g, StringComparer.Ordinal).ToArray()
            }));
            if (live.HasBeenRemovedFromState) Archive(live);
            return;
        }
        var entries = DecodeArchive(Removed.Get(target.Owner.RunState));
        int index = entries.FindIndex(e => e.OwnerId == target.Owner.NetId &&
            e.AcquisitionOrdinal == target.Acquisition && e.ModelId == target.ModelId);
        if (index < 0) return;
        entries[index] = entries[index] with
        {
            Gaps = entries[index].Gaps.Append(key).Distinct(StringComparer.Ordinal)
                .OrderBy(g => g, StringComparer.Ordinal).ToArray()
        };
        Removed.Set(target.Owner.RunState, JsonSerializer.Serialize(entries.OrderBy(e => e.OwnerId)
            .ThenBy(e => e.AcquisitionOrdinal).ToArray()));
    }

    internal static void Archive(RelicModel relic)
    {
        if (!Enabled) return;
        if (relic.IsCanonical || relic.Owner?.RunState is null or NullRunState) return;
        var run = relic.Owner.RunState;
        var entries = DecodeArchive(Removed.Get(run));
        var snapshot = Read(relic) with { Removed = true };
        if (snapshot.AcquisitionOrdinal == null) return;
        // Older affected saves can contain the same ordinal on different models.
        // Preserve both historical ledgers rather than overwriting one of them.
        entries.RemoveAll(e => e.OwnerId == snapshot.OwnerId && e.AcquisitionOrdinal == snapshot.AcquisitionOrdinal &&
            e.ModelId == snapshot.ModelId);
        entries.Add(snapshot);
        Removed.Set(run, JsonSerializer.Serialize(entries.OrderBy(e => e.OwnerId).ThenBy(e => e.AcquisitionOrdinal).ToArray()));
    }

    internal static IReadOnlyList<RelicRunStats> ReadRemoved(IRunState run) => DecodeArchive(Removed.Get(run));

    private static Ledger? Decode(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            var ledger = JsonSerializer.Deserialize<Ledger>(json);
            return ledger is { Version: 1, Metrics: not null } ? ledger : null;
        }
        catch (JsonException) { return null; }
    }

    private static List<RelicRunStats> DecodeArchive(string? json)
    {
        if (string.IsNullOrEmpty(json)) return [];
        try { return JsonSerializer.Deserialize<List<RelicRunStats>>(json) ?? []; }
        catch (JsonException) { return []; }
    }
}
