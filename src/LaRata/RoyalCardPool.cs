using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace LaRata;

// BaseLib applies our materials to the game's existing sculpted frame and banner.
public sealed class RoyalCardPool : CustomCardPoolModel
{
    public override string Title => UiText.Get("pool.royal");
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
    // Let the native rarity getter supply its exact blue/gold banner, border
    // and type-plaque materials. Pool-colored frame material is independent.
    internal static Material? Banner(CardRarity rarity) =>
        rarity is CardRarity.Uncommon or CardRarity.Rare ? null : ShaderUtils.GenerateHsv(.41f, .62f, .58f);
}
