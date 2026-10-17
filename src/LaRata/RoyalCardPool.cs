using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;

namespace LaRata;

// BaseLib supplies the game's frame shader. This pool contains no player reward cards.
public sealed class RoyalCardPool : CustomCardPoolModel
{
    public override string Title => "The Royal Court";
    public override bool IsColorless => true;
    public override bool IsShared => true;
    public override Color ShaderColor => RoyalCardStyle.FrameColor;
    public override Color DeckEntryCardColor => new("071C18");
    public override Color EnergyOutlineColor => new("073B2E");
    public override string EnergyColorName => "silent";
}

internal static class RoyalCardStyle
{
    internal static Color FrameColor => new("0B1714");
    internal static Material Frame() => ShaderUtils.GenerateHsv(FrameColor.H, FrameColor.S, FrameColor.V);
    internal static Material Banner() => ShaderUtils.GenerateHsv(.43f, .78f, .35f);
}
