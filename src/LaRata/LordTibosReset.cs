using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace LaRata;

[Pool(typeof(ColorlessCardPool))]
public sealed class LordTibosReset() : ComboCard(2, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
{
    internal override string ArtKey => "lord_tibos_reset";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override Task OnPlay(PlayerChoiceContext ctx, CardPlay play) =>
        CombatState is { } combat && CanContinue ? ResetEnergyLedger.Refund(combat) : Task.CompletedTask;
    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Exhaust);
}
