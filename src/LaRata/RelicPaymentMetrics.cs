using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace LaRata;

// Native modifier objects retain their origin, but applying a temporary discount
// is not counted as saving anything. Only a completed native payment consumes it.
internal static class RelicPaymentMetrics
{
    private sealed record Step(RelicMetricSource.Target? Source, decimal Before, decimal After);
    private sealed class Payment(CardModel card)
    {
        internal readonly CardModel Card = card;
        internal readonly List<Step> Energy = [], Stars = [];
        internal string? Phase;
        internal bool EnergyQueried, StarsQueried;
        internal int? EnergyCost, StarCost, EnergyPaid, StarsPaid;
        internal int? RequestedEnergy, RequestedStars;
        internal bool Committed;
    }
    private sealed record Scope(Payment? Previous, Payment Current);
    private static readonly AsyncLocal<Payment?> Current = new();
    private static readonly ConditionalWeakTable<LocalCostModifier, RelicMetricSource.Target> LocalSources = new();
    private static readonly ConditionalWeakTable<TemporaryCardCost, RelicMetricSource.Target> StarSources = new();
    private static readonly FieldInfo CombatPlayer = AccessTools.Field(typeof(PlayerCombatState), "_player");
    private static readonly FieldInfo LocalModifiers = AccessTools.Field(typeof(CardEnergyCost), "_localModifiers");
    private static List<LocalCostModifier> Modifiers(CardEnergyCost cost) => (List<LocalCostModifier>)LocalModifiers.GetValue(cost)!;

    internal static void Assigned(object target, string field, int before, int after)
    {
        ResetEnergyLedger.ObserveEnergyChange(target, field, before, after);
        var payment = Current.Value;
        if (payment == null || target is not PlayerCombatState state || CombatPlayer.GetValue(state) != payment.Card.Owner) return;
        if (field == "_energy" && payment.RequestedEnergy > 0 && payment.EnergyPaid == null)
        {
            payment.EnergyPaid = Math.Max(0, before - after);
            if (payment.EnergyPaid > 0) ResetEnergyLedger.RecordPayment(payment.Card, payment.EnergyPaid.Value);
        }
        if (field == "_stars" && payment.RequestedStars > 0 && payment.StarsPaid == null)
            payment.StarsPaid = Math.Max(0, before - after);
    }

    private static void Append(RelicMetricSource.Target? source, decimal before, decimal after)
    {
        if (Current.Value is not { Phase: not null } payment || before == after) return;
        (payment.Phase == "energy" ? payment.Energy : payment.Stars).Add(new(source, before, after));
    }

    [HarmonyPatchCategory(RelicMetrics.PatchCategory)]
    [HarmonyPatch(typeof(LocalCostModifier), MethodType.Constructor,
        [typeof(int), typeof(LocalCostType), typeof(LocalCostModifierExpiration), typeof(bool)])]
    private static class LocalCreated
    {
        private static void Postfix(LocalCostModifier __instance)
        { if (RelicMetricSource.Attribution is { } source) LocalSources.Add(__instance, source); }
    }
    // Observe the native list mutation as well as construction: a tiny native
    // constructor can already be inlined in a caller before Harmony patches it.
    // The instances actually appended by AddUntilPlayed still have exact origin.
    [HarmonyPatchCategory(RelicMetrics.PatchCategory)]
    [HarmonyPatch]
    private static class LocalAttached
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (string name in new[] { "SetUntilPlayed", "SetThisTurnOrUntilPlayed", "SetThisTurn", "SetThisCombat",
                "AddUntilPlayed", "AddThisTurnOrUntilPlayed", "AddThisCombat" })
                yield return AccessTools.Method(typeof(CardEnergyCost), name);
        }
        private static void Prefix(CardEnergyCost __instance, out (int Count, RelicMetricSource.Target? Source) __state) =>
            __state = (Modifiers(__instance).Count, RelicMetricSource.Attribution);
        private static void Postfix(CardEnergyCost __instance, (int Count, RelicMetricSource.Target? Source) __state)
        {
            var modifiers = Modifiers(__instance);
            for (int i = __state.Count; i < modifiers.Count; i++)
            {
                LocalSources.Remove(modifiers[i]);
                if (__state.Source != null) LocalSources.Add(modifiers[i], __state.Source);
            }
        }
    }
    [HarmonyPatchCategory(RelicMetrics.PatchCategory)]
    [HarmonyPatch(typeof(LocalCostModifier), nameof(LocalCostModifier.Clone))]
    private static class LocalCloned
    {
        private static void Postfix(LocalCostModifier __instance, LocalCostModifier __result)
        {
            LocalSources.Remove(__result);
            if (LocalSources.TryGetValue(__instance, out var source)) LocalSources.Add(__result, source);
        }
    }
    [HarmonyPatchCategory(RelicMetrics.PatchCategory)]
    [HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.Clone))]
    private static class CostCloned
    {
        private static void Postfix(CardEnergyCost __instance, CardEnergyCost __result)
        {
            var before = Modifiers(__instance); var after = Modifiers(__result);
            for (int i = 0; i < Math.Min(before.Count, after.Count); i++)
            {
                LocalSources.Remove(after[i]);
                if (LocalSources.TryGetValue(before[i], out var source)) LocalSources.Add(after[i], source);
            }
        }
    }
    [HarmonyPatchCategory(RelicMetrics.PatchCategory)]
    [HarmonyPatch(typeof(CardModel), "AddTemporaryStarCost")]
    private static class StarCreated
    {
        private static void Prefix(TemporaryCardCost cost)
        {
            StarSources.Remove(cost);
            if (RelicMetricSource.Attribution is { } source) StarSources.Add(cost, source);
        }
    }
    [HarmonyPatchCategory(RelicMetrics.PatchCategory)]
    [HarmonyPatch(typeof(LocalCostModifier), nameof(LocalCostModifier.Modify))]
    private static class LocalApplied
    {
        private static void Postfix(LocalCostModifier __instance, int currentCost, int __result)
        {
            if (Current.Value?.Phase != "energy") return;
            LocalSources.TryGetValue(__instance, out var source);
            Append(source, currentCost, __result);
        }
    }
    [HarmonyPatchCategory(RelicMetrics.PatchCategory)]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.CurrentStarCost), MethodType.Getter)]
    private static class StarApplied
    {
        private static void Postfix(CardModel __instance, int __result)
        {
            if (Current.Value is not { Phase: "stars" } payment || payment.Card != __instance ||
                __instance.TemporaryStarCost is not { } temporary || __instance.HasStarCostX) return;
            StarSources.TryGetValue(temporary, out var source);
            Append(source, __instance.BaseStarCost, __result);
        }
    }

    [HarmonyPatchCategory(RelicMetrics.PatchCategory)]
    [HarmonyPatch]
    private static class GlobalApplied
    {
        private static IEnumerable<MethodBase> TargetMethods() => new[] { typeof(AbstractModel).Assembly, typeof(RelicPaymentMetrics).Assembly }
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && typeof(AbstractModel).IsAssignableFrom(t))
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(m => m.Name is "TryModifyEnergyCostInCombat" or "TryModifyEnergyCostInCombatLate" or "TryModifyStarCost");
        private static void Postfix(AbstractModel __instance, CardModel card, decimal originalCost, decimal modifiedCost,
            MethodBase __originalMethod)
        {
            if (Current.Value is not { Phase: not null } payment || payment.Card != card || originalCost == modifiedCost) return;
            if (payment.Phase != (__originalMethod.Name == "TryModifyStarCost" ? "stars" : "energy")) return;
            RelicMetricSource.Target? source = null;
            if (__instance is RelicModel relic)
            {
                var previous = RelicMetricSource.Enter(relic);
                source = RelicMetricSource.Attribution;
                RelicMetricSource.Restore(previous);
            }
            Append(source, originalCost, modifiedCost);
        }
    }

    [HarmonyPatchCategory(RelicMetrics.PatchCategory)]
    [HarmonyPatch(typeof(CardEnergyCost), nameof(CardEnergyCost.GetWithModifiers))]
    private static class UnrelatedCalculation
    {
        private static void Prefix(CardEnergyCost __instance, out string? __state)
        {
            __state = Current.Value?.Phase;
            if (Current.Value is { } payment && !ReferenceEquals(__instance, payment.Card.EnergyCost))
                payment.Phase = null;
        }
        private static Exception? Finalizer(Exception? __exception, string? __state)
        { if (Current.Value is { } payment) payment.Phase = __state; return __exception; }
    }

    [HarmonyPatchCategory(RelicMetrics.PatchCategory)]
    [HarmonyPatch]
    private static class Calculation
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(CardEnergyCost), nameof(CardEnergyCost.GetAmountToSpend));
            yield return AccessTools.Method(typeof(CardModel), nameof(CardModel.GetStarCostWithModifiers));
        }
        private static void Prefix(object __instance, out string? __state)
        {
            var payment = Current.Value;
            __state = payment?.Phase;
            if (payment == null) return;
            if (ReferenceEquals(__instance, payment.Card.EnergyCost) && !payment.EnergyQueried)
            { payment.EnergyQueried = true; payment.Phase = "energy"; }
            else if (ReferenceEquals(__instance, payment.Card) && !payment.StarsQueried)
            { payment.StarsQueried = true; payment.Phase = "stars"; }
            else payment.Phase = null;
        }
        private static void Postfix(int __result)
        {
            if (Current.Value is not { } payment) return;
            if (payment.Phase == "energy") payment.EnergyCost = __result;
            if (payment.Phase == "stars") payment.StarCost = Math.Max(0, __result);
        }
        private static Exception? Finalizer(Exception? __exception, string? __state)
        { if (Current.Value is { } payment) payment.Phase = __state; return __exception; }
    }

    [HarmonyPatchCategory(RelicMetrics.PatchCategory)]
    [HarmonyPatch]
    private static class DebitRequested
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(CardModel), "SpendEnergy");
            yield return AccessTools.Method(typeof(CardModel), "SpendStars");
        }
        private static void Prefix(CardModel __instance, int amount, MethodBase __originalMethod)
        {
            if (Current.Value is not { } payment || payment.Card != __instance) return;
            if (__originalMethod.Name == "SpendEnergy")
            { payment.RequestedEnergy = amount; if (amount == 0) payment.EnergyPaid = 0; }
            else { payment.RequestedStars = amount; if (amount == 0) payment.StarsPaid = 0; }
        }
    }

    [HarmonyPatchCategory(RelicMetrics.PatchCategory)]
    [HarmonyPatch]
    private static class Boundary
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var method = AccessTools.Method(typeof(CardModel), nameof(CardModel.SpendResources));
            yield return method;
            yield return AccessTools.Method(method.GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType, "MoveNext");
        }
        private static void Prefix(object __instance, MethodBase __originalMethod, out Scope __state)
        {
            var card = __instance as CardModel ?? __instance.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => typeof(CardModel).IsAssignableFrom(f.FieldType))
                .Select(f => f.GetValue(__instance) as CardModel).First(c => c != null)!;
            var previous = Current.Value;
            var payment = __originalMethod.Name == "MoveNext" && previous?.Card == card ? previous : new Payment(card);
            __state = new(previous, payment);
            Current.Value = payment;
        }
        private static Exception? Finalizer(Exception? __exception, Scope __state)
        { Current.Value = __state.Previous; return __exception; }
    }
    [HarmonyPatchCategory(RelicMetrics.PatchCategory)]
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.SpendResources))]
    private static class Completed
    {
        private static void Postfix(ref Task<(int, int)> __result)
        { if (Current.Value is { } payment) __result = Observe(__result, payment); }
        private static async Task<(int, int)> Observe(Task<(int, int)> result, Payment payment)
        {
            var paid = await result;
            if (payment.Committed || !ReferenceEquals(payment.Card.Owner.RunState, RunManager.Instance.DebugOnlyGetState())) return paid;
            payment.Committed = true;
            Commit(payment.Energy, "energy", "EnergyValue", payment.EnergyCost, payment.EnergyPaid, paid.Item1);
            Commit(payment.Stars, "stars", "StarValue", payment.StarCost, payment.StarsPaid, paid.Item2);
            return paid;
        }
    }

    private static void Commit(List<Step> steps, string resource, string unit, int? finalCost, int? actualPaid, int requested)
    {
        bool exact = steps.Count == 1 && steps[0].Source != null && steps[0].Before >= 0 && steps[0].After >= 0 &&
            finalCost == actualPaid && actualPaid == requested && actualPaid == Math.Max(0, (int)steps[0].After);
        if (!exact)
        {
            foreach (var source in steps.Select(s => s.Source).Where(s => s != null).Distinct())
                RelicMetrics.MarkDeferred(source, "complex_payments_deferred");
            return;
        }
        foreach (var step in steps.Where(s => s.Source != null))
        {
            int delta = (int)step.Before - (int)step.After;
            RelicMetrics.RecordTarget(step.Source, resource + (delta > 0 ? "_saved" : "_surcharge_paid"),
                Math.Abs((decimal)delta), resource == "energy" ? "Energy" : "Stars");
        }
    }
}
