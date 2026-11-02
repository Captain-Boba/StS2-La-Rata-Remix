using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace LaRata;

// Only Lord Tibo's Reset uses this state. Native combat-entry reload discards it;
// replay rebuilds it from actual payments, including payments before Reset exists.
internal static class ResetEnergyLedger
{
    internal const string PatchCategory = "LaRataResetRefund";
    private sealed class Turn
    {
        internal readonly object Sync = new();
        internal readonly Dictionary<ulong, long> Pending = [];
        internal long Generation;
    }
    private sealed class Credit(Turn turn, long generation, Player player, long amount)
    {
        internal readonly Turn Turn = turn;
        internal readonly long Generation = generation;
        internal readonly Player Player = player;
        internal readonly PlayerCombatState State = player.PlayerCombatState!;
        internal readonly long Amount = amount;
        internal long Credited;
        internal object? RootCommand;
        internal bool AwaitingWrite;
    }
    private static readonly ConditionalWeakTable<ICombatState, Turn> Turns = new();
    private static readonly AsyncLocal<Credit?> CurrentCredit = new();
    private static readonly AsyncLocal<Credit?> CurrentWrite = new();
    private static readonly AsyncLocal<object?> CurrentCommand = new();
    private sealed class Debit(PlayerCombatState state, CardModel card)
    {
        internal readonly PlayerCombatState State = state;
        internal readonly CardModel Card = card;
        internal bool AwaitingWrite = true;
    }
    private static readonly AsyncLocal<Debit?> CurrentDebit = new();

    // Only the real native card-payment call arms this receipt. Ordinary energy
    // losses, previews, resource gains and nested Changed callbacks are excluded.
    private static void PayWithReceipt(PlayerCombatState state, decimal amount, CardModel card)
    {
        var previous = CurrentDebit.Value;
        CurrentDebit.Value = new(state, card);
        try { state.LoseEnergy(amount); }
        finally { CurrentDebit.Value = previous; }
    }

    // Called once by our independent energy assignment observer after the real
    // debit and before EnergyChanged/AfterEnergySpent can suspend or reenter.
    internal static void RecordPayment(CardModel card, int amount)
    {
        // Reset's own native debit is included before its OnPlay refund begins.
        if (amount <= 0 || card.IsCanonical ||
            card.CombatState is not { } combat || card.Owner.PlayerCombatState == null ||
            CombatManager.Instance.IsOverOrEnding) return;
        var turn = Turns.GetValue(combat, _ => new());
        lock (turn.Sync)
            turn.Pending[card.Owner.NetId] = checked(turn.Pending.GetValueOrDefault(card.Owner.NetId) + amount);
    }

    internal static IReadOnlyDictionary<ulong, long> Pending(ICombatState combat)
    {
        var turn = Turns.GetValue(combat, _ => new());
        lock (turn.Sync) return new Dictionary<ulong, long>(turn.Pending);
    }

    private static void Clear(ICombatState? combat)
    {
        if (combat == null) return;
        var turn = Turns.GetValue(combat, _ => new());
        lock (turn.Sync)
        {
            turn.Generation++;
            turn.Pending.Clear();
        }
    }

    internal static async Task Refund(ICombatState combat)
    {
        var turn = Turns.GetValue(combat, _ => new());
        var credits = new List<Credit>();
        // Reserve every recipient before any await. Other Reset copies cannot
        // claim these same payments while an energy modifier is resolving.
        lock (turn.Sync)
            foreach (var player in combat.Players.OrderBy(p => p.NetId))
            {
                long amount = turn.Pending.GetValueOrDefault(player.NetId);
                if (amount <= 0 || !player.Creature.IsAlive || player.PlayerCombatState == null) continue;
                turn.Pending[player.NetId] = 0;
                credits.Add(new(turn, turn.Generation, player, amount));
            }
        try
        {
            foreach (var credit in credits)
            {
                if (!CanCredit(credit)) continue;
                var previous = CurrentCredit.Value;
                CurrentCredit.Value = credit;
                try { await PlayerCmd.GainEnergy(credit.Amount, credit.Player); }
                finally { CurrentCredit.Value = previous; }
            }
        }
        finally
        {
            // A blocked/capped gain did not refund that part of the payment.
            // Retain it for another Reset, but never carry it into a later turn.
            lock (turn.Sync)
                foreach (var credit in credits)
                    if (turn.Generation == credit.Generation && credit.Credited < credit.Amount)
                        turn.Pending[credit.Player.NetId] = checked(turn.Pending.GetValueOrDefault(credit.Player.NetId) + credit.Amount - credit.Credited);
        }
    }

    private static bool CanCredit(Credit credit) => credit.Player.Creature.IsAlive &&
        ReferenceEquals(credit.State, credit.Player.PlayerCombatState) &&
        !CombatManager.Instance.IsOverOrEnding && credit.Generation == credit.Turn.Generation;

    // Gameplay-only assignment receipt, independent of disabled relic statistics.
    internal static void ObserveEnergyChange(object target, string field, int before, int after)
    {
        if (field == "_energy" && CurrentDebit.Value is { AwaitingWrite: true } debit &&
            ReferenceEquals(target, debit.State))
        {
            debit.AwaitingWrite = false;
            RecordPayment(debit.Card, Math.Max(0, before - after));
        }
        if (field != "_energy" || CurrentWrite.Value is not { AwaitingWrite: true } credit ||
            !ReferenceEquals(target, credit.State)) return;
        credit.AwaitingWrite = false;
        credit.Credited = Math.Min(credit.Amount, Math.Max(0L, (long)after - before));
    }

    // Replaces only PlayerCmd.GainEnergy's final native state call. All native
    // modifiers, awaits, sounds and clamping remain in the original command.
    private static void GainWithReceipt(PlayerCombatState state, decimal amount)
    {
        var credit = CurrentCredit.Value;
        if (credit == null || !ReferenceEquals(credit.RootCommand, CurrentCommand.Value) ||
            !ReferenceEquals(state, credit.State))
        {
            state.GainEnergy(amount);
            return;
        }
        if (!CanCredit(credit)) return;
        var previous = CurrentWrite.Value;
        credit.AwaitingWrite = true;
        CurrentWrite.Value = credit;
        try { state.GainEnergy(amount); }
        finally { credit.AwaitingWrite = false; CurrentWrite.Value = previous; }
    }

    [HarmonyPatch(typeof(PlayerCmd), nameof(PlayerCmd.GainEnergy))]
    [HarmonyPatchCategory(PatchCategory)]
    private static class GainCommand
    {
        private static void Prefix(out object? __state)
        {
            __state = CurrentCommand.Value;
            var command = new object();
            CurrentCommand.Value = command;
            if (CurrentCredit.Value is { RootCommand: null } credit) credit.RootCommand = command;
        }
        private static Exception? Finalizer(Exception? __exception, object? __state)
        { CurrentCommand.Value = __state; return __exception; }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(PatchCategory)]
    private static class GainCallsite
    {
        private static MethodBase TargetMethod() => AccessTools.Method(
            AccessTools.Method(typeof(PlayerCmd), nameof(PlayerCmd.GainEnergy))
                .GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType, "MoveNext");
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var native = AccessTools.Method(typeof(PlayerCombatState), nameof(PlayerCombatState.GainEnergy));
            int replaced = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(native))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(ResetEnergyLedger), nameof(GainWithReceipt));
                    replaced++;
                }
                yield return instruction;
            }
            if (replaced != 1) throw new InvalidOperationException("Reset requires exactly one native energy-gain callsite.");
        }
    }

    [HarmonyPatch]
    [HarmonyPatchCategory(PatchCategory)]
    private static class PaymentCallsite
    {
        private static Type Machine => AccessTools.Method(typeof(CardModel), "SpendEnergy")
            .GetCustomAttribute<AsyncStateMachineAttribute>()!.StateMachineType;
        private static MethodBase TargetMethod() => AccessTools.Method(Machine, "MoveNext");
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.ToList();
            var native = AccessTools.Method(typeof(PlayerCombatState), nameof(PlayerCombatState.LoseEnergy));
            if (code.Count(i => i.Calls(native)) != 1)
                throw new InvalidOperationException("Reset requires exactly one native card energy-debit callsite.");
            var card = Machine.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Single(f => f.FieldType == typeof(CardModel));
            foreach (var instruction in code)
            {
                if (instruction.Calls(native))
                {
                    var load = new CodeInstruction(OpCodes.Ldarg_0);
                    load.labels.AddRange(instruction.labels); instruction.labels.Clear();
                    load.blocks.AddRange(instruction.blocks); instruction.blocks.Clear();
                    yield return load;
                    yield return new(OpCodes.Ldfld, card);
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(ResetEnergyLedger), nameof(PayWithReceipt));
                }
                yield return instruction;
            }
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
    private static class CombatStart
    { private static void Prefix(ICombatState? combatState) => Clear(combatState); }

    [HarmonyPatch(typeof(Hook), nameof(Hook.BeforeSideTurnStart))]
    private static class TurnStart
    { private static void Prefix(ICombatState combatState) => Clear(combatState); }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
    private static class CombatEnd
    { private static void Prefix(ICombatState? combatState) => Clear(combatState); }
}
