using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;

namespace LaRata;

[Pool(typeof(SharedRelicPool))]
public sealed class Well : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool IsAllowed(IRunState runState) => runState.Players.Count > 1 && base.IsAllowed(runState);
    public override string PackedIconPath => "res://LaRata/images/relic_native/well.png";
    protected override string PackedIconOutlinePath => "res://LaRata/images/relic_native/well_outline.png";
    protected override string BigIconPath => "res://LaRata/images/well.png";
    public override List<(string, string)> Localization => [];
}
