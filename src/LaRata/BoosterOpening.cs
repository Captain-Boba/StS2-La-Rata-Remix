using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
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
            if (cards.Count is not (5 or 7) || !Packs.TryGetValue(cards[0], out var pack)) return;
            Build(__instance, cards, pack);
        }
    }

    [HarmonyPatch(typeof(NCardGridSelectionScreen), nameof(NCardGridSelectionScreen.DefaultFocusedControl), MethodType.Getter)]
    private static class OpeningFocus
    {
        [HarmonyPrefix] internal static bool Prefix(NCardGridSelectionScreen __instance, ref Control? __result)
        {
            if (!Focuses.TryGetValue(__instance, out var focus)) return true;
            __result = focus.Get();
            return false;
        }
    }

    [HarmonyPatch(typeof(NCardGridSelectionScreen), nameof(NCardGridSelectionScreen.FocusedControlFromTopBar), MethodType.Getter)]
    private static class OpeningFocusFromTopBar
    {
        [HarmonyPrefix] private static bool Prefix(NCardGridSelectionScreen __instance, ref Control? __result) =>
            OpeningFocus.Prefix(__instance, ref __result);
    }

    private static void Build(NSimpleCardSelectScreen screen, IReadOnlyList<CardModel> cards, PackInfo pack)
    {
        if (Focuses.TryGetValue(screen, out _)) return;
        bool closed = false;
        bool opening = false;
        int dealtCount = 0;
        var reveals = new List<NButton>();
        var animations = new List<Tween>();
        var refreshTips = new List<Action>();
        var controller = NControllerManager.Instance;
        bool UsingController() => controller?.IsUsingController == true;
        void FocusForController(Control? control)
        {
            if (!closed && UsingController() && control is { } && control.IsVisibleInTree())
                control.GrabFocus();
        }
        Tween Animate(Node owner)
        {
            var tween = owner.CreateTween();
            animations.Add(tween);
            return tween;
        }
        var inputChanged = Callable.From(() =>
        {
            foreach (var refresh in refreshTips) refresh();
        });
        void Cleanup()
        {
            if (closed) return;
            closed = true;
            Focuses.Remove(screen);
            if (GodotObject.IsInstanceValid(controller))
                foreach (var signal in new[] { NControllerManager.SignalName.MouseDetected, NControllerManager.SignalName.ControllerDetected })
                    if (controller.IsConnected(signal, inputChanged)) controller.Disconnect(signal, inputChanged);
            foreach (var hit in reveals)
                if (GodotObject.IsInstanceValid(hit))
                {
                    NHoverTipSet.Remove(hit);
                    hit.ReleaseFocus();
                }
            foreach (var tween in animations)
                if (GodotObject.IsInstanceValid(tween) && tween.IsValid()) tween.Kill();
        }
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
            foreach (var refresh in refreshTips) refresh();
        }
        veil.Resized += Resize;
        Resize();
        var title = new Label { Text = pack.Name, Position = new Vector2(360, 35), Size = new Vector2(1200, 65), HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        title.AddThemeFontSizeOverride("font_size", 44);
        title.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_bold_shared.tres"));
        stage.AddChild(title);
        var hint = new Label { Text = UiText.Get("booster.hint"), Position = new Vector2(300, 980), Size = new Vector2(1320, 50), HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        hint.AddThemeFontSizeOverride("font_size", 25);
        hint.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_regular_shared.tres"));
        stage.AddChild(hint);
        var open = new NButton { Position = new Vector2(780, 340), Size = new Vector2(360, 420), PivotOffset = new Vector2(180, 210), FocusMode = Control.FocusModeEnum.All };
        stage.AddChild(open);
        var packIcon = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = GD.Load<Texture2D>(pack.Icon), Size = new Vector2(360, 360), StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore };
        open.AddChild(packIcon);
        var openText = new Label { Text = UiText.Get("booster.open"), Position = new Vector2(0, 360), Size = new Vector2(360, 55), HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
        openText.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_bold_shared.tres"));
        openText.AddThemeFontSizeOverride("font_size", 32);
        open.AddChild(openText);
        foreach (var label in new[] { title, hint, openText }) UiText.ApplyFont(label);
        var nativePopup = NGenericPopup.Create()!;
        var choose = (NPopupYesNoButton)nativePopup.GetNode<NPopupYesNoButton>("VerticalPopup/YesButton").Duplicate((int)(Node.DuplicateFlags.Scripts | Node.DuplicateFlags.UseInstantiation));
        nativePopup.Free();
        choose.GetNodeOrNull<Control>("ControllerIcon")?.Free();
        stage.AddChild(choose);
        choose.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        // The native artwork is 180px wide and does not stretch when the hitbox
        // is widened. Center its original bounds instead of an invisible 340px box.
        choose.Size = new Vector2(180, 72);
        choose.Position = new Vector2((stage.Size.X - choose.Size.X) / 2, 910);
        choose.DisconnectHotkeys();
        choose.SetText(UiText.Get("booster.choose"));
        choose.Hide();
        Focuses.AddOrUpdate(screen, new Focus(() => closed ? null : open.Visible && open.IsEnabled ? open : choose.Visible ? choose :
            dealtCount == cards.Count ? reveals.FirstOrDefault(b => b.IsEnabled && !b.HasMeta("revealed")) : null));
        veil.TreeExiting += Cleanup;
        if (controller != null)
            foreach (var signal in new[] { NControllerManager.SignalName.MouseDetected, NControllerManager.SignalName.ControllerDetected })
                controller.Connect(signal, inputChanged);
        choose.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
        {
            if (closed) return;
            Cleanup();
            veil.Hide();
            foreach (var item in hidden) item.Node.Visible = item.Visible;
            foreach (var item in buttons) item.Button.SetEnabled(item.IsEnabled);
            veil.QueueFree();
            if (UsingController()) screen.DefaultFocusedControl?.GrabFocus();
        }));
        int revealed = 0;
        // Seven equal-size slots; keep pending five-card saves revealable too.
        Vector2[] positions = cards.Count == 7
            ? [new(330, 140), new(650, 140), new(970, 140), new(1290, 140), new(490, 510), new(810, 510), new(1130, 510)]
            : [new(810, 120), new(330, 265), new(1290, 265), new(570, 495), new(1050, 495)];
        open.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
        {
            if (closed || opening) return;
            opening = true;
            open.Disable();
            open.ReleaseFocus();
            var burst = Animate(open);
            burst.TweenProperty(open, "scale", Vector2.One * 1.12f, .20).SetTrans(Tween.TransitionType.Back);
            burst.TweenProperty(open, "modulate:a", 0f, .18);
            burst.TweenCallback(Callable.From(() =>
            {
                if (closed) return;
                open.Hide();
                for (int i = 0; i < cards.Count; i++) AddCard(cards[i], i);
            }));
        }));
        Callable.From(() => FocusForController(open)).CallDeferred();

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
            var face = new Control { Size = NCard.defaultSize, PivotOffset = NCard.defaultSize / 2, MouseFilter = Control.MouseFilterEnum.Ignore };
            pivot.AddChild(face);
            // This short-lived view belongs to the opening, not the native pool.
            // QueueFree of the veil must not destroy a pooled NCard behind its pool.
            var card = PreloadManager.Cache.GetScene("res://scenes/cards/card.tscn").Instantiate<NCard>();
            card.Model = model;
            face.AddChild(card);
            card.Position = new Vector2(150, 211);
            card.UpdateVisuals(PileType.None, CardPreviewMode.Normal);
            card.Scale = Vector2.One;
            card.Body.Scale = Vector2.One;
            card.Hide();
            var back = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, Texture = GD.Load<Texture2D>("res://LaRata/images/booster_back.png"), Size = new Vector2(300, 422), StretchMode = TextureRect.StretchModeEnum.Scale, MouseFilter = Control.MouseFilterEnum.Ignore };
            face.AddChild(back);
            var hit = new NButton { Size = new Vector2(300, 422), FocusMode = Control.FocusModeEnum.All,
                MouseFilter = Control.MouseFilterEnum.Stop, TooltipText = UiText.Get("booster.reveal") };
            pivot.AddChild(hit);
            hit.Disable(); // A reveal must not race the dealing tween on scale:x.
            reveals.Add(hit);
            Tween? hover = null;
            bool faceUp = false;
            bool dealt = false;
            bool flipping = false;
            bool nativeFocused = false;
            void RefreshTips()
            {
                if (closed) return;
                bool on = nativeFocused && dealt && !flipping && hit.IsEnabled && hit.IsVisibleInTree();
                Glow(on);
                NHoverTipSet.Remove(hit);
                if (on && faceUp)
                    // The card is already visible. Native card holders show its
                    // keyword tips, not a second full CardHoverTip at screen origin.
                    NHoverTipSet.CreateAndShow(hit, model.HoverTips,
                        positions[index].X > 1000 ? HoverTipAlignment.Left : HoverTipAlignment.Right);
            }
            refreshTips.Add(RefreshTips);
            hit.TreeExiting += () => NHoverTipSet.Remove(hit);
            void ClearTips()
            {
                NHoverTipSet.Remove(hit);
                if (!closed) Glow(false);
            }
            void Glow(bool on)
            {
                hover?.Kill();
                hover = Animate(aura);
                hover.TweenProperty(aura, "modulate:a", on || faceUp ? .95f : .15f, .22);
                if (on && !faceUp)
                {
                    hover.TweenProperty(aura, "modulate:a", .55f, .65);
                    hover.TweenProperty(aura, "modulate:a", .95f, .65);
                    hover.SetLoops();
                }
            }
            // Match native NCardHolder: Enable/Disable and mouse/controller hover
            // share one focus lifecycle. Geometry alone must not create tips.
            hit.Connect(NClickableControl.SignalName.Focused, Callable.From<NClickableControl>(_ =>
            {
                nativeFocused = true;
                ModEntry.Log.Debug($"BOOSTER UI slot={index} nativeFocused=true dealt={dealt} flipping={flipping} faceUp={faceUp}");
                RefreshTips();
            }));
            hit.Connect(NClickableControl.SignalName.Unfocused, Callable.From<NClickableControl>(_ =>
            {
                nativeFocused = false;
                ModEntry.Log.Debug($"BOOSTER UI slot={index} nativeFocused=false dealt={dealt} flipping={flipping} faceUp={faceUp}");
                ClearTips();
            }));
            hit.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => TaskHelper.RunSafely(Reveal())));
            var deal = Animate(pivot);
            deal.TweenInterval(index * .09);
            deal.TweenProperty(pivot, "position", positions[index], .55).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            deal.Parallel().TweenProperty(pivot, "scale", Vector2.One * .82f, .55);
            deal.TweenCallback(Callable.From(() =>
            {
                if (closed) return;
                pivot.Position = positions[index];
                pivot.Scale = Vector2.One * .82f;
                dealt = true;
                hit.Enable();
                RefreshTips();
                if (++dealtCount == cards.Count)
                    FocusForController(reveals.FirstOrDefault(b => b.IsEnabled && !b.HasMeta("revealed")));
            }));

            async Task Reveal()
            {
                if (closed || !dealt || faceUp) return;
                faceUp = true;
                flipping = true;
                hit.Disable();
                RefreshTips();
                hover?.Kill();
                var flip = Animate(face);
                flip.TweenProperty(face, "scale:x", .01f, .22).SetTrans(Tween.TransitionType.Sine);
                if (!await flip.AwaitFinished(pivot) || closed) return;
                back.Hide();
                card.Show();
                aura.Modulate = Colors.White;
                // Rarity changes both the halo and the duration/size of the reveal burst.
                var sparkGradient = new Gradient();
                sparkGradient.SetColor(0, Colors.White);
                sparkGradient.SetColor(1, new Color(1, 1, 1, 0));
                var spark = new GradientTexture2D { Width = 16, Height = 16, Gradient = sparkGradient,
                    Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new Vector2(.5f, .5f), FillTo = new Vector2(1, .5f) };
                var particles = new CpuParticles2D
                {
                    Texture = spark,
                    Position = new Vector2(150, 211), Amount = boss ? 64 : rare ? 42 : model.Rarity == CardRarity.Uncommon ? 24 : 10,
                    Lifetime = boss ? 1.6 : rare ? 1.2 : .7, OneShot = true, Explosiveness = 1,
                    EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle, EmissionRectExtents = new Vector2(145, 205),
                    Direction = Vector2.Up, Spread = 180, InitialVelocityMin = 50, InitialVelocityMax = boss ? 220 : rare ? 150 : 95,
                    Gravity = Vector2.Zero, ScaleAmountMin = .18f, ScaleAmountMax = boss ? .5f : .35f, Color = color
                };
                pivot.AddChild(particles);
                var unfold = Animate(face);
                unfold.TweenProperty(face, "scale:x", 1f, rare || boss ? .38 : .25).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
                if (!await unfold.AwaitFinished(pivot) || closed) return;
                flipping = false;
                hit.Enable();
                hit.TooltipText = "";
                hit.SetMeta("revealed", true);
                RefreshTips();
                if (++revealed == cards.Count)
                {
                    hint.Text = UiText.Get("booster.revealed");
                    choose.Show();
                    FocusForController(choose);
                }
                else FocusForController(reveals.FirstOrDefault(b => b.IsEnabled && !b.HasMeta("revealed")));
            }
        }
    }
}
