using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;

namespace LaRata;

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.Initialize))]
internal static class MapPanelPatch
{
    [HarmonyPostfix]
    private static void Postfix(NMapScreen __instance, RunState runState)
    {
        if (__instance.HasNode("LaRataPanel")) return;
        var legend = __instance.GetNode<TextureRect>("MapLegend");
        var headerSource = legend.GetNode<MegaLabel>("Header");
        var labelSource = legend.GetNode<MegaLabel>("LegendItems/UnknownLegendItem/MegaLabel");
        var panel = new TextureRect
        {
            Name = "LaRataPanel", Texture = legend.Texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        __instance.AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
        panel.OffsetLeft = -402;
        panel.OffsetRight = -62;
        panel.OffsetTop = -322;
        panel.OffsetBottom = -12;
        var title = CopyLabel(headerSource, panel, new Vector2(30, 12), new Vector2(280, 55), 26);
        var body = new Control { Position = new Vector2(30, 75), Size = new Vector2(280, 215), MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddChild(body);
        CopyLabel(labelSource, body, Vector2.Zero, new Vector2(280, 32), 22).SetTextAutoSize("Start: Bowler Hat");
        CopyLabel(labelSource, body, new Vector2(0, 32), new Vector2(280, 32), 22).SetTextAutoSize("Start: Potion Belt (+2 slots)");
        CopyLabel(labelSource, body, new Vector2(0, 64), new Vector2(280, 32), 22).SetTextAutoSize("Start: White Beast Statue");
        CopyLabel(labelSource, body, new Vector2(0, 96), new Vector2(280, 32), 22).SetTextAutoSize($"Twists: {Math.Clamp(runState.Players.Count, 1, 4)} players / Act {runState.CurrentActIndex + 1}");
        CopyLabel(labelSource, body, new Vector2(0, 128), new Vector2(280, 32), 22).SetTextAutoSize("Final boss: unseen ally");
        var detail = CopyLabel(labelSource, body, new Vector2(0, 163), new Vector2(280, 40), 21);
        void Refresh()
        {
            detail.SetTextAutoSize($"Potion slots: {string.Join(" / ", runState.Players.Select(x => x.MaxPotionCount))}");
        }
        void SetExpanded(bool expanded)
        {
            body.Visible = expanded;
            title.SetTextAutoSize("La Rata Remix");
            panel.OffsetTop = expanded ? -322 : -92;
        }
        // Transparent hit target over the native header; no replacement button art.
        var toggle = new NButton { Position = title.Position, Size = title.Size, MouseFilter = Control.MouseFilterEnum.Stop, FocusMode = Control.FocusModeEnum.All };
        panel.AddChild(toggle);
        toggle.TooltipText = "Show or hide La Rata run effects";
        toggle.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => SetExpanded(!body.Visible)));
        SetExpanded(true);
        Refresh();
        __instance.VisibilityChanged += Refresh;
        ModEntry.Log.Info("UI native map legend panel attached.");
    }

    private static MegaLabel CopyLabel(MegaLabel source, Node parent, Vector2 position, Vector2 size, int fontSize)
    {
        var label = (MegaLabel)source.Duplicate((int)Node.DuplicateFlags.Scripts);
        label.UniqueNameInOwner = false;
        parent.AddChild(label);
        label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        label.Position = position;
        label.Size = size;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.AddThemeFontSizeOverride("font_size", fontSize);
        return label;
    }
}
