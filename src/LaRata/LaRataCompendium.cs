using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;

namespace LaRata;

// This category is a reference view. No card registration, progression write or RNG call.
internal static class LaRataCompendium
{
    private sealed record View(NCardPoolFilter Filter, NCardLibraryGrid Grid, OptionButton Roles);
    private static readonly ConditionalWeakTable<NCardLibrary, View> Views = new();
    private static readonly ConditionalWeakTable<NCardLibraryGrid, View> Grids = new();
    private static readonly ConditionalWeakTable<NInspectCardScreen, object> Inspectors = new();
    private static readonly Dictionary<ModelId, LaRataCardEntry> Entries = new();
    private static readonly object InspectorMarker = new();
    private static readonly AccessTools.FieldRef<NCardLibraryGrid, List<CardModel>> GridCards =
        AccessTools.FieldRefAccess<NCardLibraryGrid, List<CardModel>>("_allCards");

    private static T Field<T>(object instance, string name) => Traverse.Create(instance).Field<T>(name).Value;
    private static LaRataCardEntry? Entry(CardModel model)
    {
        if (Entries.Count == 0)
            foreach (var entry in LaRataCardCatalog.Entries) Entries.Add(entry.ModelId, entry);
        return Entries.GetValueOrDefault(model.Id);
    }
    private static string Role(LaRataCardEntry entry) => UiText.Get(entry.Role switch
    {
        LaRataCardRole.BossExclusive => "library.boss",
        LaRataCardRole.PlayableBossVariant => "library.prize",
        _ => "library.player"
    });

    internal static string Status(LaRataCardEntry entry)
    {
        var labels = new List<string> { Role(entry) };
        if (entry.Role == LaRataCardRole.BossExclusive)
        {
            labels.Add(UiText.Get("library.unobtainable"));
            if (!entry.BossEligible) labels.Add(UiText.Get("library.inactive"));
        }
        else if (!entry.Model.Pool.GetUnlockedCards(SaveManager.Instance.GenerateUnlockStateFromProgress(), CardMultiplayerConstraint.None)
                     .Any(c => c.Id == entry.ModelId) && entry.UnlockPolicy != "AlwaysAvailable")
            labels.Add(UiText.Get("library.locked"));
        if (!SaveManager.Instance.Progress.DiscoveredCards.Contains(entry.ModelId)) labels.Add(UiText.Get("library.unseen"));
        if (!entry.HasUpgrade) labels.Add(UiText.Get("library.no_upgrade"));
        return string.Join(" · ", labels);
    }

    [HarmonyPatch(typeof(NCardLibrary), nameof(NCardLibrary._Ready))]
    private static class Attach
    {
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        private static void Postfix(NCardLibrary __instance)
        {
            if (Views.TryGetValue(__instance, out _)) return;
            var template = Field<NCardPoolFilter>(__instance, "_colorlessFilter");
            var filter = (NCardPoolFilter)template.Duplicate((int)(Node.DuplicateFlags.Scripts | Node.DuplicateFlags.UseInstantiation));
            filter.Name = "LaRataReferenceFilter";
            var image = filter.GetNode<TextureRect>("Image");
            image.Material = (Material)image.Material.Duplicate();
            image.Texture = GD.Load<Texture2D>("res://LaRata/images/relics/royal_pack.png");
            filter.Loc = new LocString("larata_ui", "library.hint");
            var grid = Field<NCardLibraryGrid>(__instance, "_grid");
            var roles = new OptionButton { Name = "LaRataRoles", CustomMinimumSize = new Vector2(240, 42), Visible = false };
            foreach (var key in new[] { "library.all", "library.player", "library.prize", "library.boss", "library.inactive" })
                roles.AddItem(UiText.Get(key));
            roles.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_regular_shared.tres"));
            roles.AddThemeFontSizeOverride("font_size", 20);
            UiText.ApplyFont(roles);
            __instance.AddChild(roles);
            roles.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft);
            roles.Position = new Vector2(44, __instance.Size.Y - 116);
            __instance.Resized += () => roles.Position = new Vector2(44, __instance.Size.Y - 116);
            var view = new View(filter, grid, roles);
            Views.Add(__instance, view);
            Grids.Add(grid, view);
            var filters = Field<Dictionary<NCardPoolFilter, Func<CardModel, bool>>>(__instance, "_poolFilters");
            filters.Add(filter, c => Entry(c) != null);
            template.AddSibling(filter);
            filter.IsSelected = false;
            filter.Connect(NCardPoolFilter.SignalName.Toggled, Callable.From<NCardPoolFilter>(f =>
                Traverse.Create(__instance).Method("UpdateCardPoolFilter", f).GetValue()));
            roles.ItemSelected += _ => Traverse.Create(__instance).Method("UpdateFilter", false).GetValue();
            filter.FocusEntered += () => Traverse.Create(__instance).Field("_lastHoveredControl").SetValue(filter);
            __instance.TreeExiting += () => { Views.Remove(__instance); Grids.Remove(grid); };
        }
    }

    [HarmonyPatch(typeof(NCardLibrary), "UpdateFilter")]
    private static class ReferenceFilter
    {
        [HarmonyPrefix]
        private static bool Prefix(NCardLibrary __instance)
        {
            if (!Views.TryGetValue(__instance, out var view)) return true;
            view.Roles.Visible = view.Filter.IsSelected;
            if (!view.Filter.IsSelected) return true;
            string query = NSearchBar.Normalize(Field<NSearchBar>(__instance, "_searchBar").Text).ToLowerInvariant();
            bool upgrades = Field<NLibraryStatTickbox>(__instance, "_viewUpgrades").IsTicked;
            bool multiplayer = Field<NLibraryStatTickbox>(__instance, "_viewMultiplayerCards").IsTicked;
            var types = Field<Dictionary<NCardTypeTickbox, Func<CardModel, bool>>>(__instance, "_cardTypeFilters");
            var rarities = Field<Dictionary<NCardRarityTickbox, Func<CardModel, bool>>>(__instance, "_rarityFilters");
            var costs = Field<Dictionary<NCardCostTickbox, Func<CardModel, bool>>>(__instance, "_costFilters");
            bool Matches(CardModel model)
            {
                var entry = Entry(model);
                if (entry == null || !multiplayer && model.MultiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly) return false;
                if (view.Roles.Selected == 1 && entry.Role != LaRataCardRole.Player ||
                    view.Roles.Selected == 2 && entry.Role != LaRataCardRole.PlayableBossVariant ||
                    view.Roles.Selected == 3 && entry.Role != LaRataCardRole.BossExclusive ||
                    view.Roles.Selected == 4 && (entry.Role != LaRataCardRole.BossExclusive || entry.BossEligible)) return false;
                if (types.Any(p => p.Key.IsTicked) && !types.Any(p => p.Key.IsTicked && p.Value(model))) return false;
                if (rarities.Any(p => p.Key.IsTicked) && !rarities.Any(p => p.Key.IsTicked && p.Value(model))) return false;
                if (costs.Any(p => p.Key.IsTicked) && !costs.Any(p => p.Key.IsTicked && p.Value(model))) return false;
                if (query.Length == 0) return true;
                var preview = (CardModel)model.MutableClone();
                if (upgrades && preview.IsUpgradable) preview.UpgradeInternal();
                string description = entry.Role == LaRataCardRole.BossExclusive && entry.BossId is int boss
                    ? RoyalBossEffects.Description(boss) : preview.GetDescriptionForPile(PileType.None);
                string text = NSearchBar.Normalize(NSearchBar.RemoveHtmlTags($"{model.Title} {description} {Status(entry)} {entry.FamilyId} {model.Id}"));
                return text.Contains(query, StringComparison.OrdinalIgnoreCase);
            }
            Traverse.Create(__instance).Field("_filter").SetValue((Func<CardModel, bool>)Matches);
            // Keep the native display/count/empty-state/sorting pipeline.
            Traverse.Create(__instance).Method("DisplayCards").GetValue<Task>();
            return false;
        }
    }

    [HarmonyPatch(typeof(NCardLibraryGrid), nameof(NCardLibraryGrid.FilterCards), [typeof(Func<CardModel, bool>), typeof(List<SortingOrders>)])]
    private static class ReferenceSource
    {
        [HarmonyPrefix]
        private static void Prefix(NCardLibraryGrid __instance, out CardModel[]? __state)
        {
            __state = null;
            if (!Grids.TryGetValue(__instance, out var view) || !view.Filter.IsSelected) return;
            var list = GridCards(__instance);
            __state = list.ToArray();
            list.Clear();
            foreach (var entry in LaRataCardCatalog.Entries)
            {
                var model = (CardModel)entry.Model.MutableClone();
                if (entry.BossId is int boss)
                    RoyalDeckView.MarkBossPreview(model, boss, entry.Role == LaRataCardRole.BossExclusive);
                list.Add(model);
            }
        }
        [HarmonyFinalizer]
        private static void Finalizer(NCardLibraryGrid __instance, CardModel[]? __state)
        {
            if (__state == null) return;
            var list = GridCards(__instance);
            list.Clear();
            list.AddRange(__state);
        }
    }

    [HarmonyPatch(typeof(NCardLibraryGrid), "GetCardVisibility")]
    private static class ReferenceVisibility
    {
        [HarmonyPrefix]
        private static bool Prefix(NCardLibraryGrid __instance, CardModel card, ref ModelVisibility __result)
        {
            if (!Grids.TryGetValue(__instance, out var view) || !view.Filter.IsSelected || Entry(card) == null) return true;
            __result = ModelVisibility.Visible;
            return false;
        }
    }

    [HarmonyPatch(typeof(NCardLibrary), "ShowCardDetail")]
    private static class ReferenceInspect
    {
        [HarmonyPrefix]
        private static bool Prefix(NCardLibrary __instance, NCardHolder holder)
        {
            var model = holder.CardModel;
            if (model == null || !Views.TryGetValue(__instance, out var view) || !view.Filter.IsSelected || Entry(model) == null) return true;
            if (NGame.Instance == null) return false;
            var screen = NGame.Instance.GetInspectCardScreen();
            Inspectors.AddOrUpdate(screen, InspectorMarker);
            Traverse.Create(__instance).Field("_lastHoveredControl").SetValue(holder);
            var cards = view.Grid.VisibleCards.ToList();
            screen.Open(cards, cards.FindIndex(c => c.Id == model.Id), Field<NLibraryStatTickbox>(__instance, "_viewUpgrades").IsTicked);
            return false;
        }
    }

    [HarmonyPatch(typeof(NInspectCardScreen), nameof(NInspectCardScreen.Close))]
    private static class EndInspection
    {
        [HarmonyPostfix] private static void Postfix(NInspectCardScreen __instance) => Inspectors.Remove(__instance);
    }

    private static bool IsReferenceCard(NCard card)
    {
        for (Node? node = card.GetParent(); node != null; node = node.GetParent())
        {
            if (node is NCardLibrary library && Views.TryGetValue(library, out var view)) return view.Filter.IsSelected;
            if (node is NInspectCardScreen inspect) return Inspectors.TryGetValue(inspect, out _);
        }
        return false;
    }

    [HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
    private static class ReferenceLabels
    {
        [HarmonyPostfix]
        private static void Postfix(NCard __instance)
        {
            var container = __instance.GetNode<Control>("CardContainer");
            var old = container.GetNodeOrNull<Label>("LaRataReferenceStatus");
            bool reference = IsReferenceCard(__instance);
            var entry = __instance.Model == null ? null : Entry(__instance.Model);
            if (old != null) old.Visible = reference && entry != null;
            if (!reference || entry == null) return;
            if (old == null)
            {
                old = new Label
                {
                    Name = "LaRataReferenceStatus", Position = new Vector2(-127, 182), Size = new Vector2(254, 38),
                    AutowrapMode = TextServer.AutowrapMode.WordSmart, HorizontalAlignment = HorizontalAlignment.Center,
                    MouseFilter = Control.MouseFilterEnum.Ignore
                };
                old.AddThemeFontOverride("font", GD.Load<Font>("res://themes/kreon_regular_shared.tres"));
                old.AddThemeFontSizeOverride("font_size", 12);
                UiText.ApplyFont(old);
                old.AddThemeColorOverride("font_color", new Color("FFF0C8"));
                old.AddThemeColorOverride("font_shadow_color", Colors.Black);
                old.AddThemeConstantOverride("shadow_outline_size", 4);
                container.AddChild(old);
            }
            old.Text = Status(entry);
            if (entry.Role == LaRataCardRole.BossExclusive && entry.BossId is int boss)
                __instance.GetNode<MegaRichTextLabel>("%DescriptionLabel").SetTextAutoSize("[center]" + RoyalBossEffects.Description(boss) + "[/center]");
        }
    }
}
