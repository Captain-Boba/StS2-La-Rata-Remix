using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace LaRata;

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.Initialize))]
internal static class MapPanelPatch
{
    [HarmonyPostfix]
    private static void Postfix(NMapScreen __instance)
    {
        if (__instance.HasNode("LaRataPanel")) return;
        var headerSource = __instance.GetNode<MegaLabel>("MapLegend/Header");
        var panel = new Control { Name = "LaRataPanel", MouseFilter = Control.MouseFilterEnum.Ignore };
        __instance.AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
        panel.OffsetLeft = -402;
        panel.OffsetRight = -62;
        panel.OffsetTop = -92;
        panel.OffsetBottom = -12;

        // Only the former collapsed title, using the native legend font.
        // No background, button, hover text or gameplay-state subscription.
        var title = (MegaLabel)headerSource.Duplicate((int)Node.DuplicateFlags.Scripts);
        title.UniqueNameInOwner = false;
        panel.AddChild(title);
        title.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        title.Position = new Vector2(30, 12);
        title.Size = new Vector2(280, 55);
        title.MouseFilter = Control.MouseFilterEnum.Ignore;
        title.AddThemeFontSizeOverride("font_size", 26);
        title.SetTextAutoSize("La Rata Remix");
        ModEntry.Log.Info("UI compact map title attached.");
    }
}
