using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;

namespace LaRata;

// BaseLib applies our materials to the game's existing sculpted frame and banner.
public sealed class RoyalCardPool : CustomCardPoolModel
{
    public override string Title => "The Royal Court";
    public override bool IsColorless => true;
    public override bool IsShared => true;
    public override Color ShaderColor => RoyalCardStyle.FrameColor;
    public override Color DeckEntryCardColor => new("344F44");
    public override Color EnergyOutlineColor => new("36996F");
    public override string EnergyColorName => "silent";
}

internal static class RoyalCardStyle
{
    internal static Color FrameColor => new("344F44");
    internal static Material Frame() => new ShaderMaterial
    {
        Shader = ResourceLoader.Load<Shader>("res://LaRata/shaders/royal_card_frame.gdshader")
    };
    internal static Material Banner() => ShaderUtils.GenerateHsv(.41f, .62f, .58f);
}
