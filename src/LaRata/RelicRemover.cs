using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace LaRata;

[Pool(typeof(SharedRelicPool))]
public sealed class RelicRemover : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool HasUponPickupEffect => true;
    public override bool IsUsedUp => Used;
    public override string PackedIconPath => "res://LaRata/images/relics/relic_remover.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    public override List<(string, string)> Localization => [];
    [SavedProperty] public bool Used { get; set; }
    [SavedProperty] public int SelectionSequence { get; set; }
    private bool opening;

    public override Task AfterObtained() => Used || opening ? Task.CompletedTask : Open(SelectionSequence + 1, false);

    internal static bool CanReopen(RelicRemover relic) => !relic.IsCanonical && !relic.Used &&
        !relic.opening && !relic.HasBeenRemovedFromState && !relic.IsMelted &&
        relic.Owner?.RunState is not null and not NullRunState &&
        relic.Owner.Relics.Contains(relic) && LocalContext.IsMe(relic.Owner) &&
        !CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsEnding;

    internal static void RequestOpen(RelicRemover relic)
    {
        if (!CanReopen(relic)) return;
        int acquisition = RelicMetrics.Acquisition.Get(relic);
        if (acquisition <= 0) return; // Only a persisted, owner-bound instance is actionable.
        var request = new RelicRemoverReopenMessage
        {
            OwnerId = relic.Owner.NetId,
            Acquisition = acquisition,
            Sequence = checked(relic.SelectionSequence + 1),
            Location = relic.Owner.RunState.RunLocation
        };
        // BaseLib sends through the existing native game service. ShouldBroadcast
        // relays a client's request through the host to the other peers; the local
        // sender executes separately, as native OneOffSynchronizer does.
        CustomTargetedMessageWrapper.Send(request);
        TaskHelper.RunSafely(relic.Open(request.Sequence, true));
    }

    internal static Task ReceiveReopen(RelicRemoverReopenMessage request, ulong senderId)
    {
        var run = RunManager.Instance.DebugOnlyGetState();
        if (run == null || senderId != request.OwnerId || request.Location != run.RunLocation ||
            CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding) return Task.CompletedTask;
        var player = run.Players.FirstOrDefault(p => p.NetId == senderId);
        var relic = player?.Relics.OfType<RelicRemover>().FirstOrDefault(r =>
            request.Acquisition > 0 && RelicMetrics.Acquisition.Get(r) == request.Acquisition);
        if (relic == null || relic.Used || relic.IsMelted || relic.opening ||
            request.Sequence != relic.SelectionSequence + 1) return Task.CompletedTask;
        return relic.Open(request.Sequence, true);
    }

    private async Task Open(int sequence, bool reopened)
    {
        if (Used || opening || HasBeenRemovedFromState || sequence != SelectionSequence + 1) return;
        var run = Owner.RunState;
        var location = run.RunLocation;
        var snapshot = Owner.Relics.ToArray();
        var synchronizer = RunManager.Instance.PlayerChoiceSynchronizer;
        opening = true; // Reserve before any await; duplicated starts cannot reserve a second choice ID.
        SelectionSequence = sequence;
        try
        {
            uint choiceId = synchronizer.ReserveChoiceId(Owner);
            List<int> indexes;
            if (LocalContext.IsMe(Owner) && RunManager.Instance.NetService.Type != NetGameType.Replay)
            {
                IReadOnlyList<int>? selected;
                try { selected = await RelicRemovalView.Choose(Owner, snapshot); }
                catch (InvalidOperationException error)
                {
                    // Native pickup can occur under another modal. Keep the
                    // unspent relic available for an explicit later retry.
                    ModEntry.Log.Warn($"RELIC REMOVER selection unavailable: {error.Message}");
                    selected = null;
                }
                indexes = selected?.ToList() ?? [-1];
                synchronizer.SyncLocalChoice(Owner, choiceId, PlayerChoiceResult.FromIndexes(indexes));
            }
            else indexes = (await synchronizer.WaitForRemoteChoice(Owner, choiceId)).AsIndexes();

            // Cancel/empty selection changes no inventory or usage. There is no
            // retry-attempt cap: only the single successful effect is consumed.
            if (indexes.Count == 0 || indexes.SequenceEqual([-1])) return;
            if (!ReferenceEquals(RunManager.Instance.DebugOnlyGetState(), run) || run.RunLocation != location ||
                (reopened && (CombatManager.Instance.IsInProgress || CombatManager.Instance.IsEnding))) return;
            await RemoveConfirmed(snapshot, indexes);
        }
        finally { opening = false; }
    }

    internal async Task<bool> RemoveConfirmed(IReadOnlyList<RelicModel> snapshot, IReadOnlyList<int> indexes)
    {
        if (Used || indexes.Count == 0 || !ValidateSelection(Owner, snapshot, indexes)) return false;
        if (!Owner.Relics.Contains(this) || HasBeenRemovedFromState) return false;
        var selected = indexes.Order().Select(i => snapshot[i]).ToArray();
        Used = true; // Reentrancy guard. No pickup benefits are rolled back.
        Status = RelicStatus.Disabled;
        // Removing this very relic is permitted, but happens last so its current
        // resolution and the remaining inventory callbacks still have an owner.
        foreach (var relic in selected.Where(r => !ReferenceEquals(r, this))) await RelicCmd.Remove(relic);
        if (selected.Any(r => ReferenceEquals(r, this))) await RelicCmd.Remove(this);
        return true;
    }

    internal static bool ValidateSelection(MegaCrit.Sts2.Core.Entities.Players.Player owner,
        IReadOnlyList<RelicModel> snapshot, IReadOnlyList<int> indexes)
    {
        if (indexes.Distinct().Count() != indexes.Count) return false;
        var instances = new HashSet<RelicModel>(ReferenceEqualityComparer.Instance);
        // Validate every entry before the first native mutation. Snapshot order
        // and object identity prevent indices from silently selecting a new relic.
        foreach (int index in indexes)
        {
            if (index < 0 || index >= snapshot.Count) return false;
            var relic = snapshot[index];
            if (!instances.Add(relic) || relic.IsCanonical || relic.Owner != owner || relic.HasBeenRemovedFromState ||
                !owner.Relics.Any(r => ReferenceEquals(r, relic))) return false;
        }
        return true;
    }
}

// One domain-specific request over BaseLib's existing reliable location-targeted
// message extension. Selection results still use the native choice synchronizer.
internal sealed class RelicRemoverReopenMessage : ICustomTargetedMessage
{
    internal ulong OwnerId;
    internal int Acquisition;
    internal int Sequence;
    public RunLocation Location { get; set; }
    public bool IsRewardMessage => false;
    public bool ShouldBroadcast => true;
    public void Serialize(PacketWriter writer)
    { writer.WriteULong(OwnerId); writer.WriteInt(Acquisition); writer.WriteInt(Sequence); writer.Write(Location); }
    public void Deserialize(PacketReader reader)
    { OwnerId = reader.ReadULong(); Acquisition = reader.ReadInt(); Sequence = reader.ReadInt(); Location = reader.Read<RunLocation>(); }
    public void HandleMessage(ulong senderId) => TaskHelper.RunSafely(RelicRemover.ReceiveReopen(this, senderId));
}
