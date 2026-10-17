using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

// RelicModel requires a Player owner. Enemy adaptations use native commands and
// a native power amount (bit mask), included in combat checksums.
public sealed class RoyalRegaliaPower : CustomPowerModel
{
    internal static RelicModel[] Relics => [ModelDb.Relic<Vajra>(), ModelDb.Relic<OddlySmoothStone>(), ModelDb.Relic<Anchor>(), ModelDb.Relic<BronzeScales>(), ModelDb.Relic<HornCleat>(), ModelDb.Relic<CaptainsWheel>(), ModelDb.Relic<Orichalcum>(), ModelDb.Relic<MercuryHourglass>(), ModelDb.Relic<Lantern>()];
    internal static readonly string[] Effects =
    [
        "Vajra: Start with 1 Strength.", "Oddly Smooth Stone: Start with 1 Dexterity.",
        "Anchor: Start with 10 base Block.", "Bronze Scales: Start with 3 Thorns.",
        "Horn Cleat: Gain 14 base Block on enemy turn 2.", "Captain's Wheel: Gain 18 base Block on enemy turn 3.",
        "Orichalcum: Gain 6 base Block at the end of an enemy turn if no Block remains.",
        "Mercury Hourglass: Deal 3 damage to every player at the start of each enemy turn.",
        "Lantern: The first royal hand has 1 additional Energy."
    ];
    internal static int Choose(Rng rng) => ChaosSelections.Distinct(5, Effects.Length, n => rng.NextInt(n)).Aggregate(0, (mask, i) => mask | (1 << i));
    internal static string Describe(int mask) => string.Join("\n", Enumerable.Range(0, Effects.Length).Where(i => (mask & (1 << i)) != 0).Select(i => Effects[i]));
    private bool Has(int i) => (Amount & (1 << i)) != 0;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => 5;
    public override string? CustomPackedIconPath => ModelDb.Relic<CaptainsWheel>().IconPath;
    public override string? CustomBigIconPath => CustomPackedIconPath;
    public override List<(string, string)> Localization => [("title", "Royal Regalia"), ("description", "Five stolen relics empower La Rata.\n{Relics}")];
    public override LocString Description { get { var text = base.Description; text.Add("Relics", Describe(Amount)); return text; } }
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!Owner.IsAlive) return;
        if (side != CombatSide.Enemy || !participants.Contains(Owner)) return;
        if (combatState.RoundNumber == 2 && Has(4)) await CreatureCmd.GainBlock(Owner, 14, ValueProp.Move, null);
        if (combatState.RoundNumber == 3 && Has(5)) await CreatureCmd.GainBlock(Owner, 18, ValueProp.Move, null);
        if (Has(7))
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), combatState.Players.Select(p => p.Creature).Where(c => c.IsAlive), 3, ValueProp.Unpowered, Owner, null);
    }
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == CombatSide.Enemy && Owner.IsAlive && participants.Contains(Owner) && Has(6) && Owner.Block == 0)
            await CreatureCmd.GainBlock(Owner, 6, ValueProp.Move, null);
    }
}
