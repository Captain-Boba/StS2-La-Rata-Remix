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
    public override string PackedIconPath => "res://LaRata/images/well.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    public override List<(string, string)> Localization =>
    [
        ("title", "Well"),
        ("description", "Win every Rock, Paper, Scissors contest for a treasure relic. Well against Well: 50/50. If more than two contenders have Well, each has an equal chance."),
        ("flavor", "Rock sinks. Scissors sink. Paper? Royal decree says it sinks too.")
    ];
}
