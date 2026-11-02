using System.Runtime.CompilerServices;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace LaRata;

// A native NPower view model only: never applied to Creature.Powers, serialized,
// dispelled, or consulted by damage rules. Its value comes from the existing rule.
public sealed class RoyalDamageRampPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldScaleInMultiplayer => false;
    public override bool ShouldPlayVfx => false;
    public override string? CustomPackedIconPath => ModelDb.Power<RitualPower>().IconPath;
    public override string? CustomBigIconPath => CustomPackedIconPath;
    public override List<(string, string)> Localization => [];
    private int NativeRound => IsMutable ? Owner?.CombatState?.RoundNumber ?? 1 : 1;
    public override int DisplayAmount => (int)(100 * RoyalDamageRamp.Factor(NativeRound));
    public override LocString Description
    {
        get
        {
            int current = DisplayAmount;
            var text = new LocString("powers", Id.Entry + (current >= 100 ? ".capped.description" : ".description"));
            text.Add("Current", current);
            text.Add("Next", (int)(100 * RoyalDamageRamp.Factor(NativeRound + 1)));
            return text;
        }
    }

    private int lastDisplay = -1; // View invalidation cache, not a gameplay counter.
    internal void Refresh()
    {
        if (DisplayAmount == lastDisplay) return;
        lastDisplay = DisplayAmount;
        InvokeDisplayAmountChanged();
    }
}

internal static class RoyalDamageRampPresentation
{
    private sealed record Binding(RoyalDamageRampPower Model, Action<CombatState> Changed);
    private static readonly ConditionalWeakTable<NPowerContainer, Binding> Bindings = new();
    // The native UI requires a mutable model with an owner. ApplyInternal would
    // mutate gameplay, so bind only the detached view model's verified owner field.
    private static readonly AccessTools.FieldRef<PowerModel, Creature> ViewOwner =
        AccessTools.FieldRefAccess<PowerModel, Creature>("_owner");
    private static readonly System.Reflection.MethodInfo AddView = AccessTools.Method(typeof(NPowerContainer), "Add");

    internal static void Attach(NPowerContainer container, Creature creature)
    {
        if (Bindings.TryGetValue(container, out _) || creature.Side != CombatSide.Enemy ||
            creature.CombatState?.Encounter is not RatKingEncounter ||
            creature.Monster is not (RatKing or KingsguardKnight)) return;
        var model = (RoyalDamageRampPower)ModelDb.Power<RoyalDamageRampPower>().ToMutable();
        ViewOwner(model) = creature;
        Action<CombatState> changed = state =>
        {
            if (ReferenceEquals(creature.CombatState, state)) model.Refresh();
        };
        AddView.Invoke(container, [model]);
        Bindings.Add(container, new Binding(model, changed));
        CombatManager.Instance.StateTracker.CombatStateChanged += changed;
        model.Refresh();
    }

    internal static void Detach(NPowerContainer container)
    {
        if (!Bindings.TryGetValue(container, out var binding)) return;
        CombatManager.Instance.StateTracker.CombatStateChanged -= binding.Changed;
        Bindings.Remove(container);
    }
}

[HarmonyPatch(typeof(NPowerContainer), nameof(NPowerContainer.SetCreature))]
internal static class AttachRoyalDamageRampPresentation
{
    [HarmonyPostfix]
    private static void Postfix(NPowerContainer __instance, Creature creature) => RoyalDamageRampPresentation.Attach(__instance, creature);
}

[HarmonyPatch(typeof(NPowerContainer), nameof(NPowerContainer._ExitTree))]
internal static class DetachRoyalDamageRampPresentation
{
    [HarmonyPrefix]
    private static void Prefix(NPowerContainer __instance) => RoyalDamageRampPresentation.Detach(__instance);
}

[HarmonyPatch(typeof(NPower), "RefreshAmount")]
internal static class RoyalDamageRampPercentage
{
    [HarmonyPostfix]
    private static void Postfix(PowerModel? ____model, MegaLabel ____amountLabel)
    {
        if (____model is RoyalDamageRampPower ramp) ____amountLabel.SetTextAutoSize($"{ramp.DisplayAmount}%");
    }
}
