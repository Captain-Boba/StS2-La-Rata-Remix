using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.addons.mega_text;

namespace LaRata;

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom._Ready))]
internal static class CombatJournalView
{
    private sealed record Played(int Round);
    private static readonly ConditionalWeakTable<CardModel, Played> Rounds = new();
    private static readonly AccessTools.FieldRef<NCombatCardPile, Vector2> PilePosition =
        AccessTools.FieldRefAccess<NCombatCardPile, Vector2>("_showPosition");

    [HarmonyPostfix] private static void Postfix(NCombatRoom __instance, ICombatRoomVisuals ____visuals)
    {
        if (__instance.Mode != CombatRoomMode.ActiveCombat || ____visuals is not CombatRoom combatRoom) return;
        var combat = combatRoom.CombatState;
        var exhaust = __instance.Ui.ExhaustPile;
        var viewport = __instance.GetTree().Root;
        var buttons = new List<(NButton Button, MegaLabel Count)>();
        NCardsViewScreen? cardsView = null;
        NGenericPopup? potionView = null;
        void CloseViews()
        {
            if (cardsView != null && ReferenceEquals(NCapstoneContainer.Instance?.CurrentCapstoneScreen, cardsView)) NCapstoneContainer.Instance!.Close();
            if (potionView != null && ReferenceEquals(NModalContainer.Instance?.OpenModal, potionView)) NModalContainer.Instance!.Clear();
            cardsView = null;
            potionView = null;
        }
        for (int i = 0; i < 2; i++)
        {
            bool royal = i == 0;
            var button = new NButton { Size = exhaust.Size, PivotOffset = exhaust.Size / 2, FocusMode = Control.FocusModeEnum.All };
            exhaust.GetParent().AddChild(button);
            button.Name = royal ? "RoyalDiscard" : "GoblinHistory";
            button.Hide();
            // Set expansion before the texture: otherwise its natural minimum
            // clamps Size and remains enlarged even after switching IgnoreSize on.
            var icon = new TextureRect { ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                Texture = GD.Load<Texture2D>(royal
                    ? "res://LaRata/images/booster_back.png"
                    : "res://images/atlases/intent_atlas.sprites/intent_card_debuff.tres"),
                Size = new Vector2(58, 66), Position = new Vector2(11, 0),
                MouseFilter = Control.MouseFilterEnum.Ignore };
            // Use an actual thrown potion for the goblin counter, updated below.
            button.AddChild(icon);
            var counter = (Control)exhaust.GetNode<Control>("CountContainer").Duplicate((int)Node.DuplicateFlags.Scripts);
            button.AddChild(counter);
            var countLabel = counter.GetNode<MegaLabel>("Count");
            countLabel.AddThemeColorOverride("font_color", new Color(royal ? "8CFFC1" : "BDEAF6"));
            void Tips(bool show)
            {
                NHoverTipSet.Remove(button);
                if (show) NHoverTipSet.CreateAndShow(button,
                    new HoverTip(new LocString("monsters", ModelDb.Monster<RatKing>().Id.Entry + (royal ? ".history.title" : ".history.goblin")),
                        royal ? "View cards played in this combat and their rounds." : "View thrown potions, rounds and recipients."), HoverTipAlignment.Left);
                icon.Modulate = show ? new Color(1.2f, 1.2f, 1.2f) : Colors.White;
            }
            button.MouseEntered += () => Tips(true);
            button.MouseExited += () => Tips(false);
            button.FocusEntered += () => Tips(true);
            button.FocusExited += () => Tips(false);
            button.TreeExiting += () => NHoverTipSet.Remove(button);
            button.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ =>
            {
                if (CombatManager.Instance.IsEnding || CombatManager.Instance.PlayerActionsDisabled) return;
                if (royal) cardsView = ShowCards(combat);
                else potionView = ShowPotions(combat);
            }));
            buttons.Add((button, countLabel));
        }
        void Layout()
        {
            if (!GodotObject.IsInstanceValid(exhaust)) return;
            // Use the pile's visible anchor even when the native empty exhaust
            // pile is hidden offscreen; history has its own visibility lifecycle.
            var anchor = PilePosition(exhaust);
            int slot = 1;
            foreach (var item in buttons)
                if (item.Button.Visible) item.Button.Position = anchor - new Vector2(0, 86 * slot++);
        }
        void Refresh()
        {
            if (!GodotObject.IsInstanceValid(__instance)) return;
            var entries = CombatJournal.Read(combat).Entries;
            bool available = !CombatManager.Instance.IsEnding && !CombatManager.Instance.PlayerActionsDisabled;
            if (entries.Length == 0 || !available) CloseViews();
            for (int i = 0; i < buttons.Count; i++)
            {
                int count = entries.Count(e => e.Kind == (i == 0 ? "card" : "potion"));
                buttons[i].Count.SetTextAutoSize(count.ToString());
                buttons[i].Button.Visible = count > 0 && !CombatManager.Instance.IsEnding;
                buttons[i].Button.SetEnabled(count > 0 && available);
                if (i == 1 && count > 0)
                    buttons[i].Button.GetChild<TextureRect>(0).Texture = ModelDb.GetById<PotionModel>(
                        ModelId.Deserialize(entries.Last(e => e.Kind == "potion").Model)).Image;
            }
            Layout();
        }
        void StateChanged(CombatState? _) => Refresh();
        CombatJournal.Changed += Refresh;
        CombatManager.Instance.PlayerActionsDisabledChanged += StateChanged;
        viewport.SizeChanged += Layout;
        __instance.TreeExiting += () =>
        {
            CombatJournal.Changed -= Refresh;
            CombatManager.Instance.PlayerActionsDisabledChanged -= StateChanged;
            viewport.SizeChanged -= Layout;
            foreach (var item in buttons)
                if (GodotObject.IsInstanceValid(item.Button)) NHoverTipSet.Remove(item.Button);
            CloseViews();
        };
        Refresh();
    }

    private static NCardsViewScreen? ShowCards(ICombatState combat)
    {
        var cards = new List<CardPileAddResult>();
        foreach (var entry in CombatJournal.Read(combat).Entries.Where(e => e.Kind == "card"))
        {
            var canonical = RoyalCard.All.FirstOrDefault(c => c.Id.ToString() == entry.Model);
            if (canonical == null) continue;
            var card = canonical.ToMutable();
            Rounds.Add(card, new(entry.Round));
            cards.Add(new CardPileAddResult { cardAdded = card });
        }
        if (cards.Count == 0) return null;
        var info = new LocString("monsters", ModelDb.Monster<RatKing>().Id.Entry + ".history.description");
        return NSimpleCardsViewScreen.ShowScreen(cards, info);
    }

    private static NGenericPopup? ShowPotions(ICombatState combat)
    {
        if (NModalContainer.Instance?.OpenModal != null) return null;
        var modal = NGenericPopup.Create()!;
        var panel = modal.GetNode<NVerticalPopup>("VerticalPopup");
        NModalContainer.Instance!.Add(modal);
        panel.SetText("Potion Goblin", "Thrown potions in this combat. Hover for their effects.");
        panel.YesButton.SetText("Close");
        panel.YesButton.Show();
        panel.YesButton.Enable();
        var description = panel.GetNode<Control>("Description");
        description.Size = new Vector2(description.Size.X, 80);
        panel.NoButton.Hide();
        panel.NoButton.DisconnectHotkeys();
        panel.YesButton.Connect(NClickableControl.SignalName.Released, Callable.From<NButton>(_ => NModalContainer.Instance!.Clear()));
        // End above the native close button, not underneath its visual bounds.
        var scroll = new ScrollContainer { Position = new Vector2(40, 195), Size = new Vector2(panel.Size.X - 80, 240), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        panel.AddChild(scroll);
        var rows = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        rows.AddThemeConstantOverride("separation", 14);
        scroll.AddChild(rows);
        foreach (var entry in CombatJournal.Read(combat).Entries.Where(e => e.Kind == "potion"))
        {
            var potion = ModelDb.GetById<PotionModel>(ModelId.Deserialize(entry.Model)).ToMutable();
            var recipient = combat.Players.FirstOrDefault(p => p.NetId == entry.Recipient);
            if (recipient != null) potion.Owner = recipient;
            int seat = combat.Players.OrderBy(p => p.NetId).ToList().FindIndex(p => p.NetId == entry.Recipient) + 1;
            var row = new HBoxContainer { CustomMinimumSize = new Vector2(0, 80), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Stop };
            row.AddThemeConstantOverride("separation", 10);
            rows.AddChild(row);
            row.AddChild(new TextureRect { Texture = potion.Image, CustomMinimumSize = new Vector2(48, 48),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore });
            if (recipient != null) row.AddChild(new TextureRect { Texture = recipient.Character.IconTexture,
                CustomMinimumSize = new Vector2(38, 48), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = Control.MouseFilterEnum.Ignore });
            var label = new Label { Text = $"Round {entry.Round} · {potion.Title.GetFormattedText()}\nTo player {seat}: {recipient?.Character.Title.GetFormattedText() ?? "Unknown"}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = Control.MouseFilterEnum.Ignore };
            label.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_regular_shared.tres"));
            label.AddThemeFontSizeOverride("font_size", 20);
            row.AddChild(label);
            row.MouseEntered += () =>
            {
                NHoverTipSet.Remove(row);
                var tips = NHoverTipSet.CreateAndShow(row, potion.HoverTips, HoverTipAlignment.Right);
                // Native tips normally live below the modal backstop. Keep this
                // set inside its owning popup, above the dimmer and outside the
                // scroll container's clipping, preserving native positioning.
                tips?.Reparent(modal);
            };
            row.MouseExited += () => NHoverTipSet.Remove(row);
            row.TreeExiting += () => NHoverTipSet.Remove(row);
        }
        panel.YesButton.GrabFocus();
        return modal;
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.HoverTips), MethodType.Getter)]
    private static class RoundTip
    {
        [HarmonyPostfix] private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
        {
            if (Rounds.TryGetValue(__instance, out var played))
            {
                var title = new LocString("monsters", ModelDb.Monster<RatKing>().Id.Entry + ".history.round");
                title.Add("Round", played.Round);
                __result = __result.Append(new HoverTip(title, "Played by La Rata in this combat."));
            }
        }
    }
}
