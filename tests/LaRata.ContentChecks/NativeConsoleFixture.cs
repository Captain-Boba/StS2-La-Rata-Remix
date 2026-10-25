using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;

// Small shared setup for the existing native-model console runner, not a game host.
internal static class NativeConsoleFixture
{
    internal static void Set(object target, string property, object? value) =>
        AccessTools.Field(target.GetType(), $"<{property}>k__BackingField").SetValue(target, value);

    internal static Player Player(ulong id)
    {
        // The normal constructor accesses the desktop progress-save singleton.
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
        AccessTools.Field(typeof(Player), "_runState").SetValue(player, NullRunState.Instance);
        AccessTools.Field(typeof(Player), "_relics").SetValue(player, new List<RelicModel>());
        AccessTools.Field(typeof(Player), "_potionSlots").SetValue(player, new List<PotionModel?>());
        Set(player, nameof(player.Character), ModelDb.Character<Ironclad>());
        Set(player, nameof(player.NetId), id);
        Set(player, nameof(player.Deck), new CardPile(PileType.Deck));
        Set(player, nameof(player.UnlockState), UnlockState.all);
        Set(player, nameof(player.IsActiveForHooks), true);
        Set(player, nameof(player.ExtraFields), new ExtraPlayerFields());
        player.Gold = 10000;
        return player;
    }

    internal static T Relic<T>(Player owner) where T : RelicModel, new()
    {
        var relic = (T)ModelDb.Relic<T>().ToMutable();
        relic.Owner = owner;
        ((List<RelicModel>)AccessTools.Field(typeof(Player), "_relics").GetValue(owner)!).Add(relic);
        return relic;
    }

    private static bool NoArguments(ref string[] __result) { __result = []; return false; }
    private static bool NoFeature(ref bool __result) { __result = false; return false; }
    private static bool NoConsole() => false;
    private static bool NoModDiscovery(ref Type[] __result) { __result = []; return false; }

    internal static void PatchDesktop(Harmony harmony)
    {
        harmony.Patch(AccessTools.Method(typeof(Godot.OS), nameof(Godot.OS.GetCmdlineArgs)), prefix: new(typeof(NativeConsoleFixture), nameof(NoArguments)));
        harmony.Patch(AccessTools.Method(typeof(Godot.OS), nameof(Godot.OS.HasFeature)), prefix: new(typeof(NativeConsoleFixture), nameof(NoFeature)));
        harmony.Patch(AccessTools.Method(typeof(MegaCrit.Sts2.Core.Logging.ConsoleLogPrinter), "Print"), prefix: new(typeof(NativeConsoleFixture), nameof(NoConsole)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(MegaCrit.Sts2.Core.Helpers.ReflectionHelper), "ModTypes"), prefix: new(typeof(NativeConsoleFixture), nameof(NoModDiscovery)));
    }
}
