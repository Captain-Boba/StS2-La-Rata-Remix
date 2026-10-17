using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace LaRata;

// The finale owns the soundtrack. A guest boss retains its native sound effects,
// but its original encounter's FMOD music parameters do not exist on that track.
[HarmonyPatch(typeof(NRunMusicController), nameof(NRunMusicController.UpdateMusicParameter))]
internal static class BossMusicPatch
{
    [HarmonyPrefix]
    private static bool Prefix(IRunState ____runState, string label)
    {
        var rules = EncounterDirector.Get((____runState.CurrentRoom as CombatRoom)?.Encounter);
        if (rules?.BossDuo != true) return true;
        return (rules.Extra, label) switch
        {
            (WaterfallGiant, "waterfall_giant_progress") => false,
            (Vantom, "vantom_progress") => false,
            (KnowledgeDemon, "knowledge_demon_progress") => false,
            _ => true
        };
    }
}
