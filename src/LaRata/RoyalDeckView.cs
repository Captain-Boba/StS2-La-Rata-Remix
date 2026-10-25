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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.addons.mega_text;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace LaRata;

internal static class RoyalDeckView
{
    private sealed record BossPreview(int? Id, string? Description, bool Replace);
    private static readonly ConditionalWeakTable<CardModel, BossPreview> BossPreviews = new();
    internal static void MarkBossPreview(CardModel model, int bossId, bool replace = true) =>
        BossPreviews.AddOrUpdate(model, new(bossId, null, replace));
    internal static Task<NCard?> Show(RatKing king, RoyalCard model) => Show(king.Creature, model);
    internal static Task<NCard?> Show(RatKing king, CardModel model, string bossDescription) =>
        ShowCard(king.Creature, model, false, bossDescription);
    internal static Task<NCard?> Show(Creature source, RoyalCard model, bool offscreen = false) =>
        ShowCard(source, model, offscreen, null);
    private static async Task<NCard?> ShowCard(Creature source, CardModel model, bool offscreen, string? bossDescription)
    {
        var room = NCombatRoom.Instance;
        if (room == null) return null;
        var card = NCard.Create(model.ToMutable());
        if (card == null) return null;
        room.Ui.PlayContainer.AddChild(card);
        card.UpdateVisuals(PileType.None, CardPreviewMode.None);
        if (bossDescription != null && card.Model is CardModel shownModel)
        {
            BossPreviews.AddOrUpdate(shownModel, new(null, bossDescription, true));
            card.GetNode<MegaRichTextLabel>("%DescriptionLabel").SetTextAutoSize("[center]" + bossDescription + "[/center]");
        }
        card.GlobalPosition = offscreen ? new Vector2(1960, 180) : room.GetCreatureNode(source)?.GlobalPosition ?? new Vector2(1200, 400);
        card.Scale = Vector2.One * .25f;
        card.AnimCardToPlayPile();
        if (offscreen)
        {
            // Even Instant mode must leave enough time to recognize the revealed card.
            var reveal = card.CreateTween();
            reveal.TweenInterval(3.35);
            if (!await reveal.AwaitFinished(card)) return null;
        }
        else await Cmd.Wait(.75f);
        return card;
    }
    internal static async Task ShowResolution(NCard? card, string note)
    {
        if (card == null || !GodotObject.IsInstanceValid(card) || card.Model is not CardModel shownModel || string.IsNullOrWhiteSpace(note)) return;
        string description = BossPreviews.TryGetValue(shownModel, out var preview) ? Describe(preview) : "";
        card.GetNode<MegaRichTextLabel>("%DescriptionLabel").SetTextAutoSize("[center]" + description + "\n[color=#FFE3A0]" + note + "[/color][/center]");
        var reading = card.CreateTween();
        reading.TweenInterval(1.0);
        await reading.AwaitFinished(card);
    }
    private static string Describe(BossPreview preview) => preview.Id is int id ? RoyalBossEffects.Description(id) : preview.Description ?? "";

    [HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.MutableClone))]
    private static class CopyPreviewContext
    {
        [HarmonyPostfix] private static void Postfix(AbstractModel __instance, AbstractModel __result)
        {
            if (__instance is CardModel source && __result is CardModel clone && BossPreviews.TryGetValue(source, out var preview))
                BossPreviews.AddOrUpdate(clone, preview);
        }
    }

    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    private static class BossPreviewVisuals
    {
        [HarmonyPostfix] private static void Postfix(NCard __instance)
        {
            if (__instance.Model != null && BossPreviews.TryGetValue(__instance.Model, out var preview) && preview.Replace)
                __instance.GetNode<MegaRichTextLabel>("%DescriptionLabel").SetTextAutoSize("[center]" + Describe(preview) + "[/center]");
        }
    }
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
    private static class BossPreviewTips
    {
        [HarmonyPostfix] private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (!BossPreviews.TryGetValue(__instance, out var preview)) return;
            IHoverTip tip = new HoverTip(new LocString("larata_ui", "library.boss_effect"), Describe(preview));
            __result = preview.Replace ? [tip] : __result.Append(tip);
        }
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
        var row = new GridContainer { Name = "RoyalRelics", Columns = 5, Position = new Vector2(-106, 94), MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("h_separation", 4);
        row.AddThemeConstantOverride("v_separation", 4);
        node.AddChild(row);
        var models = RoyalRegaliaPower.Relics;
        for (int i = 0; i < models.Length; i++)
        {
            if ((king.RegaliaMask & (1 << i)) == 0) continue;
            int index = i;
            var relic = models[i];
            var icon = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = relic.Icon, CustomMinimumSize = new Vector2(40, 40), StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Stop };
            row.AddChild(icon);
            icon.MouseEntered += () => NHoverTipSet.CreateAndShow(icon, new HoverTip(relic.Title, RoyalRegaliaPower.Effects[index], relic.Icon), HoverTipAlignment.Right);
            icon.MouseExited += () => NHoverTipSet.Remove(icon);
            icon.TreeExiting += () => NHoverTipSet.Remove(icon);
        }
        var display = node.GetNode<NCreatureStateDisplay>("%HealthBar");
        var powers = display.GetNode<NPowerContainer>("%PowerContainer");
        // The native power container wraps without increasing its own height.
        // Follow the actual power rectangles, including a second buff row.
        void Layout()
        {
            if (!GodotObject.IsInstanceValid(row) || !row.IsInsideTree()) return;
            var toLocal = node.GetGlobalTransform().AffineInverse();
            float bottom = (toLocal * powers.GlobalPosition).Y;
            foreach (var power in powers.GetChildren().OfType<Control>())
                if (power.Visible && !power.IsQueuedForDeletion())
                    bottom = Math.Max(bottom, (toLocal * power.GetGlobalTransform() * power.Size).Y);
            row.Position = new Vector2(-row.Size.X / 2, Math.Max(94, bottom + 12));
        }
        bool queued = false;
        void QueueLayout()
        {
            if (queued) return;
            queued = true;
            Callable.From(() => { queued = false; Layout(); }).CallDeferred();
        }
        powers.ChildOrderChanged += QueueLayout;
        powers.ItemRectChanged += QueueLayout;
        display.ItemRectChanged += QueueLayout;
        row.Resized += QueueLayout;
        row.TreeExiting += () =>
        {
            powers.ChildOrderChanged -= QueueLayout;
            powers.ItemRectChanged -= QueueLayout;
            display.ItemRectChanged -= QueueLayout;
        };
        QueueLayout();
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
        if (__instance.Entity.CombatState?.Encounter is RatKingEncounter) RoyalDeckView.AddRelics(__instance, king);
    }
}
