using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace LaRata;

[Pool(typeof(ColorlessCardPool))]
public sealed class Cleanse() : CustomCardModel(2, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
{
    public override CardPoolModel Pool => ModelDb.CardPool<ColorlessCardPool>();
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public override Material? CreateCustomFrameMaterial => RoyalCardStyle.Frame();
    public override Material? CreateCustomBannerMaterial => RoyalCardStyle.Banner();
    public override string? CustomPortraitPath => "res://LaRata/images/cleanse.png";
    public override List<(string, string)> Localization =>
    [
        ("title", "Cleanse"),
        ("description", "Remove all Debuffs from yourself and all other players.")
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combat = CombatState ?? throw new InvalidOperationException("Cleanse requires an active combat.");
        foreach (var player in combat.Players.OrderBy(p => p.NetId).ToArray())
        {
            var target = player.Creature;
            if (!target.IsAlive) continue;
            NCombatRoom.Instance?.PlaySplashVfx(target, new Color("329D79"));

            // Temporary stat reductions have a second, negative stat power. Their
            // amount-change hook reverses it. Zero the wrapper through that hook
            // before checking remaining debuffs, preserving existing positive stats.
            foreach (var power in target.Powers.Where(p => p.TypeForCurrentAmount == PowerType.Debuff &&
                         p is TemporaryStrengthPower or TemporaryDexterityPower).ToArray())
            {
                if (!target.Powers.Contains(power)) continue;
                await PowerCmd.ModifyAmount(choiceContext, power, -power.Amount, Owner.Creature, this);
            }

            // Use the runtime classification: negative Strength is a debuff even
            // though StrengthPower.Type is Buff. Snapshot safely across callbacks.
            foreach (var power in target.Powers.Where(p => p.TypeForCurrentAmount == PowerType.Debuff).ToArray())
                if (target.Powers.Contains(power) && power.TypeForCurrentAmount == PowerType.Debuff)
                    await PowerCmd.Remove(power);
            ModEntry.Log.Info($"CLEANSE player={player.NetId}");
        }
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
