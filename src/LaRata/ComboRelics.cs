using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

public abstract class ComboRelic : CustomRelicModel
{
    protected abstract int Kind { get; }
    private static readonly string[] Titles = ["Ashen Spool", "Crown Capacitor", "Crossroads Compass", "Bottled Echo", "Smuggler's Thread"];
    private static readonly string[] Art = ["ashen_spool", "crown_capacitor", "crossroads_compass", "bottled_echo", "smugglers_thread"];
    private static readonly string[] Text =
    [
        "Whenever you Exhaust a card, gain 3 Block. The first time each turn, also draw 1 card.",
        "The first time you play a Power each turn, gain 1 Energy and 4 Vigor.",
        "The first time you play two different card types in a turn, draw 1 card and gain 4 Block.",
        "Whenever you use a Potion in combat, gain 1 Strength and immediately draw 2 cards.",
        "The first time you discard a card during your turn, gain 1 Energy."
    ];
    public override RelicRarity Rarity => Kind is 1 or 3 ? RelicRarity.Rare : RelicRarity.Uncommon;
    public override string PackedIconPath => $"res://LaRata/images/relics/{Art[Kind]}.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    public override List<(string, string)> Localization => [("title", Titles[Kind]), ("description", Text[Kind]), ("flavor", "Everything has another use. Especially things the king threw away.")];
    [SavedProperty] public bool Triggered { get; set; }
    [SavedProperty] public int SeenTypes { get; set; }
    public override Task BeforeSideTurnStart(PlayerChoiceContext ctx, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combat)
    {
        Triggered = false;
        SeenTypes = 0;
        return Task.CompletedTask;
    }
    public override Task AfterCombatEnd(CombatRoom room)
    {
        Triggered = false;
        SeenTypes = 0;
        return Task.CompletedTask;
    }
    private bool Active => CombatManager.Instance.IsInProgress && !CombatManager.Instance.IsEnding && Owner.Creature.IsAlive;
    public override async Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)
    {
        if (Kind != 0 || card.Owner != Owner || !Active) return;
        bool draw = !Triggered;
        Triggered = true; // Reserve before awaits: recursive exhaust/draw hooks cannot retrigger.
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, 3, ValueProp.Unpowered, null);
        if (draw && Active) await CardPileCmd.Draw(ctx, 1, Owner);
    }
    public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay play)
    {
        if (play.Card.Owner != Owner || !Active || Triggered) return;
        if (Kind == 1 && play.Card.Type == CardType.Power)
        {
            Triggered = true;
            Flash();
            await PlayerCmd.GainEnergy(1, Owner);
            await PowerCmd.Apply<VigorPower>(ctx, Owner.Creature, 4, Owner.Creature, null);
        }
        if (Kind == 2)
        {
            int type = play.Card.Type switch { CardType.Attack => 1, CardType.Skill => 2, CardType.Power => 4, _ => 0 };
            SeenTypes |= type;
            if ((SeenTypes & (SeenTypes - 1)) == 0) return;
            Triggered = true;
            Flash();
            await CreatureCmd.GainBlock(Owner.Creature, 4, ValueProp.Unpowered, null);
            if (Active) await CardPileCmd.Draw(ctx, 1, Owner);
        }
    }
    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (Kind != 3 || potion.Owner != Owner || !Active) return;
        Flash();
        // Draw hooks may request a choice (e.g. an autoplayed card). Reuse the
        // native drinking action's context so its queue can pause and synchronize.
        PlayerChoiceContext ctx = RunManager.Instance.ActionExecutor.CurrentlyRunningAction is UsePotionAction action && action.OwnerId == Owner.NetId
            ? action.PlayerChoiceContext ?? new BlockingPlayerChoiceContext()
            : new BlockingPlayerChoiceContext();
        ctx.PushModel(this);
        try
        {
            await PowerCmd.Apply<StrengthPower>(ctx, Owner.Creature, 1, Owner.Creature, null);
            if (Active) await CardPileCmd.Draw(ctx, 2, Owner);
        }
        finally { ctx.PopModel(this); }
    }
    public override async Task AfterCardDiscarded(PlayerChoiceContext ctx, CardModel card)
    {
        if (Kind != 4 || card.Owner != Owner || !Active || Triggered || Owner.Creature.CombatState?.CurrentSide != Owner.Creature.Side) return;
        Triggered = true;
        Flash();
        await PlayerCmd.GainEnergy(1, Owner);
    }
}

[Pool(typeof(SharedRelicPool))] public sealed class AshenSpool : ComboRelic { protected override int Kind => 0; }
[Pool(typeof(SharedRelicPool))] public sealed class CrownCapacitor : ComboRelic { protected override int Kind => 1; }
[Pool(typeof(SharedRelicPool))] public sealed class CrossroadsCompass : ComboRelic { protected override int Kind => 2; }
[Pool(typeof(SharedRelicPool))] public sealed class BottledEcho : ComboRelic { protected override int Kind => 3; }
[Pool(typeof(SharedRelicPool))] public sealed class SmugglersThread : ComboRelic { protected override int Kind => 4; }
