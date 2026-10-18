using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace LaRata;

internal static class RoyalDeckView
{
    internal static Task<NCard?> Show(RatKing king, RoyalCard model) => Show(king.Creature, model);
    internal static async Task<NCard?> Show(Creature source, RoyalCard model, bool offscreen = false)
    {
        var room = NCombatRoom.Instance;
        if (room == null) return null;
        var card = NCard.Create(model.ToMutable());
        if (card == null) return null;
        room.Ui.PlayContainer.AddChild(card);
        card.UpdateVisuals(PileType.None, CardPreviewMode.None);
        card.GlobalPosition = offscreen ? new Vector2(1960, 180) : room.GetCreatureNode(source)?.GlobalPosition ?? new Vector2(1200, 400);
        card.Scale = Vector2.One * .25f;
        card.AnimCardToPlayPile();
        if (offscreen)
        {
            // Even Instant mode must leave enough time to recognize the revealed card.
            var reveal = card.CreateTween();
            reveal.TweenInterval(1.35);
            if (!await reveal.AwaitFinished(card)) return null;
        }
        else await Cmd.Wait(.75f);
        return card;
    }
    internal static void Dismiss(NCard? card)
    {
        if (card == null || !GodotObject.IsInstanceValid(card)) return;
        var tween = card.CreateTween();
        tween.TweenProperty(card, "modulate:a", 0f, .25);
        tween.TweenCallback(Callable.From(card.QueueFree));
    }
    internal static void AddRelics(NCreature node, RatKing king)
    {
        var row = new HBoxContainer { Name = "RoyalRelics", Position = new Vector2(-120, 38), MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 4);
        node.AddChild(row);
        var models = RoyalRegaliaPower.Relics;
        for (int i = 0; i < models.Length; i++)
        {
            if ((king.RegaliaMask & (1 << i)) == 0) continue;
            int index = i;
            var relic = models[i];
            var icon = new TextureRect { Texture = relic.Icon, CustomMinimumSize = new Vector2(44, 44), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Stop };
            row.AddChild(icon);
            icon.MouseEntered += () => NHoverTipSet.CreateAndShow(icon, new HoverTip(relic.Title, RoyalRegaliaPower.Effects[index], relic.Icon));
            icon.MouseExited += () => NHoverTipSet.Remove(icon);
        }
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
internal static class RoyalInitialViewPatch
{
    [HarmonyPostfix] private static void Postfix(NCreature __instance)
    {
        if (__instance.Entity?.CombatState?.Encounter is RatKingEncounter && __instance.Entity.Monster is TwoTailedRat)
        {
            // Encounter-local tint preserves native skeletons and animations.
            __instance.Visuals.Modulate = new Color(.32f, .80f, .64f);
            var wisps = new CpuParticles2D
            {
                Name = "ShadowCourtWisps", Position = new Vector2(0, -45), Amount = 12, Lifetime = 3.5,
                EmissionShape = CpuParticles2D.EmissionShapeEnum.Sphere, EmissionSphereRadius = 45,
                Direction = Vector2.Up, Spread = 18, Gravity = new Vector2(0, -8),
                InitialVelocityMin = 7, InitialVelocityMax = 14, ScaleAmountMin = 2, ScaleAmountMax = 4,
                Color = new Color(.10f, .8f, .48f, .35f)
            };
            var gradient = new Gradient();
            gradient.SetColor(0, new Color(.05f, .7f, .4f, 0));
            gradient.AddPoint(.35f, new Color(.1f, .85f, .5f, .5f));
            gradient.SetColor(gradient.GetPointCount() - 1, new Color(.05f, .7f, .4f, 0));
            wisps.ColorRamp = gradient;
            __instance.Visuals.AddChild(wisps);
        }
        if (__instance.Entity?.Monster is not RatKing king) return;
        var animator = __instance.Visuals.GetNodeOrNull<AnimationPlayer>("Visuals/AnimationPlayer");
        if (animator != null)
            foreach (string name in new[] { "Attack", "Cast", "Hit" }) animator.AnimationSetNext(name, "Idle");
        RoyalDeckView.AddRelics(__instance, king);
    }
}
