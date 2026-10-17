using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

[HarmonyPatch(typeof(CombatState), nameof(CombatState.CreateCreature))]
internal static class CreatureHpPatch
{
    [HarmonyPostfix]
    private static void Postfix(CombatState __instance, Creature __result)
    {
        var rules = EncounterDirector.Get(__instance.Encounter);
        if (rules == null || __result.Side != CombatSide.Enemy || __result.MaxHp <= 1) return;
        decimal factor = ReferenceEquals(__result.Monster, rules.Extra) ? rules.ExtraHpFactor :
            1m + rules.Plan.HpPercent / 100m;
        __result.SetMaxHpInternal(Math.Max(1, decimal.Ceiling(__result.MaxHp * factor)));
        __result.SetCurrentHpInternal(__result.MaxHp);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
internal static class DamagePatch
{
    // Modify base damage BEFORE native Weak, Vulnerable, Intangible and damage caps.
    // Both intent previews and actual attacks traverse exactly this hook.
    [HarmonyPrefix]
    private static void Prefix(ICombatState? combatState, Creature? dealer, ValueProp props,
        ModifyDamageHookType modifyDamageHookType, ref decimal damage)
    {
        if (dealer?.Side != CombatSide.Enemy || !props.HasFlag(ValueProp.Move) ||
            !modifyDamageHookType.HasFlag(ModifyDamageHookType.Multiplicative)) return;
        var rules = EncounterDirector.Get(combatState?.Encounter);
        if (rules == null) return;
        decimal factor = ReferenceEquals(dealer.Monster, rules.Extra) ? (rules.BossDuo ? 1m : .65m) :
            1m + rules.Plan.DamagePercent / 100m;
        damage *= factor;
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyPowerAmountGiven))]
internal static class PowerPotencyPatch
{
    [HarmonyPrefix]
    private static void Prefix(ICombatState combatState, PowerModel power, Creature giver,
        Creature? target, ref decimal amount)
    {
        var rules = EncounterDirector.Get(combatState.Encounter);
        if (rules == null || giver.Side != CombatSide.Enemy || !ReferenceEquals(giver.Monster, rules.Leader) ||
            amount <= 0 || amount > 3) return;
        // Never scale structural powers (phases, countdowns, immunities, scripted instant kills).
        // Leave 99-turn effects and negative adjustments alone; Artifact still runs afterwards.
        if (target?.Side == CombatSide.Enemy && power is StrengthPower or PlatingPower)
            amount += rules.Plan.BuffBonus;
        if (target?.Side == CombatSide.Player && power is WeakPower or FrailPower or VulnerablePower)
            amount = Math.Min(3, amount + rules.Plan.DebuffBonus);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
internal static class StartingBuffsPatch
{
    [HarmonyPostfix]
    private static void Postfix(ICombatState? combatState, ref Task __result) => __result = Apply(__result, combatState);

    private static async Task Apply(Task original, ICombatState? combat)
    {
        await original;
        var rules = EncounterDirector.Get(combat?.Encounter);
        if (rules == null || rules.Started || !rules.Leader.Creature.IsAlive) return;
        rules.Started = true;
        var target = rules.Leader.Creature;
        var context = new ThrowingPlayerChoiceContext();
        // Native Plating handles its own multiplayer scaling and decay.
        // No applier: these are encounter grants, not another amplified monster ability.
        if (rules.Plan.Plating > 0)
            await PowerCmd.Apply<PlatingPower>(context, target, rules.Plan.Plating, null, null);
        if (rules.Plan.Artifact > 0)
            await PowerCmd.Apply<ArtifactPower>(context, target, rules.Plan.Artifact, null, null);
    }
}

internal sealed record ExtraHex(MonsterModel Monster, int Kind, int Amount);

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.SetUpForCombat))]
internal static class HexTelegraphPatch
{
    internal static readonly ConditionalWeakTable<MoveState, ExtraHex> Hexes = new();

    [HarmonyPostfix]
    private static void Postfix(MonsterModel __instance)
    {
        var rules = EncounterDirector.Get(__instance.CombatState.Encounter);
        if (rules == null || !ReferenceEquals(rules.Leader, __instance) || rules.Plan.Hex == 0) return;
        var moves = __instance.MoveStateMachine!.States.Values.OfType<MoveState>()
            .Where(x => x.Intents.Any(i => i is AttackIntent or BuffIntent or DefendIntent or DebuffIntent))
            .OrderBy(x => x.Intents.Any(i => i is AttackIntent))
            .ThenBy(x => x.Id, StringComparer.Ordinal).ToArray();
        if (moves.Length == 0) return;
        var move = moves[0];
        // Keep the original move object and all references/branch conditions intact.
        var intents = move.Intents.ToList();
        if (!intents.Any(x => x is DebuffIntent)) intents.Add(new DebuffIntent());
        AccessTools.PropertySetter(typeof(MoveState), nameof(MoveState.Intents)).Invoke(move, [intents]);
        Hexes.Add(move, new(__instance, rules.Plan.Hex, rules.Plan.HexAmount));
        ModEntry.Log.Info($"TELEGRAPH monster={__instance.Id.Entry} move={move.Id} hex={EncounterPlan.HexName(rules.Plan.Hex)} amount={rules.Plan.HexAmount}");
    }
}

[HarmonyPatch(typeof(MoveState), nameof(MoveState.PerformMove))]
internal static class HexExecutionPatch
{
    [HarmonyPostfix]
    private static void Postfix(MoveState __instance, IEnumerable<Creature> targets, ref Task __result)
    {
        if (HexTelegraphPatch.Hexes.TryGetValue(__instance, out var hex))
            __result = Apply(__result, hex, targets.ToArray());
    }

    private static async Task Apply(Task original, ExtraHex hex, IReadOnlyList<Creature> targets)
    {
        await original;
        if (!hex.Monster.Creature.IsAlive || CombatManager.Instance.IsEnding) return;
        var living = targets.Where(x => x.Side == CombatSide.Player && x.IsAlive).ToArray();
        var context = new ThrowingPlayerChoiceContext();
        switch (hex.Kind)
        {
            case 1: await PowerCmd.Apply<WeakPower>(context, living, hex.Amount, hex.Monster.Creature, null); break;
            case 2: await PowerCmd.Apply<FrailPower>(context, living, hex.Amount, hex.Monster.Creature, null); break;
            case 3: await PowerCmd.Apply<VulnerablePower>(context, living, hex.Amount, hex.Monster.Creature, null); break;
        }
        ModEntry.Log.Info($"HEX resolved monster={hex.Monster.Id.Entry} targets={living.Length}");
    }
}
