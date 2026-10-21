using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
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
        if (__instance.Encounter is RatKingEncounter && __result.Monster is MegaCrit.Sts2.Core.Models.Monsters.TwoTailedRat)
        {
            __result.SetMaxHpInternal(__result.MaxHp * 2);
            __result.SetCurrentHpInternal(__result.MaxHp);
            return;
        }
        var rules = EncounterDirector.Get(__instance.Encounter);
        if (rules == null || __result.Side != CombatSide.Enemy || __result.MaxHp <= 1) return;
        int hp = rules.For(__result.Monster).FirstOrDefault(t => t.Kind == TwistKind.Sturdy)?.Amount ?? 0;
        decimal factor = (rules.IsExtra(__result.Monster) ? rules.ExtraHpFactor : 1m) * (1m + hp / 100m);
        __result.SetMaxHpInternal(Math.Max(1, decimal.Ceiling(__result.MaxHp * factor)));
        __result.SetCurrentHpInternal(__result.MaxHp);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyDamage))]
internal static class DamagePatch
{
    // The same native hook serves intent previews and actual attacks, before Weak and caps.
    [HarmonyPrefix]
    private static void Prefix(ICombatState? combatState, Creature? dealer, ValueProp props,
        ModifyDamageHookType modifyDamageHookType, ref decimal damage)
    {
        if (dealer?.Side != CombatSide.Enemy || !props.HasFlag(ValueProp.Move) ||
            !modifyDamageHookType.HasFlag(ModifyDamageHookType.Multiplicative)) return;
        var rules = EncounterDirector.Get(combatState?.Encounter);
        if (rules == null) return;
        int bonus = rules.For(dealer.Monster).FirstOrDefault(t => t.Kind == TwistKind.Keen)?.Amount ?? 0;
        damage *= (rules.IsExtra(dealer.Monster) && !rules.BossDuo && dealer.Monster is not MegaCrit.Sts2.Core.Models.Monsters.Inklet ? rules.ExtraDamageFactor : 1m) * (1m + bonus / 100m);
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
        if (rules == null || giver.Side != CombatSide.Enemy || amount <= 0 || amount > 3) return;
        var twists = rules.For(giver.Monster).ToArray();
        if (target?.Side == CombatSide.Enemy && power is StrengthPower && twists.Any(t => t.Kind == TwistKind.Empowered))
            amount += 1;
        if (target?.Side == CombatSide.Player && power is WeakPower or FrailPower or VulnerablePower && twists.Any(t => t.Kind == TwistKind.Echoing))
            amount = Math.Min(3, amount + 1);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeCombatStart))]
internal static class StartingBuffsPatch
{
    internal static int RegenCap(Creature target, EnemyTwist twist) =>
        target.Monster is MegaCrit.Sts2.Core.Models.Monsters.Inklet ? 1 : Math.Min(twist.Defense(3), Math.Max(1, target.MaxHp / 10));
    [HarmonyPostfix]
    private static void Postfix(ICombatState? combatState, ref Task __result) => __result = Apply(__result, combatState);

    private static async Task Apply(Task original, ICombatState? combat)
    {
        await original;
        var rules = EncounterDirector.Get(combat?.Encounter);
        if (rules == null || rules.Started) return;
        rules.Started = true;
        var context = new ThrowingPlayerChoiceContext();
        foreach (var twist in rules.Plan.Twists)
        {
            var target = rules.Roster[twist.Target].Creature;
            if (!target.IsAlive) continue;
            // No applier: encounter grants must not amplify one another. Native co-op power scaling remains intact.
            Task Grant<T>(int amount) where T : PowerModel => PowerCmd.Apply<T>(context, target, amount, null, null);
            Task Block(int amount) => CreatureCmd.GainBlock(target, amount, ValueProp.Move, null);
            switch (twist.Kind)
            {
                case TwistKind.Warded: await Grant<ArtifactPower>(1); break;
                case TwistKind.Empowered: await Grant<VigorPower>(2); break;
                case TwistKind.Bristling: await Grant<ThornsPower>(twist.Magnitude(2)); break;
                case TwistKind.Regenerating:
                    // The budget already scales with party size. Do not multiply this
                    // sustain a second time, especially on tiny reinforcement bodies.
                    int grant = Math.Max(0, RegenCap(target, twist) - target.GetPowerAmount<RegenPower>());
                    if (grant > 0)
                    {
                        var regen = ModelDb.Power<RegenPower>().ToMutable();
                        TwistRegenScaling.Unscaled.Add(regen, new object());
                        try { await PowerCmd.Apply(context, regen, target, grant, null, null); }
                        finally { TwistRegenScaling.Unscaled.Remove(regen); }
                    }
                    break;
                case TwistKind.Cultist: await Grant<RitualPower>(1); break;
                case TwistKind.Demonic: await Grant<DemonFormPower>(1); break;
                case TwistKind.Entrenched:
                    await PowerCmd.Apply<BarricadePower>(context, target, 1, target, null);
                    await Block(twist.Defense(8));
                    break;
                case TwistKind.Buffered: await Grant<BufferPower>(1); break;
                case TwistKind.Furious: await Grant<StrengthPower>(1); break;
                case TwistKind.Phased: await Grant<IntangiblePower>(1); break;
                case TwistKind.WoundUp: await Grant<VigorPower>(twist.Magnitude(2)); break;
                case TwistKind.Agile: await Grant<DexterityPower>(2); await Block(twist.Defense(6)); break;
                case TwistKind.Fortified: await Block(twist.Defense(12)); break;
                case TwistKind.SecondWind: await Grant<SelfFormingClayPower>(twist.Defense(8)); break;
                case TwistKind.Reservist:
                    (await PowerCmd.Apply<ToricToughnessPower>(context, target, 2, null, null))?.SetBlock(twist.Defense(6));
                    break;
                case TwistKind.VenomCoated: await Grant<EnvenomPower>(1); break;
                case TwistKind.EmberGuard: await Grant<FlameBarrierPower>(twist.Magnitude(3)); break;
                case TwistKind.GlassCrown: await Grant<DiamondDiademPower>(1); break;
                case TwistKind.MirrorGuard:
                    await Block(twist.Defense(6));
                    await Grant<ReflectPower>(1);
                    break;
                case TwistKind.BloodFeud:
                    var feud = await PowerCmd.Apply<CrabRagePower>(context, target, 1, null, null);
                    if (feud != null)
                    {
                        feud.DynamicVars.Strength.BaseValue = 2;
                        feud.DynamicVars.Block.BaseValue = twist.Defense(12);
                    }
                    break;
                case TwistKind.Doomweaver: await Grant<ShroudPower>(twist.Defense(4)); break;
                case TwistKind.Hotheaded: await Grant<FlexPotionPower>(twist.Magnitude(3)); break;
                case TwistKind.Afterglow:
                    await Block(twist.Defense(10));
                    await Grant<BlurPower>(1);
                    break;
                case TwistKind.Reckless:
                    await Grant<StrengthPower>(2); await Grant<VulnerablePower>(1); break;
                case TwistKind.BrittleWard:
                    await Block(twist.Defense(18)); await Grant<VulnerablePower>(2); break;
                case TwistKind.ToxicShell:
                    await Grant<ThornsPower>(3); await Grant<PoisonPower>(3); break;
                case TwistKind.LastStand:
                    await Grant<DemonFormPower>(1); await Grant<WeakPower>(1); break;
                case TwistKind.FadingMight:
                    await Grant<FlexPotionPower>(5); await Grant<WeakPower>(1); break;
            }
        }
    }
}

// Apply the native Regen power with an exact, already-budgeted amount. Scoping
// to this new instance preserves native/other-mod Regen and avoids inverse
// scaling fractions being truncated to zero by native PowerModel.ApplyInternal.
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.GetScaledAmountForMultiplayer))]
internal static class TwistRegenScaling
{
    internal static readonly ConditionalWeakTable<PowerModel, object> Unscaled = new();
    [HarmonyPrefix] private static bool Prefix(PowerModel __instance, decimal amount, ref decimal __result)
    {
        if (!Unscaled.TryGetValue(__instance, out _)) return true;
        __result = amount;
        return false;
    }
}

internal sealed record ExtraHex(MonsterModel Monster, EnemyTwist Twist);

[HarmonyPatch(typeof(MonsterModel), nameof(MonsterModel.SetUpForCombat))]
internal static class HexTelegraphPatch
{
    internal static readonly ConditionalWeakTable<MoveState, ExtraHex> Hexes = new();

    [HarmonyPostfix]
    private static void Postfix(MonsterModel __instance, Creature? ____creature)
    {
        // Bestiary builds the move machine before attaching a Creature. Never
        // evaluate MonsterModel.CombatState/Creature on that preview path.
        if (____creature?.CombatState is not { Encounter: not null } combat) return;
        var rules = EncounterDirector.Get(combat.Encounter);
        var twist = rules?.For(__instance).FirstOrDefault(t => t.IsHex);
        if (twist == null) return;
        var moves = __instance.MoveStateMachine!.States.Values.OfType<MoveState>()
            .Where(x => x.Intents.Any(i => i is AttackIntent or BuffIntent or DefendIntent or DebuffIntent))
            .OrderBy(x => x.Intents.Any(i => i is AttackIntent))
            .ThenBy(x => x.Id, StringComparer.Ordinal).ToArray();
        if (moves.Length == 0) return;
        var move = moves[0];
        var intents = move.Intents.ToList();
        if (twist.Kind is TwistKind.Cinders or TwistKind.Disorienting or TwistKind.Lacerating or TwistKind.Leaden or TwistKind.StaticHex) intents.Add(new StatusIntent(1));
        else if (!intents.Any(x => x is DebuffIntent)) intents.Add(new DebuffIntent());
        AccessTools.PropertySetter(typeof(MoveState), nameof(MoveState.Intents)).Invoke(move, [intents]);
        Hexes.Add(move, new(__instance, twist));
        if (!MegaCrit.Sts2.Core.TestSupport.TestMode.IsOn) ModEntry.Log.Info($"TELEGRAPH monster={__instance.Id.Entry} move={move.Id} twist={twist.Description}");
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
        var source = hex.Monster.Creature;
        async Task Capped<T>(int amount, int cap) where T : PowerModel
        {
            foreach (var target in living)
            {
                int grant = Math.Min(amount, Math.Max(0, cap - target.GetPowerAmount<T>()));
                // Echoing's native-power hook also amplifies these three powers by one.
                if (grant > 0) await PowerCmd.Apply<T>(context, target, grant, source, null);
            }
        }
        switch (hex.Twist.Kind)
        {
            case TwistKind.Echoing:
            case TwistKind.Hexed:
                int amount = hex.Twist.Kind == TwistKind.Echoing ? 1 : hex.Twist.Magnitude(2);
                // Reserve the hook's +1 so repeated Echoing cannot exceed three stacks.
                int cap = hex.Twist.Kind == TwistKind.Echoing ? 2 : 3;
                switch (hex.Twist.Amount)
                {
                    case 1: await Capped<WeakPower>(amount, cap); break;
                    case 2: await Capped<FrailPower>(amount, cap); break;
                    case 3: await Capped<VulnerablePower>(amount, cap); break;
                }
                break;
            case TwistKind.Venomous: await Capped<PoisonPower>(hex.Twist.Magnitude(2), 6); break;
            case TwistKind.Doomweaver:
            case TwistKind.Ominous:
                foreach (var target in living)
                {
                    int grant = Math.Min(hex.Twist.Kind == TwistKind.Doomweaver ? 2 : hex.Twist.Magnitude(4), Math.Max(0, target.MaxHp / 4 - target.GetPowerAmount<DoomPower>()));
                    if (grant > 0) await PowerCmd.Apply<DoomPower>(context, target, grant, source, null);
                }
                break;
            case TwistKind.Cinders: await CardPileCmd.AddToCombatAndPreview<Burn>(living, PileType.Draw, 1, null, CardPilePosition.Random); break;
            case TwistKind.Disorienting: await CardPileCmd.AddToCombatAndPreview<Dazed>(living, PileType.Draw, 1, null, CardPilePosition.Random); break;
            case TwistKind.Lacerating: await CardPileCmd.AddToCombatAndPreview<Wound>(living, PileType.Discard, 1, null); break;
            case TwistKind.Leaden: await CardPileCmd.AddToCombatAndPreview<Slimed>(living, PileType.Discard, 1, null); break;
            case TwistKind.StaticHex: await CardPileCmd.AddToCombatAndPreview<MegaCrit.Sts2.Core.Models.Cards.Void>(living, PileType.Draw, 1, null, CardPilePosition.Random); break;
            case TwistKind.Crossfire:
                await Capped<WeakPower>(1, 2); await Capped<VulnerablePower>(1, 2); break;
            case TwistKind.Shackling: await Capped<ShacklingPotionPower>(1, 1); break;
            case TwistKind.Sapping:
                foreach (var target in living)
                    if (target.GetPowerAmount<DexterityPower>() > -1)
                        await PowerCmd.Apply<DexterityPower>(context, target, -1, source, null);
                break;
            case TwistKind.Constricting: await Capped<ConstrictPower>(hex.Twist.Magnitude(2), 4); break;
            case TwistKind.Eroding: await Capped<DisintegrationPower>(1, 2); break;
        }
        ModEntry.Log.Info($"HEX resolved monster={hex.Monster.Id.Entry} twist={hex.Twist.Kind} targets={living.Length}");
    }
}
