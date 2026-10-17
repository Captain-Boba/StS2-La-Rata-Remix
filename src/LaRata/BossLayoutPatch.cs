using Godot;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;

namespace LaRata;

[HarmonyPatch]
internal static class BossCameraPatch
{
    private static IEnumerable<MethodBase> TargetMethods() =>
        new[] { typeof(EncounterModel), typeof(QueenBoss), typeof(AeonglassBoss), typeof(TestSubjectBoss) }
            .Select(t => (MethodBase)AccessTools.Method(t, nameof(EncounterModel.GetCameraScaling))).Distinct();

    [HarmonyPostfix]
    private static void Postfix(EncounterModel __instance, ref float __result)
    {
        if (EncounterDirector.Get(__instance)?.BossDuo == true) __result = Math.Min(__result, .80f);
    }
}

[HarmonyPatch(typeof(EncounterModel), nameof(EncounterModel.GetAssetPaths))]
internal static class BossAssetsPatch
{
    [HarmonyPostfix]
    private static void Postfix(EncounterModel __instance, ref IEnumerable<string> __result)
    {
        var source = EncounterDirector.Get(__instance)?.BossSource;
        if (source != null) __result = __result.Concat(source.ExtraAssetPaths).Distinct(StringComparer.Ordinal).ToArray();
    }
}

// Queen's native scene has two named markers. Preserve both and add a third in memory.
// No original scenes/assets are changed; other bosses use the game's automatic layout.
[HarmonyPatch(typeof(EncounterModel), nameof(EncounterModel.CreateScene))]
internal static class BossScenePatch
{
    [HarmonyPostfix]
    private static void Postfix(EncounterModel __instance, Control __result)
    {
        if (__instance is not QueenBoss || EncounterDirector.Get(__instance)?.BossDuo != true) return;
        __result.GetNode<Marker2D>("amalgam").Position = new Vector2(1230, 760);
        __result.GetNode<Marker2D>("queen").Position = new Vector2(1500, 680);
        __result.AddChild(new Marker2D { Name = "larata_ally", Position = new Vector2(1850, 760) });
    }
}

[HarmonyPatch(typeof(QueenBoss), "get_Slots")]
internal static class BossSlotsPatch
{
    [HarmonyPostfix]
    private static void Postfix(QueenBoss __instance, ref IReadOnlyList<string> __result)
    {
        if (EncounterDirector.Get(__instance)?.BossDuo == true) __result = [.. __result, "larata_ally"];
    }
}
