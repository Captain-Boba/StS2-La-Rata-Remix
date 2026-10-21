using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace LaRata;

// Native popup artwork, fonts, buttons, sounds and focus behavior.
// Decisions still travel through PlayerChoiceSynchronizer.
internal static class ChoicePanel
{
    private static NGenericPopup? modal;
    private static NVerticalPopup? popup;
    private static TaskCompletionSource<int>? pending;
    private static NPopupYesNoButton? defaultButton;
    internal static bool IsOpen => GodotObject.IsInstanceValid(modal);
    private static bool restoreCursor;
    private static readonly AccessTools.FieldRef<NCursorManager, bool> CursorShown =
        AccessTools.FieldRefAccess<NCursorManager, bool>("_shouldShowCursor");
    internal static bool Owns(NGenericPopup instance) => ReferenceEquals(instance, modal);
    internal static Control? DefaultFocus => GodotObject.IsInstanceValid(defaultButton) && defaultButton!.IsEnabled ? defaultButton : null;

    internal static void Show(string heading, string detail)
    {
        Close();
        restoreCursor = CursorShown(NGame.Instance!.CursorManager);
        modal = NGenericPopup.Create() ?? throw new InvalidOperationException("Native popup unavailable.");
        popup = modal.GetNode<NVerticalPopup>("VerticalPopup");
        NModalContainer.Instance!.Add(modal);
        popup.SetText(heading, detail);
        popup.YesButton.Hide();
        popup.NoButton.Hide();
        popup.YesButton.DisconnectHotkeys();
        popup.NoButton.DisconnectHotkeys();
        NGame.Instance!.CursorManager.SetCursorShown(true);
    }

    internal static Task<int> Choose(string heading, string detail, params string[] options)
    {
        Show(heading, detail);
        var source = new TaskCompletionSource<int>();
        pending = source;
        var buttons = new List<NPopupYesNoButton>();
        var template = popup!.YesButton;
        // Three native 72px buttons plus their original 96px visual bounds.
        // Reserve a distinct description region above them.
        var description = popup.GetNode<MegaRichTextLabel>("Description");
        description.Size = new Vector2(description.Size.X, 220);
        for (int i = 0; i < options.Length; i++)
        {
            int choice = i;
            var button = (NPopupYesNoButton)template.Duplicate((int)(Node.DuplicateFlags.Scripts | Node.DuplicateFlags.UseInstantiation));
            button.Name = $"LaRataChoice{i}";
            button.GetNodeOrNull<Control>("ControllerIcon")?.Free();
            popup.AddChild(button);
            button.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
            button.Position = new Vector2((popup.Size.X - template.Size.X) / 2, popup.Size.Y - 285 + i * 80);
            button.Size = template.Size;
            button.Show();
            button.DisconnectHotkeys(); // Only the focused button handles selection.
            button.SetText(options[i]);
            buttons.Add(button);
            button.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
            {
                if (source.Task.IsCompleted) return;
                foreach (var other in buttons) other.Disable();
                popup.SetText(heading, $"{options[choice]} locked in.\nWaiting for the other contenders...");
                pending = null;
                source.TrySetResult(choice);
            }));
        }
        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].FocusNeighborTop = buttons[i].GetPathTo(buttons[(i + buttons.Count - 1) % buttons.Count]);
            buttons[i].FocusNeighborBottom = buttons[i].GetPathTo(buttons[(i + 1) % buttons.Count]);
            buttons[i].FocusNext = buttons[i].GetPathTo(buttons[(i + 1) % buttons.Count]);
            buttons[i].FocusPrevious = buttons[i].GetPathTo(buttons[(i + buttons.Count - 1) % buttons.Count]);
        }
        defaultButton = buttons[0];
        buttons[0].GrabFocus();
        return source.Task;
    }

    internal static void Close()
    {
        bool wasOpen = IsOpen;
        pending?.TrySetCanceled();
        pending = null;
        defaultButton = null;
        if (GodotObject.IsInstanceValid(modal))
        {
            if (ReferenceEquals(NModalContainer.Instance?.OpenModal, modal)) NModalContainer.Instance!.Clear();
            else modal!.QueueFree();
        }
        modal = null;
        popup = null;
        if (wasOpen) NGame.Instance?.CursorManager.SetCursorShown(restoreCursor);
    }
}

// SharedRelicPicking screen updates can hide the native cursor after our popup
// opens. Override only that visibility request while the choice modal exists;
// the native manager still handles controller input and cursor artwork.
[HarmonyPatch(typeof(NCursorManager), nameof(NCursorManager.SetCursorShown))]
internal static class RelicChoiceCursorPatch
{
    [HarmonyPrefix] private static void Prefix(ref bool show)
    {
        if (ChoicePanel.IsOpen) show = true;
    }
}

[HarmonyPatch(typeof(NGenericPopup), nameof(NGenericPopup.DefaultFocusedControl), MethodType.Getter)]
internal static class RelicChoiceDefaultFocusPatch
{
    [HarmonyPrefix]
    static bool Prefix(NGenericPopup __instance, ref Control? __result)
    {
        if (!ChoicePanel.Owns(__instance)) return true;
        __result = ChoicePanel.DefaultFocus;
        return false;
    }
}
