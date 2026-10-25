using System.Reflection;
using System.Runtime.CompilerServices;
using BaseLib.Utils;
using BaseLib.Patches.Saves;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace LaRata;

// Pending provenance is not a reward counter. Only the native successful
// inventory/scalar mutations inside OnSelect are observed by the normal ledger.
internal static class RelicDeferredMetrics
{
    internal static readonly SavedSpireField<Reward, int> RewardSource =
        new(() => 0, "LaRataRewardRelicSourceV1");
    private sealed record OptionSource(Player Owner, int Acquisition);
    private static readonly ConditionalWeakTable<RestSiteOption, OptionSource> Options = new();
    internal static void Register() => _ = RewardSource;

    // BaseLib 3.4.7 preps Reward.ToSerializable, but native overrides such as
    // GoldReward create their own SerializableReward without calling base.
    // Reuse its registered payload API; no parallel reward save format.
    [HarmonyPatch]
    private static class NativeRewardSavePayload
    {
        private static IEnumerable<MethodBase> TargetMethods() => typeof(Reward).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(Reward).IsAssignableFrom(t))
            .Select(t => t.GetMethod(nameof(Reward.ToSerializable), BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .OfType<MethodInfo>();
        private static void Postfix(Reward __instance, SerializableReward __result) =>
            ExtendedSaveHandlers<Reward, SerializableReward>.ExtendedData[__result] =
                new ExtendedSaveHandlers<Reward, SerializableReward>.ExtendedSaveData(__instance);
    }

    [HarmonyPatch(typeof(Reward), MethodType.Constructor, [typeof(Player)])]
    private static class RewardCreated
    {
        private static void Postfix(Reward __instance, Player player)
        {
            if (RelicMetricSource.Attribution is { } source && ReferenceEquals(source.Owner, player))
                RewardSource.Set(__instance, source.Acquisition);
        }
    }

    [HarmonyPatch(typeof(RestSiteOption), MethodType.Constructor, [typeof(Player)])]
    private static class OptionCreated
    {
        private static void Postfix(RestSiteOption __instance, Player owner)
        {
            if (RelicMetricSource.Attribution is { } source && ReferenceEquals(source.Owner, owner))
                Options.Add(__instance, new(owner, source.Acquisition));
        }
    }

    private static IEnumerable<MethodInfo> SelectMethods(Type root) => root.Assembly.GetTypes()
        .Where(t => !t.IsAbstract && root.IsAssignableFrom(t))
        .Select(t => t.GetMethod("OnSelect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        .OfType<MethodInfo>().Distinct();

    private static object? FindSelection(object instance)
    {
        if (instance is Reward or RestSiteOption) return instance;
        return instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(f => typeof(Reward).IsAssignableFrom(f.FieldType) || typeof(RestSiteOption).IsAssignableFrom(f.FieldType))
            .Select(f => f.GetValue(instance)).FirstOrDefault(v => v != null);
    }

    private static RelicMetricSource.Target? EnterSelection(object? selection)
    {
        if (selection is Reward reward && RewardSource.Get(reward) is > 0 and var id)
            return RelicMetricSource.EnterDeferred(reward.Player, id);
        if (selection is RestSiteOption option && Options.TryGetValue(option, out var source))
            return RelicMetricSource.EnterDeferred(source.Owner, source.Acquisition);
        // A nested unrelated reward must not inherit the parent reward's origin.
        return RelicMetricSource.Enter(null);
    }

    [HarmonyPatch]
    private static class SelectionContext
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var method in SelectMethods(typeof(Reward)).Concat(SelectMethods(typeof(RestSiteOption))))
            {
                yield return method;
                if (method.GetCustomAttribute<AsyncStateMachineAttribute>() is { } async)
                    yield return AccessTools.Method(async.StateMachineType, "MoveNext");
            }
        }
        private static void Prefix(object __instance, out RelicMetricSource.Target? __state) =>
            __state = EnterSelection(FindSelection(__instance));
        private static Exception? Finalizer(Exception? __exception, RelicMetricSource.Target? __state)
        { RelicMetricSource.Restore(__state); return __exception; }
    }

    [HarmonyPatch]
    private static class SuccessfulOption
    {
        private static IEnumerable<MethodBase> TargetMethods() => SelectMethods(typeof(RestSiteOption));
        private static void Postfix(RestSiteOption __instance, ref Task<bool> __result)
        {
            if (Options.TryGetValue(__instance, out var source))
                __result = Observe(__result, source);
        }
        private static async Task<bool> Observe(Task<bool> result, OptionSource source)
        {
            bool success = await result;
            if (!success) return false;
            var previous = RelicMetricSource.EnterDeferred(source.Owner, source.Acquisition);
            try { RelicMetrics.Record(RelicMetricSource.Relic, "rest_actions_completed", 1, "Activations"); }
            finally { RelicMetricSource.Restore(previous); }
            return true;
        }
    }
}
