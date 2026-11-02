using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace LaRata;

public sealed class BloodsmithPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string? CustomPackedIconPath => ModelDb.Power<VigorPower>().PackedIconPath;
    public override string? CustomBigIconPath => CustomPackedIconPath;
    public override List<(string, string)> Localization => [];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromForge();
    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner || delta >= 0 || Owner.Player == null || !Owner.IsAlive ||
            !CombatManager.Instance.IsInProgress || CombatManager.Instance.IsOverOrEnding) return;
        Flash();
        await ForgeCmd.Forge(Amount, Owner.Player, this);
    }
}
