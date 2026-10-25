using System.Globalization;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

internal static class RoyalBossText
{
    internal static string Text(string key, string fallback, params (string Name, object Value)[] args)
    {
        var loc = new LocString("monsters", ModelDb.Monster<RatKing>().Id.Entry + ".royal_status." + key);
        if (loc.Exists())
        {
            foreach (var (name, value) in args) loc.AddObj(name, value);
            return loc.GetFormattedText();
        }
        foreach (var (name, value) in args) fallback = fallback.Replace("{" + name + "}", Convert.ToString(value, CultureInfo.InvariantCulture));
        return fallback;
    }
}

// Use native damage hooks in their native additive/multiplicative/cap order.
// Project only the explicitly announced royal changes; never mutate live powers to preview.
internal sealed class RoyalForecastAttackIntent : AttackIntent
{
    private readonly Func<RatKing?> sovereign;
    private readonly int card, basis, hits;
    private readonly bool guard;
    internal RoyalForecastAttackIntent(RatKing king, int index, int damage, int repeats)
        : this(() => king, damage, repeats, false, index) { }
    internal RoyalForecastAttackIntent(Func<RatKing?> king, int damage, int repeats, bool isGuard = true, int index = -1)
    {
        sovereign = king; basis = damage; hits = repeats; guard = isGuard; card = index;
        DamageCalc = () => damage;
    }
    public override int Repeats => hits;
    protected override LocString IntentLabelFormat => new("intents", hits == 1 ? "FORMAT_DAMAGE_SINGLE" : "FORMAT_DAMAGE_MULTI");
    public override int GetTotalDamage(IEnumerable<Creature> targets, Creature owner) => Single(targets, owner) * hits;
    public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
    {
        var text = IntentLabelFormat;
        text.Add("Damage", Single(targets, owner)); text.Add("Repeat", hits);
        return text;
    }
    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner)
    {
        var text = new LocString("intents", "ATTACK.description");
        text.Add("IsMultiplayer", owner.CombatState?.Players.Count > 1);
        text.Add("Damage", Single(targets, owner)); text.Add("Repeat", hits);
        return text;
    }
    private int Single(IEnumerable<Creature> targets, Creature owner)
    {
        var combat = owner.CombatState;
        if (combat == null) return basis;
        var target = LocalContext.GetMe(combat)?.Creature ?? targets.FirstOrDefault(c => c.IsAlive);
        return Forecast(sovereign(), owner, target, basis, card, guard);
    }
    internal static int Forecast(RatKing? king, Creature owner, Creature? target, int basis, int card, bool guard)
    {
        var combat = owner.CombatState;
        if (combat == null) return basis;
        int[] remaining = king?.Creature.IsAlive == true ? king.RemainingPlan.ToArray() : [];
        bool cleanse = remaining.Contains(25);
        bool clearWeak = cleanse || remaining.Contains(19);
        decimal raw = basis;
        if (king?.Creature.IsAlive == true)
            raw = guard ? basis + king.PendingStrength : king.PreviewAttackDamage(card, basis);
        // Cleanse removes negative Strength without changing positive Strength.
        if (cleanse) raw -= Math.Min(0, owner.GetPowerAmount<StrengthPower>());
        decimal amount = Hook.ModifyDamage(combat.RunState, combat, target, owner, raw, ValueProp.Move, null,
            ModifyDamageHookType.Additive, CardPreviewMode.None, out _);
        amount = Hook.ModifyDamage(combat.RunState, combat, target, owner, amount, ValueProp.Move, null,
            ModifyDamageHookType.Multiplicative, CardPreviewMode.None, out _);
        if (clearWeak && owner.GetPower<WeakPower>() is { } weak)
        {
            decimal factor = weak.ModifyDamageMultiplicative(target, amount, ValueProp.Move, owner, null);
            if (factor > 0) amount /= factor;
        }
        bool pendingVulnerable = target != null && (king != null
            ? ProjectsVulnerable(king, target, remaining, guard)
            : target.GetPowerAmount<VulnerablePower>() > 0);
        if (!pendingVulnerable && target?.GetPower<VulnerablePower>() is { } consumedVulnerable)
        {
            decimal factor = consumedVulnerable.ModifyDamageMultiplicative(target, amount, ValueProp.Move, owner, null);
            if (factor > 0) amount /= factor;
        }
        if (pendingVulnerable && target != null && target.GetPowerAmount<VulnerablePower>() <= 0)
        {
            decimal factor = 1.5m;
            if (owner.GetPower<CrueltyPower>() is { } cruelty) factor = cruelty.ModifyVulnerableMultiplier(target, factor, ValueProp.Move, owner, null);
            if (target.GetPower<DebilitatePower>() is { } debilitate) factor = debilitate.ModifyVulnerableMultiplier(target, factor, ValueProp.Move, owner, null);
            amount *= factor;
        }
        amount = Hook.ModifyDamage(combat.RunState, combat, target, owner, amount, ValueProp.Move, null,
            ModifyDamageHookType.Cap, CardPreviewMode.None, out _);
        return Math.Max(0, (int)amount);
    }

    // Artifact is consumed by visible debuff applications, not by their stack
    // amounts. Follow the published play order without changing live powers.
    private static bool ProjectsVulnerable(RatKing king, Creature target, int[] remaining, bool guard)
    {
        int artifact = target.GetPowerAmount<ArtifactPower>();
        bool vulnerable = target.GetPowerAmount<VulnerablePower>() > 0;
        bool poisoned = target.GetPowerAmount<PoisonPower>() > 0;
        var cindersPower = king.Creature.GetPower<RoyalCourtOfCindersPower>();
        bool cinders = cindersPower != null;
        bool cindersUsed = king.IsResolvingHand && cindersPower?.Amount == 2;
        foreach (int index in remaining)
        {
            if (!guard && RoyalDeck.IsAttack(index)) break;
            switch (index)
            {
                case 4: Apply(); Apply(); break; // Doom, Weak
                case 11: Apply(); break; // Weak after its attack
                case 14: Apply(poison: true); Apply(); break;
                case 20: Apply(); Apply(); break; // Weak, Frail
                case 28 when king.TargetFor(index) == target: Apply(poison: true); Apply(); break;
                case 35: Apply(); Apply(vulnerability: true); break;
                case 36 when target.GetPowerAmount<ThornsPower>() > 0: Apply(vulnerability: true); break;
                case 40 when poisoned: Apply(); break;
                case 41 when king.TargetFor(index) == target: vulnerable = false; break;
                case 44 when king.TargetFor(index) == target: Apply(); break;
                case 46: cinders = true; break;
            }
            bool exhausts = RoyalDeck.IsExhaust(index) || (index is 39 or 42 && king.Deck!.HasMaterial(index));
            if (cinders && !cindersUsed && exhausts)
            {
                cindersUsed = true;
                Apply(poison: true);
            }
        }
        if (guard && remaining.Contains(2)) Apply(vulnerability: true);
        return vulnerable;

        void Apply(bool poison = false, bool vulnerability = false)
        {
            if (artifact > 0) { artifact--; return; }
            poisoned |= poison;
            vulnerable |= vulnerability;
        }
    }
}
