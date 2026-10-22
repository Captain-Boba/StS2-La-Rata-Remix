using HarmonyLib;
using System.Runtime.CompilerServices;
using LaRata;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

internal static class StarterBoosterChecks
{
    private static readonly Dictionary<BoosterRelic, TaskCompletionSource> Choices = new();
    private static bool WaitForChoice(BoosterRelic __instance, ref Task __result)
    {
        __result = Finish(__instance);
        return false;
    }
    private static async Task Finish(BoosterRelic pack)
    {
        await Choices[pack].Task;
        pack.Opened = true;
    }
    internal static void Run()
    {
        // Replace only the interactive pack screen with a controllable choice.
        // The actual pending/opened lifecycle and Ancient callback are exercised.
        var harmony = new Harmony("LaRata.StarterBoosterChecks");
        harmony.Patch(AccessTools.Method(typeof(BoosterRelic), "Open"),
            prefix: new HarmonyMethod(typeof(StarterBoosterChecks), nameof(WaitForChoice)));
        try
        {
            var done = new bool[3];
            var tasks = new Task[3];
            var packs = new BoosterRelic[3];
            var events = new EventModel[3];
            for (int i = 0; i < 3; i++)
            {
                int seat = i;
                // Only the owner's relic collection is needed. The real Player
                // constructor boots Godot's SaveManager in a console process.
                var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
                var pack = (BoosterRelic)ModelDb.Relic<IroncladBooster>().ToMutable();
                pack.StarterPending = true;
                AccessTools.Field(typeof(Player), "_relics").SetValue(player, new List<RelicModel> { pack });
                var ancient = ModelDb.Event<Neow>().ToMutable();
                AccessTools.PropertySetter(typeof(EventModel), nameof(EventModel.Owner)).Invoke(ancient, [player]);
                Choices.Add(pack, new TaskCompletionSource());
                packs[i] = pack;
                events[i] = ancient;
                tasks[i] = StartingBoosterChoicePatch.OpenThenChoose(ancient, () =>
                {
                    if (!pack.Opened || pack.StarterPending) throw new Exception("Ancient overtook its starter pack.");
                    done[seat] = true;
                    return Task.CompletedTask;
                });
            }
            if (tasks.Any(t => t.IsCompleted)) throw new Exception("Pending pack did not hold its event task.");
            foreach (int seat in new[] { 2, 0, 1 })
            {
                Choices[packs[seat]].SetResult();
                tasks[seat].GetAwaiter().GetResult();
                if (!done[seat]) throw new Exception("Player waits for someone else's pack.");
            }
            // Replaying a completed callback must not open the pack again.
            Choices.Clear();
            StartingBoosterChoicePatch.OpenThenChoose(events[0], () => Task.CompletedTask).GetAwaiter().GetResult();
        }
        finally { harmony.UnpatchAll(harmony.Id); Choices.Clear(); }
        Console.WriteLine("PASS: three starter-pack choices finish independently in 3/1/2 order; each Ancient callback awaits its own pack; completed packs do not reopen. UI choice simulated, not a network test.");
    }
}
