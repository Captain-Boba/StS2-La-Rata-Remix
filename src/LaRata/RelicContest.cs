using System.Runtime.CompilerServices;
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.TreasureRelicPicking;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace LaRata;

internal static class RelicContest
{
    internal sealed class Session
    {
        internal bool Deferred;
        internal bool Resolving;
        internal bool Delivering;
        internal CancellationTokenSource Cancellation = new();
        internal Dictionary<RelicModel, RelicPickingResult> Results = new(ReferenceEqualityComparer.Instance);
    }
    internal static readonly ConditionalWeakTable<TreasureRoomRelicSynchronizer, Session> Sessions = new();
    internal static Session State(TreasureRoomRelicSynchronizer sync) => Sessions.GetOrCreateValue(sync);
    internal static bool Active => RunManager.Instance.DebugOnlyGetState() != null && State(RunManager.Instance.TreasureRoomRelicSynchronizer).Resolving;
    private static readonly System.Reflection.MethodInfo Award = AccessTools.Method(typeof(TreasureRoomRelicSynchronizer), "AwardRelics");
    private static readonly System.Reflection.MethodInfo End = AccessTools.Method(typeof(TreasureRoomRelicSynchronizer), "EndRelicVoting");
    private static string PlayerLabel(IRunState run, Player player) => $"Player {run.Players.ToList().IndexOf(player) + 1} ({player.Character.Title.GetFormattedText()})";

    internal static async Task Execute(TreasureRoomRelicSynchronizer sync, Player player, int? index)
    {
        var session = State(sync);
        if (session.Resolving) return;
        sync.OnPicked(player, index);
        if (!session.Deferred) return;
        session.Resolving = true;
        try
        {
            var run = player.RunState;
            var votes = AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_votes").GetValue(sync) as List<TreasureRoomRelicSynchronizer.PlayerVote>
                ?? throw new InvalidOperationException("La Rata: treasure vote contract changed.");
            var relics = sync.CurrentRelics!.ToArray();
            for (int i = 0; i < relics.Length; i++)
            {
                var contenders = run.Players.Where((_, p) => votes[p].index == i).ToList();
                if (contenders.Count > 1) session.Results.Add(relics[i], await Resolve(run, contenders, relics[i], i, session.Cancellation.Token));
            }
            session.Cancellation.Token.ThrowIfCancellationRequested();
            session.Delivering = true;
            Award.Invoke(sync, null);
            End.Invoke(sync, null);
        }
        catch (OperationCanceledException) when (session.Cancellation.IsCancellationRequested) { }
        finally
        {
            ChoicePanel.Close();
            Sessions.Remove(sync);
            session.Cancellation.Dispose();
        }
    }

    private static async Task<RelicPickingResult> Resolve(IRunState run, List<Player> contenders, RelicModel relic, int slot, CancellationToken cancellation)
    {
        var wells = contenders.Where(p => p.Relics.Any(r => r is Well && !r.IsMelted)).ToList();
        if (wells.Count > 0)
        {
            var rng = new Rng(run.Rng.Seed, $"LaRata/Well/v1/{run.CurrentActIndex}/{run.TotalFloor}/{slot}/{relic.Id.Entry}");
            var winner = wells[rng.NextInt(wells.Count)];
            ChoicePanel.Show("Well wins", $"{relic.Title.GetFormattedText()}\n{PlayerLabel(run, winner)} wins the contest.\n" +
                (wells.Count == 1 ? "The well swallows every hand." : $"Equal chances between {wells.Count} Well holders."));
            await Task.Delay(1800, cancellation);
            ModEntry.Log.Info($"RPS well winner={winner.NetId} contenders={contenders.Count} wells={wells.Count} relic={relic.Id}");
            return new RelicPickingResult { relic = relic, player = winner, type = RelicPickingResultType.OnlyOnePlayerVoted };
        }

        var fight = new RelicPickingFight();
        using var presentation = new RelicContestPresentation(contenders);
        fight.playersInvolved.AddRange(contenders);
        var remaining = contenders.ToList();
        int roundNumber = 0;
        while (remaining.Count > 1)
        {
            roundNumber++;
            var synchronizer = RunManager.Instance.PlayerChoiceSynchronizer;
            var ids = remaining.Select(p => synchronizer.ReserveChoiceId(p)).ToArray();
            var local = remaining.FirstOrDefault(LocalContext.IsMe);
            Task<int>? localChoice = null;
            string detail = $"{relic.Title.GetFormattedText()} · Round {roundNumber}\nChoose privately. Reveal after everyone locks in.";
            if (local != null) localChoice = ChoicePanel.Choose("Rock, Paper, Scissors", detail, "Rock", "Paper", "Scissors");
            else ChoicePanel.Show("Rock, Paper, Scissors", detail + "\nWatching the remaining contenders...");

            async Task<int> Choice(Player p, uint id)
            {
                int move;
                if (LocalContext.IsMe(p))
                {
                    move = await localChoice!.WaitAsync(cancellation);
                    synchronizer.SyncLocalChoice(p, id, PlayerChoiceResult.FromIndex(move));
                }
                else move = (await synchronizer.WaitForRemoteChoice(p, id).WaitAsync(cancellation)).AsIndex();
                if (move is < 0 or > 2) throw new InvalidOperationException("La Rata: invalid RPS choice.");
                return move;
            }
            var moves = await Task.WhenAll(remaining.Select((p, i) => Choice(p, ids[i])));
            var round = new RelicPickingFightRound();
            foreach (var p in contenders)
            {
                int i = remaining.IndexOf(p);
                round.moves.Add(i < 0 ? null : (RelicPickingFightMove)moves[i]);
            }
            fight.rounds.Add(round);
            var throwing = remaining.ToList();
            var distinct = moves.Distinct().ToArray();
            if (distinct.Length == 2)
            {
                int losing = (distinct[0] + 1) % 3 == distinct[1] ? distinct[0] : distinct[1];
                remaining = remaining.Where((_, i) => moves[i] != losing).ToList();
            }
            ModEntry.Log.Info($"RPS round={roundNumber} relic={relic.Id} moves={string.Join(',', moves)} remaining={string.Join(',', remaining.Select(p => p.NetId))}");
            await presentation.AnimateRound(throwing, moves, remaining, roundNumber, cancellation);
        }
        var result = new RelicPickingResult { relic = relic, player = remaining[0], fight = fight, type = RelicPickingResultType.FoughtOver };
        RelicContestPresentation.Animated.Add(result, new object());
        return result;
    }
}

[HarmonyPatch(typeof(PickRelicAction), "ExecuteAction")]
internal static class InteractiveRelicPickPatch
{
    [HarmonyPrefix]
    static bool Prefix(PickRelicAction __instance, Player ____player, int? ____relicIndex, ref Task __result)
    {
        if (__instance.TestSynchronizer != null || ____player.RunState.Players.Count < 2) return true;
        __result = RelicContest.Execute(RunManager.Instance.TreasureRoomRelicSynchronizer, ____player, ____relicIndex);
        return false;
    }
}

[HarmonyPatch(typeof(TreasureRoomRelicSynchronizer), "AwardRelics")]
internal static class DeferRelicAwardPatch
{
    [HarmonyPrefix]
    static bool Prefix(TreasureRoomRelicSynchronizer __instance)
    {
        if (RunManager.Instance.DebugOnlyGetState()?.Players.Count < 2) return true;
        var state = RelicContest.State(__instance);
        if (state.Delivering) return true;
        state.Deferred = true;
        return false;
    }
}

[HarmonyPatch(typeof(TreasureRoomRelicSynchronizer), "EndRelicVoting")]
internal static class DeferRelicEndPatch
{
    [HarmonyPrefix]
    static bool Prefix(TreasureRoomRelicSynchronizer __instance)
    {
        var state = RelicContest.State(__instance);
        return !state.Deferred || state.Delivering;
    }
}

[HarmonyPatch(typeof(RelicPickingResult), nameof(RelicPickingResult.GenerateRelicFight))]
internal static class ChosenRelicFightPatch
{
    [HarmonyPrefix]
    static bool Prefix(RelicModel relic, ref RelicPickingResult __result)
    {
        if (RunManager.Instance.DebugOnlyGetState() == null) return true;
        var state = RelicContest.State(RunManager.Instance.TreasureRoomRelicSynchronizer);
        if (!state.Delivering || !state.Results.TryGetValue(relic, out var result)) return true;
        __result = result;
        return false;
    }
}

[HarmonyPatch(typeof(TreasureRoomRelicSynchronizer), nameof(TreasureRoomRelicSynchronizer.PickRelicLocally))]
internal static class BlockRelicRevotePatch
{
    [HarmonyPrefix]
    static bool Prefix(TreasureRoomRelicSynchronizer __instance) => !RelicContest.State(__instance).Resolving;
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
internal static class CloseChoicePanelPatch
{
    [HarmonyPrefix]
    static void Prefix(RunManager __instance)
    {
        if (__instance.DebugOnlyGetState() != null && RelicContest.Sessions.TryGetValue(__instance.TreasureRoomRelicSynchronizer, out var session))
            session.Cancellation.Cancel();
        ChoicePanel.Close();
    }
}
