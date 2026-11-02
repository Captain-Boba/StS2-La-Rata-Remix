using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace LaRata;

// Uses DrawCardsNextTurnPower's native turn-start snapshot, with owner draw cleanup.
// A separate debuff keeps permanent MindRot and positive next-turn draw intact.
public sealed class WitheringPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool ShouldScaleInMultiplayer => false;
    public override string? CustomPackedIconPath => ModelDb.Power<MindRotPower>().PackedIconPath;
    public override string? CustomBigIconPath => CustomPackedIconPath;
    public override List<(string, string)> Localization => [];

    public override decimal ModifyHandDraw(Player player, decimal count) =>
        player == Owner.Player && AmountOnTurnStart > 0 ? Math.Max(0, count - 1) : count;

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        // This native hook follows the owner's actual Draw command, even when a
        // choice pauses SetupPlayerTurn before drawing while the side-start proceeds.
        if (player == Owner.Player && AmountOnTurnStart > 0)
            await PowerCmd.Remove(this);
    }
}
