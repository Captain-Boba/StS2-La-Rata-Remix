using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;

namespace LaRata;

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterSideTurnStart))]
internal static class PotionGoblin
{
    private static readonly ConditionalWeakTable<ICombatState, object> Visited = new();
    private static readonly Dictionary<Type, MethodInfo> Effects = new();
    private static PotionModel[] Pool =>
    [
        ModelDb.Potion<StrengthPotion>(), ModelDb.Potion<DexterityPotion>(), ModelDb.Potion<BlockPotion>(),
        ModelDb.Potion<RegenPotion>(), ModelDb.Potion<LiquidBronze>(), ModelDb.Potion<FlexPotion>(),
        ModelDb.Potion<SpeedPotion>(), ModelDb.Potion<GhostInAJar>(), ModelDb.Potion<WeakPotion>(),
        ModelDb.Potion<VulnerablePotion>(), ModelDb.Potion<PoisonPotion>(), ModelDb.Potion<FirePotion>()
    ];

    [HarmonyPostfix]
    private static void Postfix(ICombatState combatState, CombatSide side, ref Task __result) => __result = Visit(__result, combatState, side);

    private static async Task Visit(Task original, ICombatState combat, CombatSide side)
    {
        await original;
        // This hook is still in the round transition, before player action phase.
        // It runs after block clearing so a thrown Block Potion actually survives.
        if (side != CombatSide.Player || combat.RoundNumber < 3 || CombatManager.Instance.IsEnding || Visited.TryGetValue(combat, out _)) return;
        var run = combat.RunState;
        // Room history is native saved state, including event combats. The active
        // combat's room entry is already present before its first turn starts.
        int ordinal = Math.Max(1, run.MapPointHistory.SelectMany(a => a).SelectMany(p => p.Rooms)
            .Count(r => r.RoomType is RoomType.Monster or RoomType.Elite or RoomType.Boss));
        var timing = new Rng(run.Rng.Seed, $"LaRata/goblin/v1/block/{(ordinal - 1) / 3}");
        if (!ChaosSelections.GoblinVisits(ordinal, timing.NextInt(3))) return;
        int arrivalRound = 3 + new Rng(run.Rng.Seed, $"LaRata/goblin/v1/arrival/{ordinal}").NextInt(2);
        if (combat.RoundNumber != arrivalRound) return;
        Visited.Add(combat, new object());
        var players = combat.Players.Where(p => p.Creature.IsAlive).OrderBy(p => p.NetId).ToArray();
        if (players.Length == 0) return;
        var pool = Pool;
        var rng = new Rng(run.Rng.Seed, $"LaRata/goblin/v1/potions/{ordinal}");
        var selections = ChaosSelections.Distinct(players.Length, pool.Length, n => rng.NextInt(n));
        var room = NCombatRoom.Instance;
        Sprite2D? goblin = null;
        Texture2D[] poses = [];
        try
        {
            if (room != null)
            {
                var sheet = ResourceLoader.Load<Texture2D>("res://LaRata/images/potion_goblin_poses.png");
                goblin = new Sprite2D
                {
                    Position = new Vector2(2020, 630), Scale = Vector2.One * (300f / (sheet.GetHeight() / 2f))
                };
                poses = new Texture2D[]
                {
                    new AtlasTexture { Atlas = sheet, Region = new Rect2(0, 0, 627, 627) },
                    new AtlasTexture { Atlas = sheet, Region = new Rect2(627, 0, 627, 627) },
                    // The throwing foot extends slightly past the sheet's midpoint.
                    new AtlasTexture { Atlas = sheet, Region = new Rect2(0, 627, 640, 627) },
                    new AtlasTexture { Atlas = sheet, Region = new Rect2(640, 627, 614, 627) }
                };
                SetPose(goblin, poses, 0);
                room.CombatVfxContainer.AddChild(goblin);
                var entrance = goblin.CreateTween();
                entrance.TweenProperty(goblin, "position:x", 1660f, 1.0).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
                entrance.TweenInterval(1.2);
                // Native tween completion preserves the cameo even in Instant mode,
                // where Cmd.Wait is skipped. It also cancels safely on room exit.
                if (!await entrance.AwaitFinished(goblin)) return;
            }
            for (int i = 0; i < players.Length; i++)
            {
                if (CombatManager.Instance.IsEnding) break;
                var player = players[i];
                if (!player.Creature.IsAlive) continue;
                var potion = (PotionModel)pool[selections[i]].ToMutable();
                potion.Owner = player;
                ModEntry.Log.Info($"GOBLIN combat={ordinal} round={combat.RoundNumber} player={player.NetId} potion={potion.Id.Entry}");
                if (room != null && goblin != null)
                {
                    SetPose(goblin, poses, 1);
                    var bottle = new Sprite2D
                    {
                        Texture = potion.Image,
                        Position = new Vector2(75, -75) / goblin.Scale,
                        Scale = Vector2.One * (48f / potion.Image.GetWidth()) / goblin.Scale
                    };
                    goblin.AddChild(bottle);
                    var windup = goblin.CreateTween();
                    windup.TweenProperty(goblin, "rotation", .06f, .35).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
                    windup.TweenInterval(.3);
                    if (!await windup.AwaitFinished(goblin)) return;
                    bottle.QueueFree();
                    SetPose(goblin, poses, 2);
                    var release = goblin.CreateTween();
                    release.TweenProperty(goblin, "rotation", -.04f, .2).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
                    release.TweenProperty(goblin, "rotation", 0f, .3).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
                    var target = room.GetCreatureNode(player.Creature)?.VfxSpawnPosition ?? Vector2.Zero;
                    var vfx = NItemThrowVfx.Create(goblin.ToGlobal(new Vector2(-145, -5) / goblin.Scale), target, potion.Image, Vector2.One * .6f);
                    if (vfx != null)
                    {
                        // Native exported scene property; retain the game's throw arc.
                        vfx.Set(NItemThrowVfx.PropertyName._flightTime, .85f);
                        room.CombatVfxContainer.AddChild(vfx);
                    }
                    var flight = goblin.CreateTween();
                    flight.TweenInterval(.9);
                    if (!await flight.AwaitFinished(goblin)) return;
                }
                // Native OnUse contains each potion's real mechanics. Its public wrapper
                // consumes an inventory slot and fires player-drinking hooks, unsuitable
                // for an NPC splash. No fabricated inventory or Player is created.
                if (!Effects.TryGetValue(potion.GetType(), out var method))
                    Effects[potion.GetType()] = method = AccessTools.Method(potion.GetType(), "OnUse", [typeof(PlayerChoiceContext), typeof(Creature)])
                        ?? throw new MissingMethodException(potion.GetType().Name, "OnUse");
                var context = new ThrowingPlayerChoiceContext();
                context.PushModel(potion);
                CombatManager.Instance.BeginCardOrPotionEffect(player);
                try { await (Task)method.Invoke(potion, [context, player.Creature])!; }
                finally { CombatManager.Instance.EndCardOrPotionEffect(player); context.PopModel(potion); }
                if (goblin != null && GodotObject.IsInstanceValid(goblin))
                {
                    SetPose(goblin, poses, 3);
                    var reaction = goblin.CreateTween();
                    reaction.TweenProperty(goblin, "position:y", 624f, .25).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
                    reaction.TweenProperty(goblin, "position:y", 630f, .3).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
                    reaction.TweenInterval(.45);
                    if (!await reaction.AwaitFinished(goblin)) return;
                }
            }
            if (goblin != null && GodotObject.IsInstanceValid(goblin))
            {
                SetPose(goblin, poses, 0);
                var exit = goblin.CreateTween();
                exit.TweenProperty(goblin, "position:x", 2020f, .8).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
                await exit.AwaitFinished(goblin);
            }
        }
        finally { if (goblin != null && GodotObject.IsInstanceValid(goblin)) goblin.QueueFree(); }
    }

    private static void SetPose(Sprite2D goblin, Texture2D[] poses, int pose)
    {
        goblin.Texture = poses[pose];
        // Register the planted feet, retaining each pose's natural upper-body motion.
        goblin.Offset = pose switch
        {
            0 => new Vector2(-77, 0),
            1 => new Vector2(-35.5f, 0),
            2 => new Vector2(-81, 60),
            _ => new Vector2(-13.5f, 58)
        };
    }
}
