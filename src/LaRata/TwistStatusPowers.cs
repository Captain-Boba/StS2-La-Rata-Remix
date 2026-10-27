using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace LaRata;

// Native status-bar records of existing director effects. Applying one never
// repeats an HP/stat grant. These records never gate the underlying rules.
// Amount participates in native combat snapshots/checksums. Ordinary run saves
// restart the encounter and rebuild these markers through the director; this
// is not a separate mid-combat persistence mechanism.
public abstract class TwistStatusPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool ShouldScaleInMultiplayer => false;
    public override bool ShouldPlayVfx => false;
    public override string? CustomBigIconPath => CustomPackedIconPath;
    public override List<(string, string)> Localization => [];
}

public sealed class SturdyTwistPower : TwistStatusPower
{
    // Amount is the selected percentage, not a second MaxHP grant.
    public override string? CustomPackedIconPath => "res://LaRata/images/powers/sturdy_twist.svg";
}

public sealed class KeenTwistPower : TwistStatusPower
{
    // Percentage scaling remains in DamagePatch, shared by attacks and intents.
    public override string? CustomPackedIconPath => ModelDb.Power<StrengthPower>().IconPath;
}

public sealed class EmpoweredTwistPower : TwistStatusPower
{
    // The opening Vigor still uses its native, consumable status separately.
    public override string? CustomPackedIconPath => ModelDb.Power<VigorPower>().IconPath;
    public override int DisplayAmount => 0;
}

public sealed class ReinforcementTwistPower : TwistStatusPower
{
    // Amount stores 35 (scout) or 60 (regular reinforcement) percent MaxHP.
    // This only reuses Minion's icon, not its secondary-enemy/fatal behavior.
    public override string? CustomPackedIconPath => ModelDb.Power<MinionPower>().IconPath;
    public override int DisplayAmount => 0;
    public override LocString Description
    {
        get
        {
            var text = base.Description;
            text.Add("AttackPercent", IsMutable && Owner?.Monster is Inklet ? 100 : Amount == 35 ? 50 : 65);
            return text;
        }
    }
}

// One hex family per monster. The native Amount stores its stable enum identity;
// dose/Lesser/variant come from the same deterministic rule as execution, not a
// second effect table. Removing this record never removes the encounter rule.
public sealed class HexTwistPower : TwistStatusPower
{
    public override int DisplayAmount => 0;
    public override string? CustomPackedIconPath => ModelDb.Power<RitualPower>().IconPath;
    private EnemyTwist? Twist => IsMutable && Owner?.CombatState is { } combat
        ? EncounterDirector.Get(combat.Encounter)?.For(Owner.Monster).FirstOrDefault(t => t.IsHex && Amount == (int)t.Kind + 1)
        : null;
    public override LocString Title => Twist is { } twist
        ? new LocString("powers", Key(twist) + ".title") : base.Title;
    public override LocString Description
    {
        get
        {
            if (Twist is not { } twist || Owner.Monster is not { } monster) return base.Description;
            if (!HexTelegraphPatch.HasHexMove(monster))
                return new LocString("powers", "LARATA-HEX_TWIST_POWER." +
                    (twist.Kind == TwistKind.Echoing ? "echoing_unbound" : "unbound") + ".description");
            return Describe(twist);
        }
    }
    private static string Key(EnemyTwist twist) => "LARATA-HEX_TWIST_POWER." + twist.Kind.ToString().ToLowerInvariant();
    internal static LocString Describe(EnemyTwist twist)
    {
        var text = new LocString("powers", Key(twist) + ".description");
        text.Add("Dose", twist.Magnitude(twist.Kind == TwistKind.Ominous ? 4 : 2));
        text.Add("Debuff", (twist.Amount switch
        {
            2 => ModelDb.Power<FrailPower>().Title,
            3 => ModelDb.Power<VulnerablePower>().Title,
            _ => ModelDb.Power<WeakPower>().Title
        }).GetFormattedText());
        return text;
    }
}

// Keep native intent sprites and labels; the tooltip identifies the exact hex
// on the exact decorated move, instead of an unexplained generic Debuff icon.
internal sealed class HexDebuffIntent(EnemyTwist twist) : DebuffIntent
{
    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner) => HexTwistPower.Describe(twist);
}

internal sealed class HexStatusIntent(EnemyTwist twist) : StatusIntent(1)
{
    protected override LocString GetIntentDescription(IEnumerable<Creature> targets, Creature owner) => HexTwistPower.Describe(twist);
}
