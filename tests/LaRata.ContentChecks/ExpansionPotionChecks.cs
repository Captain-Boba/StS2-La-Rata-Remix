using System.Reflection;
using System.Text.Json;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

// --expansion-potions: standalone, BEFORE Program's general WaveIds setup.
// Native use-action body, wrapper, commands and packet payloads; supplied
// desktop/run/combat entry, no queue transport, game window or full disk save.
internal static class ExpansionPotionChecks
{
    private static int checks;
    private static readonly PlayerChoiceContext Context = new ThrowingPlayerChoiceContext();
    private static void Check(bool value, string why) { checks++; if (!value) throw new Exception(why); }
    private static bool DisplayName(Creature __instance, ref string __result)
    { __result = __instance.IsPlayer ? "fixture-player-" + __instance.Player!.NetId : "fixture-enemy-" + __instance.CombatId; return false; }
    private static (RunState run, CombatState combat, Player[] players, Creature[] enemies) Setup(int count = 2)
    {
        var players = Enumerable.Range(0, count).Select(i => NativeConsoleFixture.Player((ulong)(1301 + i))).ToArray();
        var run = RunState.CreateForTest(players: players, seed: "EXPANSIONPOTIONS");
        NativeConsoleFixture.Set(RunManager.Instance, "State", run);
        NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.AscensionManager),
            new MegaCrit.Sts2.Core.Entities.Ascension.AscensionManager(0));
        var combat = new CombatState(ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable(), run);
        AccessTools.Field(typeof(CombatManager), "_state").SetValue(CombatManager.Instance, combat);
        NativeConsoleFixture.Set(CombatManager.Instance, nameof(CombatManager.IsInProgress), true);
        foreach (var player in players)
        {
            NativeConsoleFixture.Set(player, nameof(player.Creature), new Creature(player, 100, 100) { CombatState = combat });
            player.ResetCombatState(); combat.AddPlayer(player);
            ((List<PotionModel?>)AccessTools.Field(typeof(Player), "_potionSlots").GetValue(player)!).Add(null);
        }
        var enemies = Enumerable.Range(0, 3).Select(i => combat.CreateCreature(ModelDb.Monster<TwoTailedRat>().ToMutable(),
            CombatSide.Enemy, "potion-target-" + i)).ToArray();
        foreach (var enemy in enemies) { enemy.SetMaxHpInternal(100); enemy.SetCurrentHpInternal(100); combat.AddCreature(enemy); }
        return (run, combat, players, enemies);
    }
    private static T Seed<T>(Creature owner, int amount) where T : PowerModel, new()
    { var power = (T)ModelDb.Power<T>().ToMutable(); power.ApplyInternal(owner, amount, true); return power; }
    private static T Procure<T>(Player owner) where T : PotionModel, new()
    {
        var potion = (T)ModelDb.Potion<T>().ToMutable();
        Check(PotionCmd.TryToProcure(potion, owner).GetAwaiter().GetResult().success, "Native procure failed: " + potion.Id);
        return potion;
    }
    private static void Execute(UsePotionAction action) =>
        ((Task)AccessTools.Method(typeof(UsePotionAction), "ExecuteAction").Invoke(action, null)!).GetAwaiter().GetResult();
    private static void Use<T>(Player owner, Creature? target = null) where T : PotionModel, new()
    {
        var potion = Procure<T>(owner);
        Execute(new UsePotionAction(potion, target ?? owner.Creature, true));
        Check(potion.HasBeenRemovedFromState && owner.GetPotionAtSlotIndex(0) == null, "Native use did not consume potion");
    }
    internal static void Run()
    {
        checks = 0; bool oldTest = TestMode.IsOn; TestMode.IsOn = true;
        var harmony = new Harmony("LaRata.ContentChecks.ExpansionPotions");
        var oldRun = RunManager.Instance.DebugOnlyGetState(); var oldAscension = RunManager.Instance.AscensionManager;
        var state = AccessTools.Field(typeof(CombatManager), "_state"); var oldState = state.GetValue(CombatManager.Instance);
        bool oldProgress = CombatManager.Instance.IsInProgress; var oldLocal = LocalContext.NetId; LocalContext.NetId = null;
        try
        {
            NativeConsoleFixture.PatchDesktop(harmony);
            harmony.Patch(AccessTools.PropertyGetter(typeof(Creature), nameof(Creature.LogName)),
                prefix: new(typeof(ExpansionPotionChecks), nameof(DisplayName)));
            harmony.CreateClassProcessor(typeof(BaseLib.Patches.Content.PrefixIdPatch)).Patch();
            foreach (var type in typeof(AbstractModel).Assembly.GetTypes().Concat(typeof(ModEntry).Assembly.GetTypes())
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(AbstractModel)) && t.GetConstructor(Type.EmptyTypes) != null))
            { ModelDb.Inject(type); if (type.Assembly == typeof(ModEntry).Assembly) SavedPropertiesTypeCache.InjectTypeIntoCache(type); }
            RelicMetrics.Register(); ModEntry.ApplyPatches(harmony); RelicMetricsDisabledChecks.CheckPatchSet(harmony);
            ModelIdSerializationCache.Init();
            Brass(); Amber(); Rust(); Grave(); Identity(); Packets();
            Console.WriteLine($"PASS: expansion potions {checks} checks; native use-action bodies/wrappers, power/block commands, turn-clear/decrement ordering, IDs/JSON/packets. Statistics OFF. Supplied desktop/combat/turn entries; no UI, transport, complete scheduler or disk-save claim.");
        }
        finally
        {
            NativeConsoleFixture.Set(RunManager.Instance, "State", oldRun);
            NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.AscensionManager), oldAscension);
            state.SetValue(CombatManager.Instance, oldState);
            NativeConsoleFixture.Set(CombatManager.Instance, nameof(CombatManager.IsInProgress), oldProgress);
            LocalContext.NetId = oldLocal; harmony.UnpatchAll(harmony.Id); TestMode.IsOn = oldTest;
        }
    }
    private static void Brass()
    {
        foreach (int count in new[] { 1, 2, 4 }) foreach (int block in new[] { 0, 23 })
        {
            var (_, _, players, _) = Setup(count); var owner = players[^1]; Seed<VigorPower>(owner.Creature, 4);
            CreatureCmd.GainBlock(owner.Creature, block, ValueProp.Unpowered, null).GetAwaiter().GetResult();
            Use<BrassPolish>(owner); Use<BrassPolish>(owner);
            Check(owner.Creature.Block == block && owner.Creature.GetPowerAmount<VigorPower>() == 4 + block * 2,
                "Brass must snapshot actual Block, retain it and stack native Vigor");
            Check(players.Take(count - 1).All(p => p.Creature.GetPowerAmount<VigorPower>() == 0), "Brass changed a partner");
        }
        var (_, _, ps, _) = Setup(); var a = ps[0]; a.Creature.SetCurrentHpInternal(0);
        var unused = Procure<BrassPolish>(a); Execute(new UsePotionAction(unused, a.Creature, true));
        Check(!unused.HasBeenRemovedFromState && a.Creature.GetPowerAmount<VigorPower>() == 0,
            "Native action consumed a potion for a dead self target or granted Vigor");
        Console.WriteLine("PASS Brass: zero/positive Block, repeated stacks, 1/2/4 owners, native procure/use and dead-owner boundary.");
    }
    private static void Turn(CombatState combat, CombatSide side, params Creature[] participants)
    {
        foreach (var creature in participants)
        {
            if (creature.Player?.PlayerCombatState is { } playerState) playerState.IncrementTurnNumber();
            creature.BeforeTurnStart(side); creature.AfterTurnStart(side).GetAwaiter().GetResult();
        }
        // Production clears Block through Creature.AfterTurnStart BEFORE this
        // hook decrements Blur. Owner participants also cover extra-turn starts.
        Hook.AfterSideTurnStart(combat, side, participants).GetAwaiter().GetResult();
    }
    private static void Amber()
    {
        var (_, combat, players, enemies) = Setup(); var a = players[0]; var b = players[1];
        a.Creature.GainBlockInternal(19); b.Creature.GainBlockInternal(11);
        Use<AmberHourglass>(a);
        Check(a.Creature.GetPowerAmount<BlurPower>() == 2 && b.Creature.GetPowerAmount<BlurPower>() == 0, "Hourglass is not own native Blur 2");
        Turn(combat, CombatSide.Enemy, enemies);
        Turn(combat, CombatSide.Player, b.Creature);
        Check(a.Creature.Block == 19 && a.Creature.GetPowerAmount<BlurPower>() == 2 && b.Creature.Block == 0,
            "Other side/partner turn decremented owner's Blur or retained partner Block");
        for (int i = 1; i <= 2; i++)
        {
            Turn(combat, CombatSide.Player, a.Creature);
            Check(a.Creature.Block == 19 && a.Creature.GetPowerAmount<BlurPower>() == 2 - i, "Blur clear/decrement order is off by one");
        }
        Turn(combat, CombatSide.Player, a.Creature);
        Check(a.Creature.Block == 0 && a.Creature.GetPower<BlurPower>() == null, "Hourglass retained Block on third own turn");
        Use<AmberHourglass>(a); Use<AmberHourglass>(a);
        Check(a.Creature.GetPowerAmount<BlurPower>() == 4, "Repeated Hourglass does not use native stack semantics");
        a.Creature.GainBlockInternal(9);
        for (int i = 0; i < 4; i++) Turn(combat, CombatSide.Player, a.Creature);
        Check(a.Creature.Block == 9, "Stacked Blur failed before four own starts");
        Turn(combat, CombatSide.Player, a.Creature);
        Check(a.Creature.Block == 0, "Stacked Blur outlived native duration");
        Console.WriteLine("PASS Amber: native Blur, exactly two own clear/decrement cycles, partner/enemy/extra-turn participant isolation and native repeated-potion stacks.");
    }
    private static void Rust()
    {
        foreach (int count in new[] { 1, 2, 4 })
        {
            var (_, combat, players, enemies) = Setup(count); var a = players[^1]; var target = enemies[1];
            var plating = Seed<PlatingPower>(target, 7); Seed<ArtifactPower>(target, 2);
            plating.BeforeSideTurnEndEarly(Context, CombatSide.Enemy, [target]).GetAwaiter().GetResult();
            target.GainBlockInternal(13); enemies[0].GainBlockInternal(5); players[0].Creature.GainBlockInternal(11);
            Use<Rustwater>(a, target);
            Check(target.Block == 0 && target.GetPower<PlatingPower>() == null && target.GetPowerAmount<ArtifactPower>() == 2 &&
                target.GetPowerAmount<VulnerablePower>() == 0 && target.CurrentHp == 100, "Rustwater did not remove ALL Block/Plating only");
            Check(enemies[0].Block == 5 && players[0].Creature.Block == 11, "Rustwater changed an untargeted creature");
            Use<Rustwater>(a, target);
            Check(target.Block == 0 && target.GetPower<PlatingPower>() == null, "Zero-state Rustwater changed target");
        }
        Console.WriteLine("PASS Rustwater: actual Plating-generated Block plus existing Block stripped; zero states, Artifact retained, no Vulnerable, 1/2/4-owner target isolation.");
    }
    private static void Identity()
    {
        foreach (var (type, rarity, art) in new[] {
            (typeof(BrassPolish), PotionRarity.Uncommon, "brass_polish"),
            (typeof(AmberHourglass), PotionRarity.Uncommon, "amber_hourglass"),
            (typeof(GraveDistillate), PotionRarity.Rare, "grave_distillate") })
        {
            var model = ModelDb.GetById<PotionModel>(ModelDb.GetId(type));
            Check(ModelDb.PotionPool<SharedPotionPool>().AllPotions.Count(p => p.Id == model.Id) == 1 &&
                model.Rarity == rarity && model.Usage == PotionUsage.CombatOnly, "New shared potion registration/rarity mismatch");
            Check(((RoyalPotion)model).CustomPackedImagePath == $"res://LaRata/images/potions/{art}.png", "Potion art binding mismatch");
            var saved = model.ToMutable().ToSerializable(0);
            var loaded = PotionModel.FromSerializable(JsonSerializer.Deserialize(JsonSerializer.Serialize(saved,
                JsonSerializationUtility.GetTypeInfo<SerializablePotion>()), JsonSerializationUtility.GetTypeInfo<SerializablePotion>())!);
            Check(loaded.Id == model.Id, "Potion JSON ID changed");
        }
        Check(ModelDb.Potion<Rustwater>().Rarity == PotionRarity.Uncommon && ModelDb.Potion<Rustwater>().ArtKey == "rustwater",
            "Existing Rustwater rarity/art changed");
    }
    private static void Packets()
    {
        foreach (var type in new[] { typeof(BrassPolish), typeof(AmberHourglass), typeof(Rustwater), typeof(GraveDistillate) })
        {
            byte[]? payload = null; string? firstOutcome = null;
            for (int peer = 0; peer < 2; peer++)
            {
                var (run, combat, players, enemies) = Setup(4); var owner = players[^1];
                owner.Creature.GainBlockInternal(11); enemies[1].GainBlockInternal(13); Seed<PlatingPower>(enemies[1], 7);
                Seed<PoisonPower>(enemies[1], 7); Seed<DoomPower>(enemies[1], 2);
                var potion = ModelDb.GetById<PotionModel>(ModelDb.GetId(type)).ToMutable();
                Check(PotionCmd.TryToProcure(potion, owner).GetAwaiter().GetResult().success, "Packet fixture could not procure potion");
                var target = potion.TargetType == TargetType.Self ? owner.Creature : enemies[1];
                if (payload == null)
                {
                    var writer = new PacketWriter(); ((NetUsePotionAction)new UsePotionAction(potion, target, true).ToNetAction()).Serialize(writer);
                    payload = writer.Buffer;
                }
                var reader = new PacketReader(); reader.Reset(payload);
                var received = new NetUsePotionAction(); received.Deserialize(reader);
                var action = (UsePotionAction)received.ToGameAction(owner);
                Check(action.OwnerId == 1304 && action.PotionIndex == 0 && action.TargetId == target.CombatId,
                    "Native use packet lost owner/slot/target identity");
                var rng = run.Rng.CombatTargets.Counter;
                Execute(action);
                Check(potion.HasBeenRemovedFromState && players.Take(3).All(p => p.Creature.Block == 0 &&
                    p.Creature.GetPowerAmount<VigorPower>() == 0 && p.Creature.GetPowerAmount<BlurPower>() == 0),
                    "Received potion action did not consume only its owner's potion/effect");
                Check(run.Rng.CombatTargets.Counter == rng && enemies[0].Block == 0 && enemies[2].Block == 0,
                    "Deterministic targeted potion used RNG or modified other enemies");
                string outcome = $"{owner.Creature.Block}/{owner.Creature.GetPowerAmount<VigorPower>()}/{owner.Creature.GetPowerAmount<BlurPower>()}/" +
                    $"{enemies[1].Block}/{enemies[1].GetPowerAmount<PlatingPower>()}/" +
                    $"{enemies[1].GetPowerAmount<PoisonPower>()}/{enemies[1].GetPowerAmount<DoomPower>()}";
                if (firstOutcome == null) firstOutcome = outcome;
                else Check(outcome == firstOutcome, "Native use packet produced different modeled-peer outcomes");
            }
        }
        Console.WriteLine("PASS native use packets: identical owner/slot/combat-target payload applied to two independent four-owner native states; no live clients or sockets.");
    }
    private static void Grave()
    {
        foreach (int count in new[] { 1, 2, 4 })
        foreach (var (poison, doom) in new[] { (0, 0), (0, 7), (9, 0), (9, 3), (3, 9), (5, 5) })
        {
            var (run, combat, players, enemies) = Setup(count); var owner = players[^1]; var target = enemies[1];
            if (poison > 0) Seed<PoisonPower>(target, poison);
            if (doom > 0) Seed<DoomPower>(target, doom);
            int before = CombatManager.Instance.History.Entries.Count();
            Use<GraveDistillate>(owner, target);
            Check(target.GetPowerAmount<PoisonPower>() == doom && target.GetPowerAmount<DoomPower>() == poison,
                $"Snapshot swap failed for {poison}/{doom}, owners={count}");
            Check(target.CurrentHp == 100 && target.Block == 0 && players.All(p => p.Creature.Powers.Count == 0) &&
                enemies.Where(e => e != target).All(e => e.Powers.Count == 0), "Swap damaged or affected untargeted creature");
            var applied = CombatManager.Instance.History.Entries.Skip(before).OfType<PowerReceivedEntry>().ToArray();
            var expected = poison == doom ? Array.Empty<Type>() :
                (doom > 0 ? new[] { typeof(PoisonPower) } : []).Concat(poison > 0 ? [typeof(DoomPower)] : []).ToArray();
            Check(applied.Select(e => e.Power.GetType()).SequenceEqual(expected) && applied.All(e => e.Applier == owner.Creature),
                "Native swap reapplication order/applier changed or equal swap caused hooks");
        }
        foreach (int artifact in new[] { 1, 2 })
        {
            var (_, _, ps, enemies) = Setup(); var target = enemies[1];
            Seed<PoisonPower>(target, 9); Seed<DoomPower>(target, 3); Seed<ArtifactPower>(target, artifact);
            Use<GraveDistillate>(ps[0], target);
            Check(target.GetPowerAmount<PoisonPower>() == 0 && target.GetPowerAmount<DoomPower>() == (artifact == 1 ? 9 : 0) &&
                target.GetPowerAmount<ArtifactPower>() == 0, "Artifact must block Poison first, then Doom normally");
        }
        {
            var (_, _, ps, enemies) = Setup(); var target = enemies[1]; Seed<ArtifactPower>(target, 2);
            Seed<PoisonPower>(target, 5); Seed<DoomPower>(target, 5);
            Use<GraveDistillate>(ps[0], target);
            Check(target.GetPowerAmount<ArtifactPower>() == 2 && target.GetPowerAmount<PoisonPower>() == 5 &&
                target.GetPowerAmount<DoomPower>() == 5, "Equal swap consumed Artifact or re-applied powers");
        }
        {
            var (_, _, ps, enemies) = Setup(); var target = enemies[1]; Seed<DoomPower>(target, 7);
            NativeConsoleFixture.Relic<SneckoSkull>(ps[1]);
            Use<GraveDistillate>(ps[0], target);
            Check(target.GetPowerAmount<PoisonPower>() == 7 && target.GetPowerAmount<DoomPower>() == 0, "Partner Snecko modified potion owner's Poison");
            PowerCmd.Remove<PoisonPower>(target).GetAwaiter().GetResult(); Seed<DoomPower>(target, 7);
            NativeConsoleFixture.Relic<SneckoSkull>(ps[0]);
            Use<GraveDistillate>(ps[0], target);
            Check(target.GetPowerAmount<PoisonPower>() == 8 && target.GetPowerAmount<DoomPower>() == 0, "Own Snecko Skull was bypassed");
        }
        {
            var (_, _, ps, enemies) = Setup(); var target = enemies[1]; Seed<PoisonPower>(target, 9); Seed<DoomPower>(target, 3);
            var immune = Seed<IllusionPower>(target, 1);
            var dataType = typeof(IllusionPower).GetNestedType("Data", BindingFlags.NonPublic)!;
            var data = AccessTools.Method(typeof(PowerModel), "GetInternalData").MakeGenericMethod(dataType).Invoke(immune, null)!;
            AccessTools.Field(dataType, "isReviving").SetValue(data, true);
            Check(!target.CanReceivePowers, "Native reviving-immunity fixture did not block powers");
            Use<GraveDistillate>(ps[0], target);
            Check(target.GetPowerAmount<PoisonPower>() == 9 && target.GetPowerAmount<DoomPower>() == 3,
                "Initially immune target had powers stripped or immunity bypassed");
        }
        DeathBoundaries();
        Console.WriteLine("PASS Grave: snapshot 0/one/both/equal, 1/2/4 owners, fixed native Poison→Doom order, Artifact 1/2, own vs partner Snecko, native reviving immunity and death boundaries.");
    }
    private static void DeathBoundaries()
    {
        {
            var (_, _, ps, enemies) = Setup(); var target = enemies[1]; target.SetCurrentHpInternal(10);
            Seed<PoisonPower>(target, 20); Seed<DoomPower>(target, 3);
            Use<GraveDistillate>(ps[0], target);
            Check(target.IsAlive && target.GetPowerAmount<DoomPower>() == 20, "Swap invented immediate Doom death");
            target.GetPower<DoomPower>()!.BeforeSideTurnEnd(Context, CombatSide.Enemy, [target]).GetAwaiter().GetResult();
            Check(target.IsDead, "Native end-turn Doom threshold no longer kills");
        }
        {
            var (_, combat, ps, enemies) = Setup(); var target = enemies[1]; target.SetCurrentHpInternal(5); Seed<DoomPower>(target, 7);
            Use<GraveDistillate>(ps[0], target);
            Check(target.IsAlive && target.GetPowerAmount<PoisonPower>() == 7, "Swap invented immediate Poison damage");
            target.GetPower<PoisonPower>()!.AfterSideTurnStart(CombatSide.Enemy, [target], combat).GetAwaiter().GetResult();
            Check(target.IsDead, "Native Poison turn-start threshold no longer kills");
        }
        {
            var (_, _, ps, enemies) = Setup(); var target = enemies[1]; var poison = Seed<PoisonPower>(target, 9); Seed<DoomPower>(target, 3);
            poison.Removed += () => target.SetCurrentHpInternal(0);
            Use<GraveDistillate>(ps[0], target);
            Check(target.GetPower<PoisonPower>() == null && target.GetPowerAmount<DoomPower>() == 3,
                "Potion continued past death during a native removal callback");
        }
        {
            var (_, _, ps, enemies) = Setup(); var target = enemies[1]; target.SetCurrentHpInternal(0);
            var potion = Procure<GraveDistillate>(ps[0]); Execute(new UsePotionAction(potion, target, true));
            Check(!potion.HasBeenRemovedFromState, "Native action consumed Grave Distillate for a dead target");
        }
    }
}
