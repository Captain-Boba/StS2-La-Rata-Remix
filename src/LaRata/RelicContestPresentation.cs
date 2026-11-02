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
    private readonly NTreasureRoomRelicCollection screen;
    private readonly CancellationTokenSource sceneExit = new();
    private readonly Callable onSceneExit;
    private readonly List<Tween> wellTweens = [];
    private Control? wellOverlay;
    private bool disposed;

    internal RelicContestPresentation(List<Player> players)
    {
        screen = Current ?? throw new InvalidOperationException("Native treasure presentation unavailable.");
        hands = screen.GetNode<NHandImageCollection>("%HandsContainer");
        backdrop = screen.GetNode<Control>("%FightBackstop");
        label = screen.GetNode<MegaLabel>("%FightLabel");
        originalLabel = label.Text;
        this.players = players;
        onSceneExit = Callable.From(() =>
        {
            sceneExit.Cancel();
            RelicContest.CancelPresentation();
        });
        screen.Connect(Node.SignalName.TreeExiting, onSceneExit);
        hands.BeforeRelicsAwarded();
        HideHands();
    }

    internal void HideHands()
    {
        foreach (var hand in hands.GetChildren().OfType<NHandImage>()) hand.Hide();
    }

    internal async Task AnimateRound(List<Player> contenders, int[] moves, List<Player> survivors, int number, CancellationToken cancellation)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, sceneExit.Token);
        cancellation = linked.Token;
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

    // Hand textures are cosmetic symbols, not fabricated synchronized choices.
    // The Well branch has already chosen its winner using the existing rule.
    internal async Task AnimateWell(List<Player> contenders, List<Player> wells, Player winner,
        string winnerLabel, string relicTitle, CancellationToken cancellation)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, sceneExit.Token);
        cancellation = linked.Token;
        cancellation.ThrowIfCancellationRequested();
        ChoicePanel.Close();
        backdrop.Show();
        backdrop.Modulate = Colors.White;
        label.SetTextAutoSize(UiText.Get("contest.well_title"));
        wellOverlay = new Control { Name = "LaRataWellContest", MouseFilter = Control.MouseFilterEnum.Stop, ZIndex = 2 };
        // The native collection itself is only 900x580; its backdrop spans the
        // whole room, preserving normal native font/icon sizes at every viewport.
        backdrop.AddChild(wellOverlay);
        wellOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var canvas = new Control { Size = new Vector2(1920, 1080), MouseFilter = Control.MouseFilterEnum.Ignore };
        wellOverlay.AddChild(canvas);
        void Resize()
        {
            float scale = Math.Min(wellOverlay.Size.X / canvas.Size.X, wellOverlay.Size.Y / canvas.Size.Y);
            canvas.Scale = Vector2.One * scale;
            canvas.Position = (wellOverlay.Size - canvas.Size * scale) / 2;
        }
        wellOverlay.Connect(Control.SignalName.Resized, Callable.From(Resize));
        Resize();
        Vector2 area = canvas.Size;
        Vector2 center = area * new Vector2(.5f, .48f);
        var texture = GD.Load<Texture2D>("res://LaRata/images/well.png");
        TextureRect Icon(Texture2D image, Vector2 position, Vector2 size)
        {
            var icon = new TextureRect { Texture = image, Position = position, Size = size,
                PivotOffset = size / 2, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore };
            canvas.AddChild(icon);
            return icon;
        }
        Label Caption(string text, Vector2 position, Vector2 size)
        {
            var caption = new Label { Text = text, Position = position, Size = size,
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore };
            caption.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_regular_shared.tres"));
            caption.AddThemeFontSizeOverride("font_size", 26);
            UiText.ApplyFont(caption);
            canvas.AddChild(caption);
            return caption;
        }
        Tween Animate(Node owner)
        {
            var tween = owner.CreateTween();
            wellTweens.Add(tween);
            return tween;
        }
        var well = Icon(texture, center - new Vector2(125, 125), new Vector2(250, 250));
        well.ZIndex = 2;
        well.Scale = Vector2.Zero;
        Caption(wells.Count == 1 ? UiText.Get("contest.well_swallow") : UiText.Format("contest.well_multiple", wells.Count),
            center + new Vector2(-300, 135), new Vector2(600, 75));
        var opponents = contenders.Where(p => !wells.Contains(p)).ToList();
        var symbols = opponents.Select((p, i) => Icon((i % 3) switch
        {
            0 => p.Character.ArmRockTexture,
            1 => p.Character.ArmPaperTexture,
            _ => p.Character.ArmScissorsTexture
        }, new Vector2(area.X * (i + 1f) / (opponents.Count + 1), area.Y * .23f) - new Vector2(65, 65), new Vector2(130, 130))).ToList();
        var tokens = wells.Select((p, i) =>
        {
            float x = area.X * .5f + (i - (wells.Count - 1) / 2f) * 220;
            var token = Icon(texture, new Vector2(x - 42, area.Y * .77f), new Vector2(84, 84));
            var name = Caption(UiText.Format("contest.player", p.RunState.Players.ToList().IndexOf(p) + 1, p.Character.Title.GetFormattedText()),
                new Vector2(x - 105, area.Y * .77f + 86), new Vector2(210, 75));
            return (Player: p, Icon: token, Name: name);
        }).ToList();
        var appear = Animate(well);
        appear.TweenProperty(well, "scale", Vector2.One, .3).SetTrans(Tween.TransitionType.Back);
        await appear.AwaitFinished(cancellation);
        await Task.WhenAll(symbols.Select((symbol, i) =>
        {
            var tween = Animate(symbol);
            tween.TweenInterval(i * .08);
            tween.TweenProperty(symbol, "position", center - symbol.Size / 2, .55).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            tween.Parallel().TweenProperty(symbol, "scale", Vector2.One * .05f, .55);
            tween.Parallel().TweenProperty(symbol, "modulate:a", 0f, .55);
            return tween.AwaitFinished(cancellation);
        }));
        cancellation.ThrowIfCancellationRequested();
        foreach (var token in tokens)
        {
            bool won = ReferenceEquals(token.Player, winner);
            token.Name.Modulate = won ? new Color("FFD66B") : new Color(1, 1, 1, .45f);
            var tween = Animate(token.Icon).SetParallel();
            tween.TweenProperty(token.Icon, "scale", Vector2.One * (won ? 1.2f : .85f), .25);
            tween.TweenProperty(token.Icon, "modulate", won ? Colors.White : new Color(1, 1, 1, .3f), .25);
        }
        label.SetTextAutoSize(UiText.Get("contest.decided"));
        Caption(UiText.Format("contest.well_awarded", winnerLabel, relicTitle),
            new Vector2(area.X * .15f, area.Y * .08f), new Vector2(area.X * .7f, 110));
        await Cmd.Wait(.75f, cancellation);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        sceneExit.Cancel();
        if (GodotObject.IsInstanceValid(screen) && screen.IsConnected(Node.SignalName.TreeExiting, onSceneExit))
            screen.Disconnect(Node.SignalName.TreeExiting, onSceneExit);
        foreach (var tween in wellTweens)
            if (GodotObject.IsInstanceValid(tween) && tween.IsValid()) tween.Kill();
        if (GodotObject.IsInstanceValid(wellOverlay)) wellOverlay!.QueueFree();
        sceneExit.Dispose();
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
