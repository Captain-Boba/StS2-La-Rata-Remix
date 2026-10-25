using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;

namespace LaRata;

// Native status-bar records of existing director effects. These powers NEVER
// change HP, damage or Strength: cloning/loading a marker cannot reapply a twist.
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
    public override string? CustomPackedIconPath => ModelDb.Power<RegenPower>().IconPath;
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
