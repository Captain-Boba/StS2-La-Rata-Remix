using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;

namespace LaRata;

// Presentation only. Native CardSelectCmd owns the selection, network message and result.
internal static class BoosterOpening
{
    private sealed record PackInfo(string Name, string Icon);
    private static readonly ConditionalWeakTable<CardModel, PackInfo> Packs = new();
    private sealed record Focus(Func<Control?> Get);
    private static readonly ConditionalWeakTable<NCardGridSelectionScreen, Focus> Focuses = new();
    internal static void Mark(CardModel card, string name, string icon) => Packs.AddOrUpdate(card, new(name, icon));
    internal static void Unmark(CardModel card) => Packs.Remove(card);
    private static readonly AccessTools.FieldRef<NCardGridSelectionScreen, IReadOnlyList<CardModel>> ScreenCards =
        AccessTools.FieldRefAccess<NCardGridSelectionScreen, IReadOnlyList<CardModel>>("_cards");

    [HarmonyPatch(typeof(NSimpleCardSelectScreen), nameof(NSimpleCardSelectScreen._Ready))]
    private static class Attach
    {
        [HarmonyPostfix] private static void Postfix(NSimpleCardSelectScreen __instance)
        {
            var cards = ScreenCards(__instance);
            if (cards.Count != 5 || !Packs.TryGetValue(cards[0], out var pack)) return;
            Build(__instance, cards, pack);
        }
    }

    [HarmonyPatch(typeof(NCardGridSelectionScreen), nameof(NCardGridSelectionScreen.DefaultFocusedControl), MethodType.Getter)]
    private static class OpeningFocus
    {
        [HarmonyPrefix] private static bool Prefix(NCardGridSelectionScreen __instance, ref Control? __result)
        {
            if (!Focuses.TryGetValue(__instance, out var focus)) return true;
            __result = focus.Get();
            return false;
        }
    }

    private static void Build(NSimpleCardSelectScreen screen, IReadOnlyList<CardModel> cards, PackInfo pack)
    {
        var hidden = new[] { "%CardGrid", "%BottomText", "%Confirm", "%PeekButton" }
            .Select(path => screen.GetNode<Control>(path)).Select(c => (Node: c, c.Visible)).ToArray();
        foreach (var item in hidden) item.Node.Hide();
        var buttons = hidden.Select(i => i.Node).OfType<NClickableControl>().Select(b => (Button: b, b.IsEnabled)).ToArray();
        foreach (var item in buttons) item.Button.Disable();
        var veil = new ColorRect { Name = "LaRataBoosterOpening", Color = new Color("080D12"), MouseFilter = Control.MouseFilterEnum.Stop };
        screen.AddChild(veil);
        veil.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var stage = new Control { Name = "PackTable", Size = new Vector2(1920, 1080), MouseFilter = Control.MouseFilterEnum.Ignore };
        veil.AddChild(stage);
        void Resize()
        {
            float scale = Math.Min(veil.Size.X / 1920f, veil.Size.Y / 1080f);
            stage.Scale = Vector2.One * scale;
            stage.Position = (veil.Size - stage.Size * scale) * .5f;
        }
        veil.Resized += Resize;
        Resize();
        var title = new Label { Text = pack.Name + " Booster", Position = new Vector2(360, 35), Size = new Vector2(1200, 65), HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        title.AddThemeFontSizeOverride("font_size", 44);
        title.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_bold_shared.tres"));
        stage.AddChild(title);
        var hint = new Label { Text = "Open the pack. Hover to sense rarity. Click each card to reveal it.", Position = new Vector2(300, 980), Size = new Vector2(1320, 50), HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        hint.AddThemeFontSizeOverride("font_size", 25);
        hint.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_regular_shared.tres"));
        stage.AddChild(hint);
        var open = new NButton { Position = new Vector2(780, 340), Size = new Vector2(360, 420), PivotOffset = new Vector2(180, 210), FocusMode = Control.FocusModeEnum.All };
        stage.AddChild(open);
        var packIcon = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = GD.Load<Texture2D>(pack.Icon), Size = new Vector2(360, 360), StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore };
        open.AddChild(packIcon);
        var openText = new Label { Text = "Open pack", Position = new Vector2(0, 360), Size = new Vector2(360, 55), HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        openText.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_bold_shared.tres"));
        openText.AddThemeFontSizeOverride("font_size", 32);
        open.AddChild(openText);
        var nativePopup = NGenericPopup.Create()!;
        var choose = (NPopupYesNoButton)nativePopup.GetNode<NPopupYesNoButton>("VerticalPopup/YesButton").Duplicate((int)(Node.DuplicateFlags.Scripts | Node.DuplicateFlags.UseInstantiation));
        nativePopup.Free();
        choose.GetNodeOrNull<Control>("ControllerIcon")?.Free();
        stage.AddChild(choose);
        choose.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        choose.Position = new Vector2(790, 910);
        choose.Size = new Vector2(340, 72);
        choose.DisconnectHotkeys();
        choose.SetText("Choose cards");
        choose.Hide();
        var reveals = new List<NButton>();
        Focuses.AddOrUpdate(screen, new Focus(() => open.Visible ? open : choose.Visible ? choose : reveals.FirstOrDefault(b => b.IsEnabled)));
        veil.TreeExiting += () => Focuses.Remove(screen);
        choose.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
        {
            Focuses.Remove(screen);
            foreach (var item in hidden) item.Node.Visible = item.Visible;
            foreach (var item in buttons) item.Button.SetEnabled(item.IsEnabled);
            veil.QueueFree();
            screen.DefaultFocusedControl?.GrabFocus();
        }));
        int revealed = 0;
        // Five-card fan: one at the top, two at the sides, two below the center.
        Vector2[] positions = [new(810, 120), new(330, 265), new(1290, 265), new(570, 495), new(1050, 495)];
        open.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
        {
            open.Disable();
            var burst = open.CreateTween();
            burst.TweenProperty(open, "scale", Vector2.One * 1.12f, .20).SetTrans(Tween.TransitionType.Back);
            burst.TweenProperty(open, "modulate:a", 0f, .18);
            burst.TweenCallback(Callable.From(() =>
            {
                open.Hide();
                for (int i = 0; i < cards.Count; i++) AddCard(cards[i], i);
            }));
        }));
        Callable.From(open.GrabFocus).CallDeferred();

        void AddCard(CardModel model, int index)
        {
            bool boss = model is RemixCard { IsBossPrize: true };
            bool rare = model.Rarity is CardRarity.Rare or CardRarity.Ancient;
            Color color = boss ? new Color("56FFA5") : rare ? new Color("F2BE55") : model.Rarity == CardRarity.Uncommon ? new Color("65BCEB") : new Color("C1CED5");
            var pivot = new Control { Position = new Vector2(810, 390), Size = new Vector2(300, 422), PivotOffset = new Vector2(150, 211), Scale = Vector2.One * .25f, MouseFilter = Control.MouseFilterEnum.Ignore };
            stage.AddChild(pivot);
            var aura = new Panel { Position = new Vector2(-12, -12), Size = new Vector2(324, 446), MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = new Color(1, 1, 1, .15f) };
            var rim = new StyleBoxFlat { BgColor = new Color(color, .06f), BorderColor = color, ShadowColor = new Color(color, .7f), ShadowSize = boss ? 32 : rare ? 25 : 16 };
            rim.SetBorderWidthAll(2);
            rim.SetCornerRadiusAll(18);
            aura.AddThemeStyleboxOverride("panel", rim);
            pivot.AddChild(aura);
            var card = NCard.Create(model)!;
            pivot.AddChild(card);
            card.Position = new Vector2(150, 211);
            card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            card.Hide();
            var back = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = GD.Load<Texture2D>("res://LaRata/images/booster_back.png"), Size = new Vector2(300, 422), StretchMode = TextureRect.StretchModeEnum.Scale, MouseFilter = Control.MouseFilterEnum.Ignore };
            pivot.AddChild(back);
            var hit = new NButton { Size = new Vector2(300, 422), FocusMode = Control.FocusModeEnum.All, TooltipText = "Reveal card" };
            pivot.AddChild(hit);
            hit.Disable(); // A reveal must not race the dealing tween on scale:x.
            reveals.Add(hit);
            Tween? hover = null;
            bool faceUp = false;
            void Glow(bool on)
            {
                hover?.Kill();
                hover = aura.CreateTween();
                hover.TweenProperty(aura, "modulate:a", on || faceUp ? .95f : .15f, .22);
                if (on && !faceUp)
                {
                    hover.TweenProperty(aura, "modulate:a", .55f, .65);
                    hover.TweenProperty(aura, "modulate:a", .95f, .65);
                    hover.SetLoops();
                }
            }
            hit.MouseEntered += () => Glow(true);
            hit.MouseExited += () => Glow(false);
            hit.FocusEntered += () => Glow(true);
            hit.FocusExited += () => Glow(false);
            hit.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => TaskHelper.RunSafely(Reveal())));
            var deal = pivot.CreateTween();
            deal.TweenInterval(index * .09);
            deal.TweenProperty(pivot, "position", positions[index], .55).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            deal.Parallel().TweenProperty(pivot, "scale", Vector2.One * .82f, .55);
            deal.TweenCallback(Callable.From(() =>
            {
                hit.Enable();
                if (index == 0) hit.GrabFocus();
            }));

            async Task Reveal()
            {
                if (faceUp) return;
                faceUp = true;
                hit.Disable();
                hover?.Kill();
                var flip = pivot.CreateTween();
                flip.TweenProperty(pivot, "scale:x", 0f, .22).SetTrans(Tween.TransitionType.Sine);
                if (!await flip.AwaitFinished(pivot)) return;
                back.Hide();
                card.Show();
                card.ActivateRewardScreenGlow();
                aura.Modulate = Colors.White;
                // Rarity changes both the halo and the duration/size of the reveal burst.
                var particles = new CpuParticles2D
                {
                    Position = new Vector2(150, 211), Amount = boss ? 64 : rare ? 42 : model.Rarity == CardRarity.Uncommon ? 24 : 10,
                    Lifetime = boss ? 1.6 : rare ? 1.2 : .7, OneShot = true, Explosiveness = 1,
                    EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle, EmissionRectExtents = new Vector2(145, 205),
                    Direction = Vector2.Up, Spread = 180, InitialVelocityMin = 50, InitialVelocityMax = boss ? 220 : rare ? 150 : 95,
                    Gravity = Vector2.Zero, ScaleAmountMin = 2, ScaleAmountMax = boss ? 6 : 4, Color = color
                };
                pivot.AddChild(particles);
                var unfold = pivot.CreateTween();
                unfold.TweenProperty(pivot, "scale:x", boss ? .93f : .87f, rare || boss ? .38 : .25).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                unfold.TweenProperty(pivot, "scale:x", .82f, .18);
                if (!await unfold.AwaitFinished(pivot)) return;
                if (++revealed == cards.Count)
                {
                    hint.Text = "Your pack is revealed. Keep all, some, or none.";
                    choose.Show();
                    choose.GrabFocus();
                }
                else reveals.FirstOrDefault(b => b.IsEnabled)?.GrabFocus();
            }
        }
    }
}
