using System.Runtime.CompilerServices;
using System.Threading;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.TreasureRelicPicking;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;

namespace LaRata;

// Presentation of already synchronized choices. Native hand tweens use cosmetic
// RNG only; the winner never depends on animation timing or frame rate.
internal sealed class RelicContestPresentation : IDisposable
{
    internal static NTreasureRoomRelicCollection? Current;
    internal static readonly ConditionalWeakTable<RelicPickingResult, object> Animated = new();
    private readonly NHandImageCollection hands;
    private readonly Control backdrop;
    private readonly MegaLabel label;
    private readonly string originalLabel;
    private readonly List<Player> players;

    internal RelicContestPresentation(List<Player> players)
    {
        var screen = Current ?? throw new InvalidOperationException("Native treasure presentation unavailable.");
        hands = screen.GetNode<NHandImageCollection>("%HandsContainer");
        backdrop = screen.GetNode<Control>("%FightBackstop");
        label = screen.GetNode<MegaLabel>("%FightLabel");
        originalLabel = label.Text;
        this.players = players;
        hands.BeforeRelicsAwarded();
        HideHands();
    }

    internal void HideHands()
    {
        foreach (var hand in hands.GetChildren().OfType<NHandImage>()) hand.Hide();
    }

    internal async Task AnimateRound(List<Player> contenders, int[] moves, List<Player> survivors, int number, CancellationToken cancellation)
    {
        ChoicePanel.Close();
        backdrop.Show();
        backdrop.Modulate = Colors.White;
        label.SetTextAutoSize(UiText.Format("contest.round", number));
        hands.BeforeFightStarted(contenders);
        foreach (var p in contenders) hands.GetHand(p.NetId)!.Show();
        await Cmd.Wait(.35f, cancellation);
        await Task.WhenAll(contenders.Select((p, i) =>
            hands.GetHand(p.NetId)!.DoFightMove((RelicPickingFightMove)moves[i], 1.5f).AwaitFinished(cancellation)));
        for (int i = 0; i < contenders.Count; i++)
            if (ModEntry.AuditChecksums) ModEntry.Log.Info($"RPS animated round={number} player={contenders[i].NetId} move={(RelicPickingFightMove)moves[i]}");
        await Cmd.Wait(.7f, cancellation);
        await Task.WhenAll(contenders.Where(p => !survivors.Contains(p)).Select(p => hands.GetHand(p.NetId)!.DoLoseShake(.7f)));
        label.SetTextAutoSize(UiText.Get(survivors.Count == 1 ? "contest.decided" : survivors.Count == contenders.Count ? "contest.tie" : "contest.next"));
        await Cmd.Wait(.7f, cancellation);
        foreach (var p in contenders) hands.GetHand(p.NetId)!.SetIsInFight(false);
        backdrop.Hide();
        HideHands();
    }

    public void Dispose()
    {
        if (!GodotObject.IsInstanceValid(hands)) return;
        backdrop.Hide();
        backdrop.Modulate = new Color(1, 1, 1, 0);
        label.SetTextAutoSize(originalLabel);
        foreach (var hand in hands.GetChildren().OfType<NHandImage>())
        {
            hand.SetIsInFight(false);
            hand.Show();
        }
    }

}

[HarmonyPatch(typeof(NTreasureRoomRelicCollection), nameof(NTreasureRoomRelicCollection._Ready))]
internal static class RegisterTreasurePresentationPatch
{
    [HarmonyPostfix]
    static void Postfix(NTreasureRoomRelicCollection __instance) => RelicContestPresentation.Current = __instance;
}

[HarmonyPatch(typeof(NTreasureRoomRelicCollection), nameof(NTreasureRoomRelicCollection._ExitTree))]
internal static class UnregisterTreasurePresentationPatch
{
    [HarmonyPrefix]
    static void Prefix(NTreasureRoomRelicCollection __instance)
    {
        if (ReferenceEquals(RelicContestPresentation.Current, __instance)) RelicContestPresentation.Current = null;
    }
}

[HarmonyPatch(typeof(NTreasureRoomRelicCollection), "AnimateRelicAwards")]
internal static class AlreadyAnimatedRelicFightPatch
{
    [HarmonyPrefix]
    static void Prefix(ref List<RelicPickingResult> results)
    {
        // Only the native animation gets a presentation copy. The synchronized
        // contest result stays FoughtOver with its complete chosen-hand history.
        // The native uncontested animation already does exactly what remains:
        // winner grabs relic, ownership/consolation and completion run normally.
        // Skipping DoFight alone would still flash the enclosing "Fight!" banner.
        results = results.Select(result => RelicContestPresentation.Animated.TryGetValue(result, out _)
            ? new RelicPickingResult { relic = result.relic, player = result.player, fight = result.fight,
                type = RelicPickingResultType.OnlyOnePlayerVoted }
            : result).ToList();
    }
}
