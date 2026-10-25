using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace LaRata;

// Selection only. The caller owns synchronization, snapshot validation and removal.
internal static class RelicRemovalView
{
    private static NGenericPopup? modal;
    private static Control? focused;
    private static readonly AccessTools.FieldRef<NCursorManager, bool> CursorShown =
        AccessTools.FieldRefAccess<NCursorManager, bool>("_shouldShowCursor");

    internal static Task<IReadOnlyList<int>?> Choose(Player owner, IReadOnlyList<RelicModel> snapshot)
    {
        if (modal != null || NModalContainer.Instance?.OpenModal != null)
            throw new InvalidOperationException("Cannot open relic selection over another modal.");
        if (snapshot.Any(r => !ReferenceEquals(r.Owner, owner)))
            throw new ArgumentException("Relic selection can only show the owner's relics.", nameof(snapshot));

        var completion = new TaskCompletionSource<IReadOnlyList<int>?>();
        var popup = NGenericPopup.Create() ?? throw new InvalidOperationException("Native relic selection popup unavailable.");
        modal = popup;
        var cursor = NGame.Instance!.CursorManager;
        bool restoreCursor = CursorShown(cursor);
        var panel = popup.GetNode<NVerticalPopup>("VerticalPopup");
        NModalContainer.Instance!.Add(popup);
        panel.SetText(UiText.Get("removal.title"), UiText.Get("removal.detail"));
        panel.GetNode<Control>("Description").Size = new Vector2(panel.Size.X - 80, 112);
        var selected = new SortedSet<int>();
        panel.YesButton.SetText(UiText.Format("removal.confirm", 0));
        panel.YesButton.Show();
        panel.YesButton.Disable();
        panel.NoButton.SetText(UiText.Get("common.cancel"));
        panel.NoButton.Show();
        panel.NoButton.Enable();
        panel.YesButton.DisconnectHotkeys();
        panel.NoButton.DisconnectHotkeys();
        cursor.SetCursorShown(true);

        var scroll = new ScrollContainer
        {
            Position = new Vector2(40, 205), Size = new Vector2(panel.Size.X - 80, 218),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        panel.AddChild(scroll);
        var rows = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        rows.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(rows);
        var buttons = new List<NButton>();
        for (int i = 0; i < snapshot.Count; i++)
        {
            int index = i;
            var relic = snapshot[index];
            var button = new NButton
            {
                CustomMinimumSize = new Vector2(0, 62), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                FocusMode = Control.FocusModeEnum.All
            };
            rows.AddChild(button);
            buttons.Add(button);
            var surface = new ColorRect { Color = new Color("26382F"), MouseFilter = Control.MouseFilterEnum.Ignore };
            button.AddChild(surface);
            surface.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            var icon = new TextureRect
            {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Texture = relic.Icon, Position = new Vector2(30, 7), Size = new Vector2(48, 48), MouseFilter = Control.MouseFilterEnum.Ignore
            };
            button.AddChild(icon);
            var title = new Label
            {
                Position = new Vector2(86, 8), Size = new Vector2(panel.Size.X - 210, 48),
                Text = UiText.Format("removal.row", "☐", relic.Title.GetFormattedText()),
                VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore,
                TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis
            };
            title.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_regular_shared.tres"));
            title.AddThemeFontSizeOverride("font_size", 22);
            UiText.ApplyFont(title);
            button.AddChild(title);
            void Tips(bool show)
            {
                NHoverTipSet.Remove(button);
                if (show) NHoverTipSet.CreateAndShow(button, relic.HoverTips)?.Reparent(popup);
            }
            button.MouseEntered += () => Tips(true);
            button.MouseExited += () => Tips(false);
            button.FocusEntered += () => { focused = button; scroll.EnsureControlVisible(button); Tips(true); };
            button.FocusExited += () => Tips(false);
            button.TreeExiting += () => NHoverTipSet.Remove(button);
            button.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
            {
                if (!selected.Add(index)) selected.Remove(index);
                title.Text = UiText.Format("removal.row", selected.Contains(index) ? "☑" : "☐", relic.Title.GetFormattedText());
                surface.Color = new Color(selected.Contains(index) ? "41674F" : "26382F");
                panel.YesButton.SetText(UiText.Format("removal.confirm", selected.Count));
                if (selected.Count > 0) panel.YesButton.Enable(); else panel.YesButton.Disable();
            }));
        }

        void Finish(IReadOnlyList<int>? result)
        {
            if (!completion.TrySetResult(result)) return;
            if (ReferenceEquals(NModalContainer.Instance?.OpenModal, popup)) NModalContainer.Instance!.Clear();
            else if (GodotObject.IsInstanceValid(popup)) popup.QueueFree();
        }
        panel.YesButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
        {
            if (selected.Count > 0) Finish(selected.ToArray());
        }));
        panel.NoButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => Finish(null)));
        popup.TreeExiting += () =>
        {
            modal = null;
            focused = null;
            completion.TrySetResult(null);
            if (GodotObject.IsInstanceValid(cursor)) cursor.SetCursorShown(restoreCursor);
        };
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].FocusNeighborTop = buttons[i].GetPathTo(i == 0 ? panel.NoButton : buttons[i - 1]);
            buttons[i].FocusNeighborBottom = buttons[i].GetPathTo(i == buttons.Count - 1 ? panel.NoButton : buttons[i + 1]);
        }
        panel.NoButton.FocusNeighborTop = panel.NoButton.GetPathTo(buttons.LastOrDefault() ?? (Control)panel.YesButton);
        focused = buttons.FirstOrDefault() ?? (Control)panel.NoButton;
        focused.GrabFocus();
        return completion.Task;
    }

    [HarmonyPatch(typeof(NGenericPopup), nameof(NGenericPopup.DefaultFocusedControl), MethodType.Getter)]
    private static class SelectionFocus
    {
        [HarmonyPrefix] private static bool Prefix(NGenericPopup __instance, ref Control? __result)
        {
            if (!ReferenceEquals(__instance, modal)) return true;
            __result = focused;
            return false;
        }
    }

    [HarmonyPatch(typeof(NCursorManager), nameof(NCursorManager.SetCursorShown))]
    private static class SelectionCursor
    {
        [HarmonyPrefix] private static void Prefix(ref bool show)
        {
            if (GodotObject.IsInstanceValid(modal)) show = true;
        }
    }
}
