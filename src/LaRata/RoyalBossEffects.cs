using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

internal static class RoyalBossEffects
{
    internal static decimal CardBlock(MonsterModel caster, decimal basis) => RoyalBalance.CardBlock(basis, caster is RatKing);
    internal static decimal CardHealing(MonsterModel caster, decimal basis) => caster is RatKing ? RoyalBalance.ReducedHealing(basis) : basis;
    internal static decimal GraveLevyHealing(MonsterModel caster) => caster is RatKing ? RoyalBalance.GraveLevyHealing(caster.Creature.MaxHp) : 24;
    internal static void ValidateCatalog()
    {
        if (LaRataCardCatalog.BossCards.Count != RoyalDeck.Costs.Length)
            throw new InvalidOperationException("Royal execution catalog size differs from the shared card catalog.");
        var families = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < RoyalDeck.Costs.Length; i++)
        {
            var entry = LaRataCardCatalog.GetBossEntry(i);
            if (entry.BossEligible != RoyalDeck.ActiveCards.Contains(i) || !families.Add(entry.FamilyId) ||
                LaRataCardCatalog.BossCost(i) != RoyalDeck.Costs[i] || LaRataCardCatalog.BossIsAttack(i) != RoyalDeck.IsAttack(i) ||
                LaRataCardCatalog.BossExhausts(i) != RoyalDeck.IsExhaust(i) || LaRataCardCatalog.BossIsPersistent(i) != RoyalDeck.IsPersistent(i))
                throw new InvalidOperationException($"Royal catalog/adapter mismatch at {i}: {entry.FamilyId}.");
        }
    }

    internal static string Description(int index)
    {
        var text = new LocString("monsters", ModelDb.Monster<RatKing>().Id.Entry + ".royal_adapter." + index);
        return text.Exists() ? text.GetFormattedText() : EnglishDescription(index);
    }
    internal static string EnglishDescription(int index)
    {
        if (index < 25) return index switch
        {
            4 => "Apply 10 Doom and 1 Weak to ALL players.",
            6 => "Gain 12 base Block. If you have no Buffer, gain 1 Buffer.",
            7 => "Lose up to 12 HP without dying. Give ALL living allies 2 Strength.",
            8 => "Gain 8 base Block and 2 Vulnerable.",
            10 => "Deal 18 damage to ALL players. Heal 1% of your Max HP.",
            13 => "Gain 15 base Block and 2 Buffer if no Buffer remains.",
            15 => "Shuffle 2 Dazed into each player's draw pile. Gain 6 base Block.",
            16 => "Heal ALL court members for 1.25% of their Max HP. Give them 2 base Plating (cap 14).",
            19 => "Remove Weak and Frail from every living court member. Gain 5 base Block.",
            22 => "Give ALL court members 15 base Block and 3 Strength.",
            23 => "Deal 26 damage to ALL players. Heal ALL court members for 2% of their Max HP.",
            24 => "Give ALL court members 2 Ritual (cap 6).",
            _ => RoyalCard.Descriptions[index]
        };
        string text = index switch
        {
            25 => "Remove ALL Debuffs from ALL court members. Exhaust.",
            26 => "Next royal hand: +1 Energy and +1 card. Exhaust.",
            27 => "Gain 4 base Block and 4 Vigor.",
            28 => "Apply 5 Poison and 5 Doom to the announced player.",
            29 => "Deal 3 damage to ALL players 4 times.",
            30 => "Heal 1 HP. Next royal hand: +2 cards. Exhaust.",
            31 => "Lose 3 HP. Next royal hand: +2 cards. Requires more than 3 HP. Exhaust.",
            32 => "Discard 1 spare royal card. Next royal hand: +3 cards.",
            33 => "Give ALL court members 2.5 base Block. Next royal hand: +1 Energy.",
            34 => "Gain 1 unpowered Block after each further royal card this combat. Does not stack.",
            35 => "Apply 2 Weak and 2 Vulnerable to ALL players. Exhaust.",
            36 => "Remove ALL Thorns from ALL players. Apply 3 Vulnerable to players that lost Thorns. Exhaust.",
            37 => "Deal 4 damage to ALL players. Gain 1.5 base Block.",
            38 => "Discard 1 spare royal card if available. Gain 4.5 base Block.",
            39 => "Exhaust 1 spare royal card to gain 4 Vigor.",
            40 => "Deal 5 damage to ALL players twice. Apply 4 Doom to players with Poison.",
            41 => "Remove ALL Vulnerable from the announced player. Gain 3 base Block per Vulnerable removed. Exhaust.",
            42 => "Exhaust 1 spare royal card to gain 4 Vigor. Exhaust.",
            43 => "Discard 1 spare royal card if available. Gain 1 base Plating (cap 14). Next royal hand: +1 card.",
            44 => "Gain 4 base Block. Apply 5 Doom to the announced player.",
            45 => "The first active royal discard each turn grants 3 Vigor this combat. Does not stack.",
            46 => "The first royal Exhaust each turn applies 5 Poison to ALL players this combat. Does not stack.",
            RoyalDeck.ResetCard => "Reserve your unrefunded Energy spent this turn for your next royal hand (shared +2 cap). Excludes ALL Reset costs. Exhaust.",
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };
        return text;
    }

    internal static string? Unavailable(RatKing king, int index) => index switch
    {
        12 when Court(king).All(c => c.GetPowerAmount<PlatingPower>() >= 14 * (king.CombatState.Players.Count * 2 - 1)) => RoyalBossText.Text("plating_full", "Every living court member is at the royal Plating cap."),
        24 when Court(king).All(c => c.GetPowerAmount<RitualPower>() >= 6) => RoyalBossText.Text("ritual_full", "Every living court member is at the Ritual cap."),
        16 when Court(king).All(c => c.CurrentHp == c.MaxHp && c.GetPowerAmount<PlatingPower>() >= 14 * (king.CombatState.Players.Count * 2 - 1)) => RoyalBossText.Text("healing_plating_full", "The court is at full HP and the royal Plating cap."),
        25 when !Court(king).Any(c => c.Powers.Any(p => p.TypeForCurrentAmount == PowerType.Debuff)) => RoyalBossText.Text("no_debuff", "No court Debuffs to remove; the royal deck has no Curses."),
        31 when king.Creature.CurrentHp <= 3 => RoyalBossText.Text("unsafe_hp", "Not enough HP to pay safely."),
        36 when !Players(king).Any(c => c.GetPowerAmount<ThornsPower>() > 0) => RoyalBossText.Text("no_thorns", "No living player has Thorns to remove."),
        41 when !Players(king).Any(c => c.GetPowerAmount<VulnerablePower>() > 0) => RoyalBossText.Text("no_vulnerable", "No living player has Vulnerable to remove."),
        _ => null
    };

    internal static Creature[] Court(RatKing king) => king.CombatState.Enemies.Where(c => c.IsAlive && c.Monster is RatKing or KingsguardKnight).OrderBy(c => c.CombatId).ToArray();
    internal static Creature[] Players(RatKing king) => king.CombatState.Players.OrderBy(p => p.NetId).Select(p => p.Creature).Where(c => c.IsAlive).ToArray();
    internal static (int Damage, int Hits) Attack(int index) => index switch
    {
        0 => (12, 1), 1 => (7, 3), 2 => (34, 1), 9 => (5, 4), 10 => (18, 1), 11 => (6, 3),
        17 => (40, 1), 18 => (4, 5), 23 => (26, 1), 29 => (3, 4), 37 => (4, 1), 40 => (5, 2), _ => (12, 1)
    };

    internal static IEnumerable<AbstractIntent> Intents(RatKing king, int index)
    {
        if (RoyalDeck.IsAttack(index))
        {
            var (damage, hits) = Attack(index);
            yield return new RoyalForecastAttackIntent(king, index, damage, hits);
            if (index is 2 or 11 or 40) yield return new DebuffIntent();
            if (index is 9 or 18) yield return new StatusIntent(1);
            if (index is 10 or 23) yield return new HealIntent();
            if (index == 37) yield return new DefendIntent();
            yield break;
        }
        if (index < 25)
        {
            foreach (var intent in RoyalCard.All[index].GetIntents(king)) yield return intent;
            yield break;
        }
        if (index is 27 or 33 or 38 or 41 or 43 or 44) yield return new DefendIntent();
        if (index is 28 or 35 or 36 or 44) yield return new DebuffIntent();
        if (index == 30) yield return new HealIntent();
        yield return new BuffIntent();
    }

    internal static async Task Resolve(RatKing king, int index, IReadOnlyList<Creature> targets)
    {
        if (index < 25)
        {
            if (index is 6 or 13 && king.Creature.GetPowerAmount<BufferPower>() > 0)
                king.Note(index, RoyalBossText.Text("buffer_present", "Existing Buffer is not stacked; Block still resolves."));
            if (index == 10 && king.Creature.CurrentHp == king.Creature.MaxHp || index is 16 or 23 && Court(king).All(c => c.CurrentHp == c.MaxHp))
                king.Note(index, RoyalBossText.Text("full_hp", "Healing has no missing HP to restore; the other effects still resolve."));
            await RoyalCard.All[index].Resolve(king, targets); return;
        }
        var ctx = new ThrowingPlayerChoiceContext();
        var owner = king.Creature;
        var deck = king.Deck ?? throw new InvalidOperationException("Royal deck missing.");
        if (!RoyalDeck.IsAttack(index)) await CreatureCmd.TriggerAnim(owner, "Cast", RatKing.CastImpactTime);
        switch (index)
        {
            case 25:
                foreach (var ally in Court(king))
                {
                    foreach (var power in ally.Powers.Where(p => p.TypeForCurrentAmount == PowerType.Debuff && p is TemporaryStrengthPower or TemporaryDexterityPower).ToArray())
                        if (ally.Powers.Contains(power)) await PowerCmd.ModifyAmount(ctx, power, -power.Amount, owner, null);
                    foreach (var power in ally.Powers.Where(p => p.TypeForCurrentAmount == PowerType.Debuff).ToArray())
                        if (ally.Powers.Contains(power)) await PowerCmd.Remove(power);
                }
                king.Note(index, RoyalBossText.Text("cleansed", "Court Debuffs removed; no royal Curse cards exist.")); break;
            case 26: await king.ReserveNext(index, 1, 1); break;
            case 27: await Block(owner, 8); await Vigor(4); break;
            case 28: await TargetPower<PoisonPower>(5); await TargetPower<DoomPower>(5); break;
            case 29: await AttackNow(); break;
            case 30:
                if (owner.CurrentHp == owner.MaxHp) king.Note(index, RoyalBossText.Text("full_hp", "Healing has no missing HP to restore; the other effects still resolve."));
                await CreatureCmd.Heal(owner, CardHealing(king, 3)); await king.ReserveNext(index, 2, 0); break;
            case 31: await PayHp(3); break;
            case 32: if (await Material(false)) await king.ReserveNext(index, 3, 0); break;
            case 33:
                foreach (var ally in Court(king)) await Block(ally, 5);
                await king.ReserveNext(index, 0, 1); break;
            case 34: await PowerCmd.Apply<RoyalSecondShadowPower>(ctx, owner, 1, owner, null); break;
            case 35:
                await PowerCmd.Apply<WeakPower>(ctx, Players(king), 2, owner, null);
                await PowerCmd.Apply<VulnerablePower>(ctx, Players(king), 2, owner, null); break;
            case 36:
                var stripped = new List<Creature>();
                foreach (var player in Players(king))
                    if (player.GetPower<ThornsPower>() is { Amount: > 0 } thorns)
                    {
                        int before = player.GetPowerAmount<ThornsPower>();
                        await PowerCmd.Remove(thorns);
                        if (player.IsAlive && player.GetPowerAmount<ThornsPower>() < before) stripped.Add(player);
                    }
                if (stripped.Count == 0) king.Note(index, RoyalBossText.Text("no_thorns", "No living player has Thorns to remove."));
                await PowerCmd.Apply<VulnerablePower>(ctx, stripped.Where(c => c.IsAlive), 3, owner, null); break;
            case 37: await AttackNow(); if (owner.IsAlive) await Block(owner, 3); break;
            case 38: await Material(false); await Block(owner, 9); break;
            case 39: if (await Material(true)) await Vigor(4); break;
            case 40:
                await AttackNow();
                await PowerCmd.Apply<DoomPower>(ctx, Players(king).Where(c => c.GetPowerAmount<PoisonPower>() > 0), 4, owner, null); break;
            case 41:
                var graveTarget = king.TargetFor(index);
                if (graveTarget?.IsAlive != true)
                    king.Note(index, RoyalBossText.Text("dead_target", "The announced target is no longer alive; no retargeting."));
                else if (graveTarget.GetPower<VulnerablePower>() is { Amount: > 0 } vulnerable)
                {
                    int before = graveTarget.GetPowerAmount<VulnerablePower>();
                    await PowerCmd.Remove(vulnerable);
                    int removed = Math.Max(0, before - graveTarget.GetPowerAmount<VulnerablePower>());
                    if (removed > 0 && owner.IsAlive) await Block(owner, 6 * removed);
                    else king.Note(index, RoyalBossText.Text("no_vulnerable", "No living player has Vulnerable to remove."));
                }
                else king.Note(index, RoyalBossText.Text("target_no_vulnerable", "The announced target has no Vulnerable to remove; no Block gained."));
                break;
            case 42: if (await Material(true)) await Vigor(4); break;
            case 43:
                await Material(false);
                int scale = Math.Max(1, king.CombatState.Players.Count * 2 - 1);
                int room = Math.Max(0, 14 - (int)Math.Ceiling(owner.GetPowerAmount<PlatingPower>() / (decimal)scale));
                if (room > 0) await PowerCmd.Apply<PlatingPower>(ctx, owner, 1, owner, null);
                else king.Note(index, RoyalBossText.Text("plating_cap", "Plating is at its royal cap; draw reservation still resolves."));
                await king.ReserveNext(index, 1, 0); break;
            case 44: await Block(owner, 8); await TargetPower<DoomPower>(5); break;
            case 45: await PowerCmd.Apply<RoyalSmugglersDividendPower>(ctx, owner, 1, owner, null); break;
            case 46: await PowerCmd.Apply<RoyalCourtOfCindersPower>(ctx, owner, 1, owner, null); break;
            case RoyalDeck.ResetCard:
                var refund = deck.ReserveReset();
                if (refund.Eligible == 0)
                    king.Note(index, RoyalBossText.Text("reset_no_spend", "No unrefunded Energy spent before Reset; ALL Reset costs are excluded."));
                else if (refund.Gained == 0)
                    king.Note(index, RoyalBossText.Text("reset_cap", "The next-hand card Energy cap is full; no refund reserved."));
                else
                    king.Note(index, RoyalBossText.Text("reset_refund", "Next royal hand: +{Energy} Energy refunded. {Remaining} eligible Energy remains unrefunded due to the shared cap.", ("Energy", refund.Gained), ("Remaining", refund.Remaining)));
                break;
            default: throw new InvalidOperationException($"Missing royal adapter {index}.");
        }
        async Task Block(Creature target, decimal basis) => await CreatureCmd.GainBlock(target, CardBlock(king, basis), ValueProp.Move, null);
        async Task Vigor(int amount) => await PowerCmd.Apply<VigorPower>(ctx, owner, amount, owner, null);
        async Task AttackNow()
        {
            var (damage, hits) = Attack(index);
            await DamageCmd.Attack(damage).FromMonster(king).WithHitCount(hits).OnlyPlayAnimOnce()
                .WithAttackerAnim("Attack", RatKing.AttackImpactTime).Execute(ctx);
        }
        async Task TargetPower<T>(int amount) where T : PowerModel, new()
        {
            var target = king.TargetFor(index);
            if (target?.IsAlive == true) await PowerCmd.Apply<T>(ctx, target, amount, owner, null);
            else king.Note(index, RoyalBossText.Text("dead_target", "The announced target is no longer alive; no retargeting."));
        }
        async Task<bool> Material(bool isExhaust)
        {
            var victim = deck.UseMaterial(index, isExhaust);
            if (victim == null) { king.Note(index, RoyalBossText.Text("no_material", "No spare hand card available.")); return false; }
            king.Note(index, RoyalBossText.Text(isExhaust ? "exhausted" : "discarded", isExhaust ? "Exhausted {Card}." : "Discarded {Card}.", ("Card", LaRataCardCatalog.GetBossModel(victim.Value).Title)));
            await king.AfterPileAction(isExhaust);
            return true;
        }
        async Task PayHp(int amount)
        {
            if (owner.CurrentHp <= amount) { king.Note(index, RoyalBossText.Text("unsafe_hp", "Not enough HP to pay safely.")); return; }
            await CreatureCmd.Damage(ctx, owner, amount, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, owner, null);
            if (owner.IsAlive) await king.ReserveNext(index, 2, 0);
        }
    }
}
