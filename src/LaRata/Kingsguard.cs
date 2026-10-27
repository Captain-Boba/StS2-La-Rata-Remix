using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace LaRata;

// Reuse the installed game's Spine scenes and animators, not its monster AI.
// Nothing from the original knight encounter is patched or redistributed.
public abstract class KingsguardKnight : CustomMonsterModel, ILocalizationProvider
{
    protected abstract MonsterModel Model { get; }
    protected abstract string NativeName { get; }
    protected abstract string GuardName { get; }
    protected abstract int StartingPlating { get; }
    protected override string VisualsPath => $"res://scenes/creature_visuals/{NativeName}.tscn";
    public override CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller) => Model.GenerateAnimator(controller);
    public override string? CustomDeathSfx => Model.DeathSfx;
    public override string? HurtSfx => Model.HurtSfx;
    public override float HpBarSizeReduction => 30;
    public override int MaxInitialHp => MinInitialHp;
    public List<(string, string)> Localization => [];

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal damage, ValueProp props,
        Creature? dealer, CardModel? cardSource) => RoyalDamageRamp.For(Creature, dealer, props);
    private RatKing? sovereign;
    // Bestiary builds and evaluates moves before a Creature exists. Bind the
    // commander only through the native live-room lifecycle.
    protected int PendingRoyalStrength => sovereign?.Creature.IsAlive == true ? sovereign.PendingStrength : 0;
    protected AbstractIntent RoyalAttackIntent(int damage, int hits = 1) => new RoyalForecastAttackIntent(() => sovereign, damage, hits);
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        sovereign = CombatState.Enemies.Select(c => c.Monster).OfType<RatKing>().FirstOrDefault();
    }
    protected Creature[] Court => CombatState.Enemies.Where(c => c.IsAlive && c.Monster is KingsguardKnight or RatKing)
        .OrderBy(c => c.CombatId).ToArray();
    public override Task BeforeCombatStart() => Plate([Creature], StartingPlating);

    protected async Task Plate(IEnumerable<Creature> targets, int amount)
    {
        // Native Plating scales by 1/3/5/7 and decays by party size. Cap in the
        // same units, rather than feeding an already-scaled amount back into Apply.
        amount *= 3;
        int scale = 2 * CombatState.Players.Count - 1;
        foreach (var target in targets)
        {
            if (!Creature.IsAlive || CombatManager.Instance.IsEnding) break;
            if (!target.IsAlive) continue;
            int grant = Math.Min(amount, Math.Max(0, 42 * scale - target.GetPowerAmount<PlatingPower>()) / scale);
            if (grant > 0) await PowerCmd.Apply<PlatingPower>(new ThrowingPlayerChoiceContext(), target, grant, Creature, null);
        }
    }
    protected async Task Shield(IEnumerable<Creature> targets, int block)
    {
        foreach (var target in targets)
        {
            if (!Creature.IsAlive || CombatManager.Instance.IsEnding) break;
            if (target.IsAlive) await CreatureCmd.GainBlock(target, block, ValueProp.Move, null);
        }
    }
    protected Task Cast(string trigger = "Cast") => CreatureCmd.TriggerAnim(Creature, trigger, .45f);
    protected async Task Strike(int damage, int hits, string trigger, string sound) =>
        await DamageCmd.Attack(damage).WithHitCount(hits).FromMonster(this).OnlyPlayAnimOnce()
            .WithAttackerAnim(trigger, .45f).WithAttackerFx(null, $"event:/sfx/enemy/enemy_attacks/{NativeName}/{sound}")
            .Execute(new ThrowingPlayerChoiceContext());
    protected static MonsterMoveStateMachine Cycle(MoveState first, MoveState second, MoveState third)
    {
        first.FollowUpState = second; second.FollowUpState = third; third.FollowUpState = first;
        return new MonsterMoveStateMachine([first, second, third], first);
    }
}

public sealed class CrownBulwark : KingsguardKnight
{
    protected override MonsterModel Model => ModelDb.Monster<FlailKnight>();
    protected override string NativeName => "flail_knight";
    protected override string GuardName => "Crown Bulwark";
    public override int MinInitialHp => 220;
    protected override int StartingPlating => 10;
    protected override MonsterMoveStateMachine GenerateMoveStateMachine() => Cycle(
        new MoveState("OATH", async _ => { await Cast(); await Shield(Court.Where(c => c != Creature), 14); await Plate(Court, 2); }, new DefendIntent(), new BuffIntent()),
        new MoveState("STRIKE", _ => Strike(18, 2, "FlailAttack", "flail_knight_flail"), RoyalAttackIntent(18, 2)),
        new MoveState("RENEW", async _ => { await Strike(36, 1, "RamAttack", "flail_knight_ram"); await Shield([Creature], 10); }, RoyalAttackIntent(36), new DefendIntent()));
}

public sealed class CrownHeadsman : KingsguardKnight
{
    protected override MonsterModel Model => ModelDb.Monster<FlailKnight>();
    protected override string NativeName => "flail_knight";
    protected override string GuardName => "Crown Headsman";
    public override int MinInitialHp => 210;
    protected override int StartingPlating => 8;
    protected override MonsterMoveStateMachine GenerateMoveStateMachine() => Cycle(
        new MoveState("STRIKE", _ => Strike(20, 2, "FlailAttack", "flail_knight_flail"), RoyalAttackIntent(20, 2)),
        new MoveState("OATH", async _ => { await Cast(); await Plate(Court.Where(c => c != Creature), 3); await Shield([Creature], 16); }, new BuffIntent(), new DefendIntent()),
        new MoveState("RENEW", _ => Strike(36, 1, "RamAttack", "flail_knight_ram"), RoyalAttackIntent(36)));
}

public sealed class CrownArmorer : KingsguardKnight
{
    protected override MonsterModel Model => ModelDb.Monster<MagiKnight>();
    protected override string NativeName => "magi_knight";
    protected override string GuardName => "Crown Armorer";
    public override int MinInitialHp => 165;
    protected override int StartingPlating => 6;
    protected override MonsterMoveStateMachine GenerateMoveStateMachine() => Cycle(
        new MoveState("OATH", async _ => { await Cast("ShieldAttack"); await Plate(Court, 3); }, new BuffIntent()),
        new MoveState("STRIKE", async _ => { await Strike(16, 1, "ShieldAttack", "magi_knight_cast_shield"); await Shield(Court.Where(c => c != Creature), 8); }, RoyalAttackIntent(16), new DefendIntent()),
        new MoveState("RENEW", async _ => {
            await Cast("ShieldAttack");
            var ally = Court.Where(c => c != Creature).OrderBy(c => (decimal)c.CurrentHp / c.MaxHp).ThenBy(c => c.CombatId).FirstOrDefault();
            if (ally != null) { await CreatureCmd.Heal(ally, RoyalBalance.GuardHealing(ally.MaxHp, .25m, ally.Monster is RatKing)); await Plate([ally], 4); }
            else await Shield([Creature], 12);
        }, new HealIntent(), new BuffIntent(), new DefendIntent()));
}

public sealed class CrownHerald : KingsguardKnight
{
    protected override MonsterModel Model => ModelDb.Monster<SpectralKnight>();
    protected override string NativeName => "spectral_knight";
    protected override string GuardName => "Crown Herald";
    public override int MinInitialHp => 155;
    protected override int StartingPlating => 6;
    protected override MonsterMoveStateMachine GenerateMoveStateMachine() => Cycle(
        new MoveState("STRIKE", _ => Strike(10, 2, "AttackFlame", "spectral_knight_soul_flame"), RoyalAttackIntent(10, 2)),
        new MoveState("RENEW", async _ => {
            await Cast();
            foreach (var ally in Court) await CreatureCmd.Heal(ally, RoyalBalance.GuardHealing(ally.MaxHp, .10m, ally.Monster is RatKing));
        }, new HealIntent()),
        new MoveState("OATH", async _ => { await Cast(); await Shield(Court, 10); await Plate(Court.Where(c => c != Creature), 2); }, new DefendIntent(), new BuffIntent()));
}

public sealed class KingsguardEncounter : CustomEncounterModel, ILocalizationProvider
{
    public KingsguardEncounter() : base(RoomType.Elite) { }
    public List<(string, string)> Localization => [];
    public override bool IsValidForAct(ActModel act) => false;
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        [ModelDb.Monster<CrownBulwark>(), ModelDb.Monster<CrownHeadsman>(), ModelDb.Monster<CrownArmorer>(), ModelDb.Monster<CrownHerald>()];
    public override string? CustomScenePath => "res://LaRata/scenes/kingsguard_encounter.tscn";
    public override BackgroundAssets? CustomEncounterBackground(ActModel parentAct, Rng rng) => new("queen_boss", rng);
    public override float GetCameraScaling() => .78f;
    public override Vector2 GetCameraOffset() => Vector2.Down * 35;
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<CrownBulwark>().ToMutable(), "bulwark"),
        (ModelDb.Monster<CrownHeadsman>().ToMutable(), "headsman"),
        (ModelDb.Monster<CrownArmorer>().ToMutable(), "armorer"),
        (ModelDb.Monster<CrownHerald>().ToMutable(), "herald")
    ];
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
internal static class KingsguardSkin
{
    [HarmonyPostfix]
    private static void Postfix(NCreature __instance)
    {
        if (__instance.Entity?.Monster is not KingsguardKnight) return;
        var visuals = __instance.Visuals;
        // Spine's supported normal-material slot preserves skeletal animations,
        // native hit flashes, death effects and transient potion overlays.
        var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://LaRata/shaders/kingsguard.gdshader") };
        material.SetShaderParameter("accent", __instance.Entity.Monster is CrownHeadsman ? new Color(.12f, .70f, .36f) : new Color(.12f, .85f, .57f));
        material.SetShaderParameter("chest_energy", __instance.Entity.Monster is CrownBulwark or CrownHeadsman ? 1f : 0f);
        visuals.SpineBody?.SetNormalMaterial(material);
        __instance.SetScaleAndHue(.82f, 0);
    }
}
