using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;

namespace LaRata;

// Install uncategorized, BEFORE the Reset category recompiles payment/refund
// callers. Observe only Energy's actual write, before its reentrant callbacks.
// This is gameplay bookkeeping; it never activates any relic statistics patch.
[HarmonyPatch(typeof(PlayerCombatState), nameof(PlayerCombatState.Energy), MethodType.Setter)]
internal static class ResetEnergyAssignmentPatch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var code = instructions.ToList();
        var field = AccessTools.Field(typeof(PlayerCombatState), "_energy");
        if (code.Count(i => i.opcode == OpCodes.Stfld && Equals(i.operand, field)) != 1)
            throw new InvalidOperationException("Reset requires exactly one native Energy assignment.");
        foreach (var instruction in code)
        {
            if (instruction.opcode != OpCodes.Stfld || !Equals(instruction.operand, field))
            { yield return instruction; continue; }
            var old = generator.DeclareLocal(typeof(int));
            var next = generator.DeclareLocal(typeof(int));
            var first = new CodeInstruction(OpCodes.Stloc, next);
            first.labels.AddRange(instruction.labels); instruction.labels.Clear();
            first.blocks.AddRange(instruction.blocks); instruction.blocks.Clear();
            yield return first;
            yield return new(OpCodes.Dup);
            yield return new(OpCodes.Ldfld, field);
            yield return new(OpCodes.Stloc, old);
            yield return new(OpCodes.Ldloc, next);
            yield return instruction;
            yield return new(OpCodes.Ldarg_0);
            yield return new(OpCodes.Ldstr, "_energy");
            yield return new(OpCodes.Ldloc, old);
            yield return new(OpCodes.Ldloc, next);
            yield return CodeInstruction.Call(typeof(ResetEnergyLedger), nameof(ResetEnergyLedger.ObserveEnergyChange));
        }
    }
}
