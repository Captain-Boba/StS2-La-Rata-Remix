using System.Globalization;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;
using Godot;

namespace LaRata;

// Native localization table, with English fallback supplied by BaseLib's normal loader.
internal static class UiText
{
    internal static void Register() => CustomLocTableManager.Register("larata_ui.json");
    internal static string Get(string key) => new LocString("larata_ui", key).GetFormattedText();
    internal static void ApplyFont(Control control) => control.ApplyLocaleFontSubstitution(FontType.Regular, "font");
    internal static string Format(string key, params object[] values)
    {
        var text = new LocString("larata_ui", key);
        for (int i = 0; i < values.Length; i++)
            text.AddObj(i.ToString(CultureInfo.InvariantCulture), values[i]);
        return text.GetFormattedText();
    }
}
