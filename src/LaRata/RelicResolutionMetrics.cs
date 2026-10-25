using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Hooks;

namespace LaRata;

// Finite native resolution boundaries. A tooltip repeatedly querying damage,
// healing, draw or energy values never enters one of these command boundaries.
internal static class RelicResolution
{
    private sealed record Frame(string Kind, Frame? Parent, bool Enabled = true)
    {
        internal readonly HashSet<(string Hook, object? Target, object? Phase)> Queries = new();
    }
    private static readonly AsyncLocal<Frame?> Current = new();
    private static readonly Dictionary<MethodBase, string> Kinds = new();
    internal static bool In(string kind)
    {
        for (var f = Current.Value; f != null; f = f.Parent) if (f.Kind == kind) return f.Enabled;
        return false;
    }

    [HarmonyPatch]
    private static class CalculationBoundary
    {
        private static readonly Dictionary<string, string> Commands = new()
        {
            ["ModifyDamage"] = "damage", ["ModifyHpLost"] = "damage", ["ModifyBlock"] = "block",
            ["ModifyGoldGained"] = "gold", ["ModifyEnergyGain"] = "energy",
            ["ModifyPowerAmountGiven"] = "power", ["ModifyHandDraw"] = "turn_setup",
            ["ModifyMaxEnergy"] = "refill", ["ModifyRestSiteHealAmount"] = "rest_heal"
        };
        private static IEnumerable<MethodBase> TargetMethods() => typeof(Hook).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => Commands.ContainsKey(m.Name));
        private static void Prefix(MethodBase __originalMethod, object[] __args, out object? __state)
        {
            __state = Current.Value;
            string command = Commands[__originalMethod.Name];
            Frame? resolution = Current.Value;
            while (resolution != null && resolution.Kind != command) resolution = resolution.Parent;
            string name = __originalMethod.Name;
            // HP-loss stages have separate before/after-Osty calls and may use
            // different actual targets during one redirected damage resolution.
            var key = (name, name is "ModifyHpLost" or "ModifyDamage" ? __args[2] : null,
                name == "ModifyHpLost" ? __args[7] : null);
            bool preview = __originalMethod.GetParameters().Select((p, i) => (p, i))
                .Any(pair => pair.p.Name == "previewMode" && __args[pair.i]?.ToString() != "None");
            bool allowed = resolution != null && !preview && resolution.Queries.Add(key);
            Current.Value = new("calculation:" + __originalMethod.Name, Current.Value, allowed);
        }
        private static Exception? Finalizer(Exception? __exception, object? __state)
        { Current.Value = (Frame?)__state; return __exception; }
    }

    [HarmonyPatch]
    private static class CommandBoundary
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var methods = new List<(Type Type, string Name, string Kind)>
            {
                (typeof(CreatureCmd), "Damage", "damage"),
                (typeof(CreatureCmd), "GainBlock", "block"),
                (typeof(PlayerCmd), "GainGold", "gold"),
                (typeof(PlayerCmd), "GainEnergy", "energy"),
                (typeof(PowerCmd), "Apply", "power"),
                (typeof(PowerCmd), "ModifyAmount", "power"),
                (typeof(CombatManager), "SetupPlayerTurn", "turn_setup"),
                (typeof(PlayerCombatState), "ResetEnergy", "refill"),
                (typeof(PlayerCombatState), "AddMaxEnergyToCurrent", "refill"),
                (typeof(HealRestSiteOption), "ExecuteRestSiteHeal", "rest_heal"),
                (typeof(CardCmd), "Upgrade", "upgrade")
            };
            foreach (var (type, name, kind) in methods)
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.Name == name && !m.ContainsGenericParameters))
                {
                    Kinds[method] = kind;
                    if (method.GetCustomAttribute<AsyncStateMachineAttribute>() is { } async)
                    {
                        var step = AccessTools.Method(async.StateMachineType, "MoveNext");
                        if (step != null) Kinds[step] = kind;
                    }
                }
            return Kinds.Keys;
        }
        private static void Prefix(MethodBase __originalMethod, out object? __state)
        {
            __state = Current.Value;
            string kind = Kinds[__originalMethod];
            // The outer async call already created this resolution frame. Reuse
            // the captured frame at each MoveNext so Changed/UI callbacks cannot
            // turn a repeated value query into another recorded contribution.
            if (__originalMethod.Name != "MoveNext" || Current.Value?.Kind != kind)
                Current.Value = new(kind, Current.Value);
        }
        private static Exception? Finalizer(Exception? __exception, object? __state)
        { Current.Value = (Frame?)__state; return __exception; }
    }
}

// Quantities here are adjustments to an actual calculation, not a counterfactual
// claim about final HP, draw count or gold. Later modifiers/clamping remain native.
[HarmonyPatch]
internal static class RelicNumericContribution
{
    private static readonly Dictionary<string, (string Boundary, string Key, string Unit)> Supported = new()
    {
        ["ModifyDamageAdditive"] = ("ModifyDamage", "damage_value_adjustment", "DamageValue"),
        ["ModifyDamageMultiplicative"] = ("ModifyDamage", "damage_value_adjustment", "DamageValue"),
        ["ModifyWeakMultiplier"] = ("ModifyDamage", "weak_multiplier_adjustment", "Multiplier"),
        ["ModifyVulnerableMultiplier"] = ("ModifyDamage", "vulnerable_multiplier_adjustment", "Multiplier"),
        ["ModifyHpLostBeforeOsty"] = ("ModifyHpLost", "hp_loss_adjustment", "HPValue"),
        ["ModifyHpLostAfterOsty"] = ("ModifyHpLost", "hp_loss_adjustment", "HPValue"),
        ["ModifyHpLostAfterOstyLate"] = ("ModifyHpLost", "hp_loss_adjustment", "HPValue"),
        ["ModifyBlockAdditive"] = ("ModifyBlock", "block_value_adjustment", "BlockValue"),
        ["ModifyBlockMultiplicative"] = ("ModifyBlock", "block_value_adjustment", "BlockValue"),
        ["ModifyGoldGained"] = ("ModifyGoldGained", "gold_gain_adjustment", "GoldValue"),
        ["ModifyEnergyGain"] = ("ModifyEnergyGain", "energy_gain_adjustment", "EnergyValue"),
        ["ModifyPowerAmountGivenAdditive"] = ("ModifyPowerAmountGiven", "power_value_adjustment", "PowerValue"),
        ["ModifyPowerAmountGivenMultiplicative"] = ("ModifyPowerAmountGiven", "power_value_adjustment", "PowerValue"),
        ["ModifyHandDraw"] = ("ModifyHandDraw", "hand_draw_adjustment", "RequestedCards"),
        ["ModifyHandDrawLate"] = ("ModifyHandDraw", "hand_draw_adjustment", "RequestedCards"),
        ["ModifyMaxEnergy"] = ("ModifyMaxEnergy", "energy_capacity_at_refill", "EnergyValue"),
        ["ModifyRestSiteHealAmount"] = ("ModifyRestSiteHealAmount", "rest_heal_adjustment", "HPValue")
    };
    private static IEnumerable<MethodBase> TargetMethods() => typeof(RelicModel).Assembly.GetTypes()
        .Where(t => typeof(RelicModel).IsAssignableFrom(t))
        .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        .Where(m => m.ReturnType == typeof(decimal) && Supported.ContainsKey(m.Name));
    private static void Postfix(RelicModel __instance, MethodBase __originalMethod, object[] __args, decimal __result)
    {
        var spec = Supported[__originalMethod.Name];
        if (!RelicResolution.In("calculation:" + spec.Boundary) || __instance.IsCanonical || __instance.IsMelted) return;
        var parameters = __originalMethod.GetParameters();
        int index = Array.FindIndex(parameters, p => p.ParameterType == typeof(decimal));
        if (index < 0 || __args[index] is not decimal before) return;
        decimal delta = __originalMethod.Name.EndsWith("Additive", StringComparison.Ordinal) ? __result :
            __originalMethod.Name.EndsWith("Multiplicative", StringComparison.Ordinal) ? before * (__result - 1) : __result - before;
        RelicMetrics.Record(__instance, spec.Key, delta, spec.Unit, "Contribution");
    }
}
