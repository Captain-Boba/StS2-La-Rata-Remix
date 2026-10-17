using BaseLib.Abstracts;
using MegaCrit.Sts2.addons.mega_text;
using BaseLib.Patches.Content;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Singleton;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace LaRata;

// The native table only defines acts 0..2. Keep its HP/block rules for the
// bonus act, including the stronger boss factor, without replacing the table.
[HarmonyPatch(typeof(MultiplayerScalingModel), nameof(MultiplayerScalingModel.GetMultiplayerScaling))]
internal static class BonusMultiplayerScalingPatch
{
    [HarmonyPrefix]
    static void Prefix(ref int actIndex)
    {
        if (actIndex >= 3 && RunManager.Instance.DebugOnlyGetState()?.Act is RatDomain)
            actIndex = 2;
    }
}

// Native character-unlock epochs exist only for the three regular acts.
// The regular finale already awarded its epoch before entering this bonus.
[HarmonyPatch(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch")]
internal static class BonusEpochPatch
{
    [HarmonyPrefix]
    static bool Prefix(Player localPlayer, int act) => localPlayer.RunState.Act is not RatDomain || act < 3;
}

[HarmonyPatch(typeof(NGameOverScreen), "InitializeBannerAndQuote")]
internal static class BonusDefeatMessagePatch
{
    [HarmonyPostfix]
    static void Postfix(RunState ____runState, NCommonBanner ____banner, MegaRichTextLabel ____victoryDamageLabel)
    {
        if (Endgame.Phase.Get(____runState) != 4 || ____runState.Act is not RatDomain) return;
        ____banner.label.SetTextAutoSize("Regular victory secured");
        ____victoryDamageLabel.Text = "The bonus challenge ends here.\nYour regular victory and progression are preserved.\nLa Rata keeps his crown... for now.";
    }
}

internal static class Endgame
{
    // Saved by BaseLib in native run saves AND native multiplayer save packets.
    // 0 regular, 1 invitation pending, 2 accepted, 3 declined, 4 bonus active, 5 bonus beaten.
    internal static readonly SavedSpireField<IRunState, int> Phase = new(() => 0, "LaRataEndgameV1");
    // Missing custom fields in legacy network save packets deserialize as zero.
    // Use the same zero sentinel locally; regular finales cannot be act index zero.
    internal static readonly SavedSpireField<IRunState, int> FinalIndex = new(() => 0, "LaRataRegularFinalV1");
    internal static int RegularFinalIndex(IRunState run) => FinalIndex.Get(run) > 0 ? FinalIndex.Get(run) : run.Acts.Count - 1;
    internal static void Register()
    {
        _ = Phase.Name;
        CustomContentDictionary.RegisterType(typeof(RatDomain));
        CustomContentDictionary.RegisterType(typeof(RatInvitation));
    }
    internal static async Task Offer(RunManager manager, RunState run)
    {
        FinalIndex.Set(run, run.CurrentActIndex);
        Phase.Set(run, 1);
        var invitation = new EventRoom(ModelDb.Event<RatInvitation>());
        await EnterEndingRoom(manager, invitation);
        if (manager.ShouldSave) await SaveManager.Instance.SaveRun(invitation);
        ModEntry.Log.Info("ENDGAME invitation; regular victory secured.");
    }
    internal static async Task Proceed(RunManager manager, RunState run)
    {
        if (Phase.Get(run) == 3)
        {
            var ending = new EventRoom(ModelDb.Event<TheArchitect>());
            await EnterEndingRoom(manager, ending);
            if (manager.ShouldSave) await SaveManager.Instance.SaveRun(ending);
            return;
        }
        if (Phase.Get(run) != 2) throw new InvalidOperationException("La Rata: invitation has no committed decision.");
        var existing = run.Acts.Select((a, i) => (a, i)).FirstOrDefault(x => x.a is RatDomain);
        int index;
        if (existing.a != null) index = existing.i;
        else
        {
            var act = (RatDomain)ModelDb.Act<RatDomain>().ToMutable();
            act.GenerateRooms(new Rng(run.Rng.Seed, "LaRata/BonusRooms/v1"), run.UnlockState, run.Players.Count > 1);
            index = run.Acts.Count;
            AccessTools.PropertySetter(typeof(RunState), nameof(RunState.Acts)).Invoke(run, new object[] { run.Acts.Append(act).ToArray() });
        }
        Phase.Set(run, 4);
        await manager.EnterAct(index);
        if (manager.ShouldSave) await SaveManager.Instance.SaveRun(null);
        ModEntry.Log.Info($"ENDGAME entered bonus act index={index}; original finale={RegularFinalIndex(run)}.");
    }
    internal static async Task FinishBonus(RunManager manager, RunState run)
    {
        Phase.Set(run, 5);
        var ending = new EventRoom(ModelDb.Event<TheArchitect>());
        await EnterEndingRoom(manager, ending);
        if (manager.ShouldSave) await SaveManager.Instance.SaveRun(ending);
    }
    private static async Task EnterEndingRoom(RunManager manager, EventRoom room)
    {
        using var loading = new NetLoadingHandle(manager.NetService);
        await NGame.Instance!.Transition.RoomFadeOut();
        AccessTools.Method(typeof(RunManager), "ClearScreens").Invoke(manager, null);
        await manager.EnterRoom(room);
        await (Task)AccessTools.Method(typeof(RunManager), "FadeIn").Invoke(manager, new object[] { true })!;
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.LoadIntoLatestMapCoord))]
internal static class RestoreInvitationPatch
{
    [HarmonyPrefix]
    static void Prefix(RunManager __instance, ref AbstractRoom? preFinishedRoom)
    {
        var run = __instance.DebugOnlyGetState();
        // The regular boss node has no separate map coordinate for its ending event.
        if (run != null && Endgame.Phase.Get(run) is >= 1 and <= 3 && run.CurrentActIndex == Endgame.RegularFinalIndex(run))
            preFinishedRoom = new EventRoom(ModelDb.Event<RatInvitation>());
        else if (run != null && Endgame.Phase.Get(run) == 5 && run.Act is RatDomain)
            preFinishedRoom = new EventRoom(ModelDb.Event<TheArchitect>());
    }
}

[HarmonyPatch(typeof(NEventLayout), nameof(NEventLayout.SetPortrait))]
internal static class InvitationPortraitPatch
{
    [HarmonyPostfix]
    static void Postfix(EventModel ____event, TextureRect? ____portrait)
    {
        if (____event is not RatInvitation || ____portrait == null) return;
        ____portrait.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        ____portrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        ____portrait.CustomMinimumSize = Vector2.Zero;
        ____portrait.Size = new Vector2(690, 750);
        ____portrait.Position = new Vector2(105, 190);
        ____portrait.MouseFilter = Control.MouseFilterEnum.Ignore;
    }
}

public sealed class RatInvitation : CustomEventModel
{
    public RatInvitation() : base(false) { }
    public override bool IsAllowed(IRunState runState) => false;
    public override bool IsShared => true;
    public override string? CustomInitialPortraitPath => "res://LaRata/images/rat_king.png";
    public override List<(string, string)> Localization =>
    [
        ("title", "You Are Not Prepared"),
        ("pages.INITIAL.description", "Your victory is secure. Beyond the Spire, the Rat King waits.\n\nAn optional path lies ahead: three unknown rooms, a shop, and La Rata. Even defeat here will preserve your regular victory.\n\nEvery player must agree to enter."),
        ("pages.INITIAL.options.ENTER.title", "Enter the Rat King's domain"),
        ("pages.INITIAL.options.ENTER.description", "Face the optional final challenge. Requires unanimous agreement."),
        ("pages.INITIAL.options.LEAVE.title", "End the run"),
        ("pages.INITIAL.options.LEAVE.description", "Continue to the Architect and the regular ending."),
        ("pages.ACCEPTED.description", "The party has agreed. The Rat King awaits. Your regular victory remains secure."),
        ("pages.DECLINED.description", "The party has chosen the regular ending. The Architect awaits.")
    ];
    protected override IReadOnlyList<EventOption> GenerateInitialOptions() => [Option(Enter), Option(Leave)];
    protected override void SetInitialEventState(bool isPreFinished)
    {
        int phase = Endgame.Phase.Get(Owner!.RunState);
        if (phase is 2 or 3) SetEventFinished(PageDescription(phase == 2 ? "ACCEPTED" : "DECLINED"));
        else base.SetInitialEventState(false);
    }
    private Task Enter() => Decide(2, "ACCEPTED");
    private Task Leave() => Decide(3, "DECLINED");
    private async Task Decide(int phase, string page)
    {
        var owner = Owner ?? throw new InvalidOperationException("Invitation has no owner.");
        Endgame.Phase.Set(owner.RunState, phase);
        SetEventFinished(PageDescription(page));
        if (owner == owner.RunState.Players[0] && RunManager.Instance.ShouldSave)
            await SaveManager.Instance.SaveRun(owner.RunState.CurrentRoom);
    }
}

public sealed class RatDomain : CustomActModel, ILocalizationProvider
{
    public RatDomain() : base(4, false) { }
    public List<(string, string)> Localization => [("title", "The Rat King's Domain")];
    protected override int BaseNumberOfRooms => 4;
    public override IEnumerable<AncientEventModel> AllAncients => ModelDb.Act<Glory>().AllAncients;
    public override IEnumerable<EventModel> AllEvents => ModelDb.Act<Glory>().AllEvents;
    public override IEnumerable<EncounterModel> GenerateAllEncounters() => ModelDb.Act<Glory>().AllEncounters.Where(e => e.RoomType != RoomType.Boss).Append(ModelDb.Encounter<RatKingEncounter>());
    protected override string CustomMapTopBgPath => ModelDb.Act<Glory>().MapTopBgPath;
    protected override string CustomMapMidBgPath => ModelDb.Act<Glory>().MapMidBgPath;
    protected override string CustomMapBotBgPath => ModelDb.Act<Glory>().MapBotBgPath;
    protected override string CustomRestSiteBackgroundPath => ModelDb.Act<Glory>().RestSiteBackgroundPath;
    protected override ActMap? CustomCreateMap(RunState runState, bool replaceTreasureWithElites) => new RatPathMap();
}

internal sealed class RatPathMap : ActMap
{
    protected override MapPoint?[,] Grid { get; } = new MapPoint?[7, 4];
    public override MapPoint StartingMapPoint { get; } = new(3, 0) { PointType = MapPointType.Unknown };
    public override MapPoint BossMapPoint { get; } = new(3, 4) { PointType = MapPointType.Boss };
    internal RatPathMap()
    {
        MapPoint previous = StartingMapPoint;
        for (int row = 1; row <= 3; row++)
        {
            var next = new MapPoint(3, row) { PointType = row == 3 ? MapPointType.Shop : MapPointType.Unknown };
            Grid[3, row] = next;
            previous.AddChildPoint(next);
            previous = next;
        }
        startMapPoints.Add(Grid[3, 1]!);
        previous.AddChildPoint(BossMapPoint);
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterNextAct))]
internal static class BonusTransitionPatch
{
    [HarmonyPrefix]
    static bool Prefix(RunManager __instance, ref Task __result)
    {
        var run = __instance.DebugOnlyGetState();
        if (run == null) return true;
        if (run.CurrentRoom is EventRoom { CanonicalEvent: RatInvitation })
        {
            __result = Endgame.Proceed(__instance, run);
            return false;
        }
        if (Endgame.Phase.Get(run) == 0 && run.CurrentActIndex == run.Acts.Count - 1 && run.CurrentRoom is CombatRoom { RoomType: RoomType.Boss })
        {
            __result = Endgame.Offer(__instance, run);
            return false;
        }
        if (run.Act is RatDomain && run.CurrentRoom is CombatRoom { RoomType: RoomType.Boss })
        {
            __result = Endgame.FinishBonus(__instance, run);
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(NEventRoom), nameof(NEventRoom.Proceed))]
internal static class BonusProceedPatch
{
    [HarmonyPrefix]
    static bool Prefix(ref Task __result)
    {
        if (RunManager.Instance.DebugOnlyGetState()?.CurrentRoom is not EventRoom { CanonicalEvent: RatInvitation }) return true;
        RunManager.Instance.ActChangeSynchronizer.SetLocalPlayerReady();
        __result = Task.CompletedTask;
        return false;
    }
}

[HarmonyPatch(typeof(EventSynchronizer), "ChooseSharedEventOption")]
internal static class UnanimousInvitationPatch
{
    [HarmonyPrefix]
    static bool Prefix(EventSynchronizer __instance, EventModel? ____canonicalEvent, List<uint?> ____playerVotes,
        INetGameService ____netService, uint ____pageIndex, RunLocationTargetedMessageBuffer ____messageBuffer)
    {
        if (____canonicalEvent is not RatInvitation) return true;
        if (____netService.Type == NetGameType.Client || ____playerVotes.Count == 0 || ____playerVotes.Any(v => !v.HasValue))
            throw new InvalidOperationException("La Rata: invalid invitation resolution context.");
        uint selected = ____playerVotes.All(v => v == 0u) ? 0u : 1u;
        string votes = string.Join(',', ____playerVotes);
        ____netService.SendMessage(new SharedEventOptionChosenMessage { optionIndex = selected, pageIndex = ____pageIndex, location = ____messageBuffer.CurrentLocation });
        AccessTools.Method(typeof(EventSynchronizer), "ChooseOptionForSharedEvent").Invoke(__instance, new object[] { selected });
        ModEntry.Log.Info($"ENDGAME unanimous={selected == 0} votes={votes}");
        return false;
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.OnEnded))]
internal static class PreserveRegularVictoryPatch
{
    [HarmonyPrefix]
    static void Prefix(RunManager __instance, ref bool isVictory)
    {
        var run = __instance.DebugOnlyGetState();
        if (run != null && Endgame.Phase.Get(run) > 0)
        {
            if (!isVictory) ModEntry.Log.Info("ENDGAME bonus defeat/exit; preserving regular victory in progression and history.");
            isVictory = true;
        }
    }
}

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.SetMap))]
internal static class BonusMapWordsPatch
{
    [HarmonyPostfix]
    static void Postfix(NMapScreen __instance, RunState ____runState, Dictionary<MapCoord, NMapPoint> ____mapPointDictionary,
        Control ____pathsContainer, Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>> ____paths, ref float ____distY)
    {
        if (____runState.Act is not RatDomain) return;
        ____distY = 190f;
        foreach (var path in ____pathsContainer.GetChildren()) { ____pathsContainer.RemoveChild(path); path.QueueFree(); }
        ____paths.Clear();
        foreach (var pair in ____mapPointDictionary)
        {
            var point = pair.Value;
            var anchor = point.GetParent<Control>().Size * new Vector2(point.AnchorLeft, point.AnchorTop);
            point.Position = anchor + (point is NBossMapPoint ? new Vector2(-200, -160) : new Vector2(-80, 800 - pair.Key.row * 190));
            if (point is NBossMapPoint)
            {
                foreach (string name in new[] { "%PlaceholderImage", "%PlaceholderOutline" })
                {
                    var portrait = point.GetNode<TextureRect>(name);
                    portrait.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                    portrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
                    portrait.Size = new Vector2(280, 300);
                    portrait.MouseFilter = Control.MouseFilterEnum.Ignore;
                    // The prototype has no separate silhouette asset. Avoid
                    // drawing a second, offset copy of the full-color portrait.
                    if (name == "%PlaceholderOutline") portrait.Visible = false;
                }
            }
        }
        var drawPaths = AccessTools.Method(typeof(NMapScreen), "DrawPaths");
        foreach (var point in ____mapPointDictionary.Values) drawPaths.Invoke(__instance, new object[] { point, point.Point });
        string[] words = ["YOU", "ARE", "NOT", "PREPARED"];
        for (int row = 0; row < words.Length; row++)
        {
            if (!____mapPointDictionary.TryGetValue(new MapCoord(3, row), out var point) || point.HasNode("LaRataWord")) continue;
            var source = __instance.GetNode<MegaLabel>("MapLegend/Header");
            var label = (MegaLabel)source.Duplicate((int)Node.DuplicateFlags.Scripts);
            label.Name = "LaRataWord";
            label.UniqueNameInOwner = false;
            label.MouseFilter = Control.MouseFilterEnum.Ignore;
            label.AddThemeFontSizeOverride("font_size", 30);
            point.AddChild(label);
            label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
            label.Position = new Vector2(65, -20);
            label.Size = new Vector2(210, 55);
            label.SetTextAutoSize(words[row]);
        }
    }
}
