using System.Runtime.CompilerServices;
using BaseLib.Abstracts;
using BaseLib.Audio;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace LaRata;

// A real power puts the concealed card and its round into native combat checksums.
// DisplayAmount deliberately reveals neither; the UI only announces an unknown card.
public sealed class RoyalInterventionPower : CustomPowerModel
{
    internal const string Notice = "La Rata will play an unknown card before the enemies act. Defeat this marked enemy before then to cancel it.";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => 0;
    public override string? CustomPackedIconPath => "res://images/atlases/intent_atlas.sprites/intent_card_debuff.tres";
    public override string? CustomBigIconPath => CustomPackedIconPath;
    public override List<(string, string)> Localization => [("title", "The King's Gambit"), ("description", Notice)];
    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength) =>
        creature == Owner && !wasRemovalPrevented ? PowerCmd.Remove(this) : Task.CompletedTask;
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterSideTurnStart))]
internal static class RoyalIntervention
{
    private static readonly ConditionalWeakTable<ICombatState, object> Announced = new();
    [HarmonyPostfix]
    private static void Postfix(ICombatState combatState, CombatSide side, ref Task __result) => __result = Run(__result, combatState, side);

    private static async Task Run(Task original, ICombatState combat, CombatSide side)
    {
        await original;
        if (CombatManager.Instance.IsEnding || combat.Encounter is not { } encounter ||
            encounter is RatKingEncounter or KingsguardEncounter ||
            encounter.RoomType is not (RoomType.Monster or RoomType.Elite) ||
            encounter.IsDebugEncounter || !encounter.ShouldGiveRewards) return;

        if (side == CombatSide.Enemy)
        {
            // All native start-turn hooks/block clearing have finished, but no monster
            // has attacked. Death/removal cancels the marked enemy's power, no retarget.
            var marked = combat.Enemies.Where(c => c.IsAlive && c.Monster != null)
                .OrderBy(c => c.CombatId).Select(c => c.GetPower<RoyalInterventionPower>())
                .FirstOrDefault(p => p != null);
            if (marked == null || combat.RoundNumber < CameoSchedule.Round(marked.Amount)) return;
            int index = CameoSchedule.Card(marked.Amount);
            var owner = marked.Owner;
            await PowerCmd.Remove(marked);
            if (!owner.IsAlive || CombatManager.Instance.IsEnding) return;
            if (!CameoSchedule.RoyalPool.Contains(index))
            {
                ModEntry.Log.Warn("ROYAL CAMEO cancelled: another effect changed its encoded power amount.");
                return;
            }
            var card = RoyalCard.All[index];
            ModEntry.Log.Info($"ROYAL CAMEO play round={combat.RoundNumber} target={owner.CombatId} card={card.Id.Entry}");
            // Cosmetic audio/visuals do not select or gate a gameplay result.
            if (NCombatRoom.Instance is { } room)
                ModAudio.PlaySound(new ModSound("res://LaRata/audio/royal_chuckle.mp3"), volumeAdd: -3, targetNode: room);
            var preview = await RoyalDeckView.Show(owner, card, offscreen: true);
            try
            {
                if (ReferenceEquals(CombatManager.Instance.DebugOnlyGetState(), combat) && owner.IsAlive && !CombatManager.Instance.IsEnding)
                    await card.Resolve(owner.Monster!, combat.Players.OrderBy(p => p.NetId).Select(p => p.Creature).ToArray());
            }
            finally { RoyalDeckView.Dismiss(preview); }
            return;
        }
        if (side != CombatSide.Player || Announced.TryGetValue(combat, out _) ||
            combat.Enemies.Any(c => c.GetPower<RoyalInterventionPower>() != null)) return;

        var run = combat.RunState;
        var rooms = run.MapPointHistory.SelectMany(a => a).SelectMany(p => p.Rooms).ToArray();
        int ordinal = Math.Max(1, rooms.Count(r => r.RoomType is RoomType.Monster or RoomType.Elite or RoomType.Boss));
        int eligible = Math.Max(1, rooms.Count(r => (r.RoomType is RoomType.Monster or RoomType.Elite) &&
            r.ModelId != ModelDb.Encounter<KingsguardEncounter>().Id));
        if (combat.RoundNumber != CameoSchedule.RoyalRound(run.Rng.Seed, eligible, ordinal)) return;
        Announced.Add(combat, new object());
        var targets = combat.Enemies.Where(c => c.IsAlive && c.CanReceivePowers && c.Monster != null)
            .OrderBy(c => c.CombatId).ToArray();
        if (targets.Length == 0) return;
        var rng = new Rng(run.Rng.Seed, $"LaRata/royal-cameo/v1/selection/{ordinal}/{encounter.Id.Entry}");
        var target = targets[rng.NextInt(targets.Length)];
        int chosen = CameoSchedule.RoyalPool[rng.NextInt(CameoSchedule.RoyalPool.Length)];
        await PowerCmd.Apply<RoyalInterventionPower>(new ThrowingPlayerChoiceContext(), target,
            CameoSchedule.Encode(combat.RoundNumber, chosen), null, null);
        ModEntry.Log.Info($"ROYAL CAMEO announce combat={ordinal} round={combat.RoundNumber} target={target.CombatId}; hidden card");
    }
}

[HarmonyPatch(typeof(NCreature), nameof(NCreature._Ready))]
internal static class RoyalInterventionMarker
{
    [HarmonyPostfix]
    private static void Postfix(NCreature __instance)
    {
        var creature = __instance.Entity;
        if (creature?.Monster == null) return;
        TextureRect? marker = null;
        void Hide()
        {
            if (marker == null || !GodotObject.IsInstanceValid(marker)) return;
            NHoverTipSet.Remove(marker);
            marker.QueueFree();
            marker = null;
        }
        void Refresh()
        {
            if (!creature.IsAlive || creature.GetPower<RoyalInterventionPower>() == null) { Hide(); return; }
            if (marker != null || !GodotObject.IsInstanceValid(__instance)) return;
            var icon = new CardDebuffIntent().GetTexture([], creature);
            marker = new TextureRect
            {
                Name = "RoyalIntervention", Texture = icon, Position = new Vector2(-44, -112), Size = new Vector2(88, 88),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Stop,
                Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://LaRata/shaders/royal_marker.gdshader") }
            };
            __instance.Visuals.IntentPosition.AddChild(marker);
            var shown = marker;
            shown.MouseEntered += () => NHoverTipSet.CreateAndShow(shown, new HoverTip(ModelDb.Power<RoyalInterventionPower>().Title, RoyalInterventionPower.Notice, icon));
            shown.MouseExited += () => NHoverTipSet.Remove(shown);
            // Gentle breathing, no twitching or gameplay RNG.
            var pulse = shown.CreateTween().SetLoops();
            pulse.TweenProperty(shown, "modulate:a", .90f, 1.3).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
            pulse.TweenProperty(shown, "modulate:a", 1f, 1.3).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        }
        void Changed(PowerModel _) => Refresh();
        void Died(Creature _) => Hide();
        creature.PowerApplied += Changed;
        creature.PowerRemoved += Changed;
        creature.Died += Died;
        __instance.TreeExiting += () => { creature.PowerApplied -= Changed; creature.PowerRemoved -= Changed; creature.Died -= Died; Hide(); };
        Refresh();
    }
}
