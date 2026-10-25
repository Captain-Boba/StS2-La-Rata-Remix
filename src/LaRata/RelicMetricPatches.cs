using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace LaRata;

// A private observation context, not a gameplay dispatcher. In particular, a card
// or power triggered by a relic masks the relic: its whole output is not credited
// to the relic that happened to draw/apply it. Await continuations enter/leave at
// each native MoveNext, so parallel players cannot share a process-global source.
internal static class RelicMetricSource
{
    internal sealed record Target(Player Owner, int Acquisition, string ModelId, RelicModel? Live);
    private static readonly AsyncLocal<Target?> Current = new();
    internal static RelicModel? Relic => Current.Value?.Live;
    internal static Target? Attribution => Current.Value;
    internal static Target? Enter(RelicModel? relic)
    {
        var previous = Current.Value;
        int acquisition = relic is { HasBeenRemovedFromState: false, IsMelted: false, Owner: not null }
            ? RelicMetrics.Identity(relic) : 0;
        Current.Value = acquisition > 0 ? new(relic!.Owner!, acquisition, relic.Id.ToString(), relic) : null;
        return previous;
    }
    internal static Target? EnterDeferred(Player owner, int acquisition)
    {
        var live = owner.Relics.FirstOrDefault(r => RelicMetrics.Acquisition.Get(r) == acquisition);
        if (live != null) return Enter(live);
        var previous = Current.Value;
        var archived = RelicMetrics.ReadRemoved(owner.RunState).FirstOrDefault(r =>
            r.OwnerId == owner.NetId && r.AcquisitionOrdinal == acquisition);
        Current.Value = archived == null ? null : new(owner, acquisition, archived.ModelId, null);
        return previous;
    }
    internal static void Restore(Target? previous) => Current.Value = previous;

    private static RelicModel? Find(object? instance, int depth = 0)
    {
        if (instance is RelicModel relic) return relic;
        if (instance == null || depth > 2) return null;
        foreach (var field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (typeof(RelicModel).IsAssignableFrom(field.FieldType)) return field.GetValue(instance) as RelicModel;
            if (field.Name.Contains("this", StringComparison.Ordinal) || field.Name.Contains("locals", StringComparison.Ordinal))
            {
                var found = Find(field.GetValue(instance), depth + 1);
                if (found != null) return found;
            }
        }
        return null;
    }

    [HarmonyPatch]
    [HarmonyPatchCategory("LaRataRelicEffectContexts")]
    private static class EffectContext
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var result = new HashSet<MethodBase>();
            var assemblies = new[] { typeof(RelicModel).Assembly, typeof(RelicMetrics).Assembly };
            foreach (var type in assemblies.SelectMany(a => a.GetTypes()).Where(t => t != typeof(RelicModel) && typeof(AbstractModel).IsAssignableFrom(t)))
            {
                bool isRelic = typeof(RelicModel).IsAssignableFrom(type);
                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (method.IsAbstract || method.IsSpecialName || method.ContainsGenericParameters) continue;
                    bool provenanceFactory = isRelic && method.ReturnType == typeof(bool) &&
                        method.Name is "TryModifyRewards" or "TryModifyRewardsLate" or
                            "TryModifyRestSiteHealRewards" or "TryModifyRestSiteOptions";
                    if (method.ReturnType != typeof(void) && !typeof(Task).IsAssignableFrom(method.ReturnType) && !provenanceFactory) continue;
                    if (!isRelic && method.GetBaseDefinition() == method) continue;
                    result.Add(method);
                    if (method.GetCustomAttribute<AsyncStateMachineAttribute>() is { } async)
                    {
                        var step = AccessTools.Method(async.StateMachineType, "MoveNext");
                        if (step != null) result.Add(step);
                    }
                }
                if (!isRelic) continue;
                // Deferred native relic choices (e.g. a rest-site option) retain
                // their owner through the compiler-generated closure.
                foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                    foreach (var method in nested.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                        if (!method.IsSpecialName && !method.ContainsGenericParameters &&
                            (method.ReturnType == typeof(void) || typeof(Task).IsAssignableFrom(method.ReturnType)))
                            result.Add(method);
            }
            return result;
        }
        private static void Prefix(object __instance, MethodBase __originalMethod, out Target? __state)
        {
            var relic = Find(__instance);
            // A suspended replacement effect can resume after removing itself.
            // Its captured context may finish; a NEW call on a removed relic
            // still fails the normal Enter guard.
            if (__originalMethod.Name == "MoveNext" && relic != null &&
                ReferenceEquals(Current.Value?.Live, relic) && relic.HasBeenRemovedFromState)
            { __state = Current.Value; return; }
            __state = Enter(relic);
        }
        // Preserve original failures. A statistic must never hide a game exception.
        private static Exception? Finalizer(Exception? __exception, Target? __state)
        { Restore(__state); return __exception; }
    }
}

// Observe immediately AFTER the actual scalar field assignment and BEFORE its
// Changed callbacks. A before/after Task delta would include recursive effects.
// These seven native fields are intentionally finite and game-version-pinned.
[HarmonyPatch]
internal static class RelicScalarObservation
{
    private static readonly string[] Fields = ["_block", "_currentHp", "_maxHp", "_gold", "_energy", "_stars", "_amount"];
    private static readonly FieldInfo CombatPlayer = AccessTools.Field(typeof(PlayerCombatState), "_player");
    private static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.PropertySetter(typeof(Creature), nameof(Creature.Block));
        yield return AccessTools.PropertySetter(typeof(Creature), nameof(Creature.CurrentHp));
        yield return AccessTools.PropertySetter(typeof(Creature), nameof(Creature.MaxHp));
        yield return AccessTools.PropertySetter(typeof(Player), nameof(Player.Gold));
        yield return AccessTools.PropertySetter(typeof(PlayerCombatState), nameof(PlayerCombatState.Energy));
        yield return AccessTools.PropertySetter(typeof(PlayerCombatState), nameof(PlayerCombatState.Stars));
        yield return AccessTools.Method(typeof(PowerModel), nameof(PowerModel.SetAmount));
    }
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode != OpCodes.Stfld || instruction.operand is not FieldInfo field || !Fields.Contains(field.Name))
            { yield return instruction; continue; }
            var old = generator.DeclareLocal(typeof(int));
            var next = generator.DeclareLocal(typeof(int));
            var first = new CodeInstruction(OpCodes.Stloc, next);
            first.labels.AddRange(instruction.labels);
            first.blocks.AddRange(instruction.blocks);
            instruction.labels.Clear(); instruction.blocks.Clear();
            yield return first;
            yield return new(OpCodes.Dup);
            yield return new(OpCodes.Ldfld, field);
            yield return new(OpCodes.Stloc, old);
            yield return new(OpCodes.Ldloc, next);
            yield return instruction;
            yield return new(OpCodes.Ldarg_0);
            yield return new(OpCodes.Ldstr, field.Name);
            yield return new(OpCodes.Ldloc, old);
            yield return new(OpCodes.Ldloc, next);
            yield return CodeInstruction.Call(typeof(RelicScalarObservation), nameof(Observe));
        }
    }
    internal static void Observe(object target, string field, int before, int after)
    {
        RelicPaymentMetrics.Assigned(target, field, before, after);
        var source = RelicMetricSource.Attribution;
        decimal delta = (decimal)after - before;
        if (source == null || delta == 0) return;
        if (target is PowerModel unattached && unattached.Owner == null) return;
        bool ownerTarget = target switch
        {
            Creature c => c.Player == source.Owner || c.PetOwner == source.Owner,
            Player p => p == source.Owner,
            PlayerCombatState p => CombatPlayer.GetValue(p) == source.Owner,
            PowerModel p => p.Owner?.Player == source.Owner,
            _ => false
        };
        string suffix = ownerTarget ? "" : ":other";
        var (key, unit) = field switch
        {
            "_block" => (delta > 0 ? "block_gained" : "block_removed", "Block"),
            "_currentHp" => (delta > 0 ? "hp_restored" : "hp_lost", "HP"),
            "_maxHp" => (delta > 0 ? "max_hp_gained" : "max_hp_lost", "HP"),
            "_gold" => (delta > 0 ? "gold_gained" : "gold_lost", "Gold"),
            "_energy" => (delta > 0 ? "energy_gained" : "energy_lost", "Energy"),
            "_stars" => (delta > 0 ? "stars_gained" : "stars_lost", "Stars"),
            "_amount" when target is PowerModel power => (delta > 0 ? "power_applied" : "power_removed", "PowerStacks:" + power.Id),
            _ => ("", "")
        };
        if (key.Length != 0) RelicMetrics.Record(source.Live, key + suffix, Math.Abs(delta), unit);
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.AddRelicInternal))]
internal static class RelicMetricsAcquired
{
    private static void Postfix(RelicModel relic, bool silent)
    {
        if (silent) return;
        RelicMetrics.Record(RelicMetricSource.Relic, "relics_gained", 1, "Relics");
        RelicMetrics.Obtained(relic);
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.FinalizeStartingRelics))]
internal static class RelicMetricsNewRun
{
    private static void Prefix(RunManager __instance)
    {
        // Native finalization runs only for a new run, never during load.
        var run = __instance.DebugOnlyGetState();
        if (run == null) return;
        foreach (var player in run.Players)
            foreach (var relic in player.Relics)
                RelicMetrics.Obtained(relic);
    }
}

[HarmonyPatch(typeof(RelicModel), nameof(RelicModel.ToSerializable))]
internal static class RelicMetricsSaveIdentity
{
    private static void Prefix(RelicModel __instance) => RelicMetrics.Identity(__instance);
}

[HarmonyPatch(typeof(Player), nameof(Player.RemoveRelicInternal))]
internal static class RelicMetricsRemoved
{
    private static void Postfix(RelicModel relic, bool silent)
    {
        if (silent) return;
        RelicMetrics.RecordCommittedRemoval(RelicMetricSource.Relic);
        // Postfix only: failed native removal is not a historical removal.
        // Self-removal includes its committed final event in the archived ledger.
        RelicMetrics.Archive(relic);
    }
}

[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.CardDrawn))]
internal static class RelicMetricsDraw
{
    private static void Postfix() => RelicMetrics.Record(RelicMetricSource.Relic, "cards_drawn", 1, "Cards");
}

[HarmonyPatch(typeof(CardPile), nameof(CardPile.AddInternal))]
internal static class RelicMetricsDeckAdd
{
    private static void Postfix(CardPile __instance, CardModel card)
    {
        RelicGeneratedCardMetrics.Added(card);
        string? key = __instance.Type switch
        {
            PileType.Deck => "cards_added",
            PileType.Exhaust => "cards_exhausted",
            PileType.Discard => "cards_moved_to_discard",
            _ => null
        };
        if (key != null) RelicMetrics.Record(RelicMetricSource.Relic, key, 1, "Cards");
    }
}

[HarmonyPatch(typeof(CardPile), nameof(CardPile.RemoveInternal))]
internal static class RelicMetricsDeckRemove
{
    private static void Postfix(CardPile __instance)
    { if (__instance.Type == PileType.Deck) RelicMetrics.Record(RelicMetricSource.Relic, "cards_removed", 1, "Cards"); }
}
