using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace LaRata;

public abstract class ComboTurnPower : CustomPowerModel
{
    // Transient combat state: native run checkpoints do not serialize PowerModels.
    public bool Triggered { get; set; }
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected bool CombatActive => Owner.Player != null && Owner.IsAlive &&
        CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsOverOrEnding &&
        Owner.CombatState != null;
    protected bool Active => CombatActive && Owner.CombatState is { } combat && combat.CurrentSide == Owner.Side;

    public override Task BeforeSideTurnStart(PlayerChoiceContext ctx, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combat)
    {
        if (side == Owner.Side) Triggered = false;
        return Task.CompletedTask;
    }
    internal static void Register()
    {
        CustomContentDictionary.RegisterType(typeof(SmugglersDividendPower));
        CustomContentDictionary.RegisterType(typeof(CourtOfCindersPower));
    }
}

public sealed class SmugglersDividendPower : ComboTurnPower
{
    public override string? CustomPackedIconPath => ModelDb.Power<VigorPower>().PackedIconPath;
    public override string? CustomBigIconPath => CustomPackedIconPath;
    public override List<(string, string)> Localization => [];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VigorPower>()];
    public override async Task AfterCardDiscarded(PlayerChoiceContext ctx, CardModel card)
    {
        if (!Active || Triggered || card.Owner != Owner.Player) return;
        Triggered = true; // Reserve before native hooks can recursively discard another card.
        int amount = Amount;
        Flash();
        await PowerCmd.Apply<VigorPower>(ctx, Owner, amount, Owner, null);
    }
}

public sealed class CourtOfCindersPower : ComboTurnPower
{
    public override string? CustomPackedIconPath => ModelDb.Power<PoisonPower>().PackedIconPath;
    public override string? CustomBigIconPath => CustomPackedIconPath;
    public override List<(string, string)> Localization => [];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PoisonPower>()];
    public override Task BeforeSideTurnStart(PlayerChoiceContext ctx, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combat)
    {
        Triggered = false;
        return Task.CompletedTask;
    }
    public override async Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)
    {
        var player = Owner.Player;
        if (player == null || !CombatActive || Triggered || card.Owner != player) return;
        Triggered = true;
        int poison = Amount; // Copies stack the Poison grant, never the trigger count.
        Flash();
        if (Owner.CombatState is not { } combat) return;
        var enemies = combat.HittableEnemies.OrderBy(c => c.CombatId).ToArray();
        await PowerCmd.Apply<PoisonPower>(ctx, enemies, poison, Owner, null);
    }
}
