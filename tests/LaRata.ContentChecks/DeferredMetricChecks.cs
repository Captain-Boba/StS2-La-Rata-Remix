using System.Reflection;
using System.Text.Json;
using BaseLib.Patches.Saves;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

internal static class DeferredMetricChecks
{
    private static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }
    private static decimal Value(RelicModel relic, string key) => RelicMetrics.Read(relic).Metrics.Where(m => m.Key == key).Sum(m => m.Value);
    private static decimal Archived(Player owner, int id, string key) => RelicMetrics.ReadRemoved(owner.RunState)
        .Single(r => r.OwnerId == owner.NetId && r.AcquisitionOrdinal == id).Metrics.Where(m => m.Key == key).Sum(m => m.Value);
    private static T Obtain<T>(Player owner) where T : RelicModel, new()
    { var relic = (T)ModelDb.Relic<T>().ToMutable(); owner.AddRelicInternal(relic); return relic; }
    private static void Scoped(RelicModel? relic, Action action)
    { var old = RelicMetricSource.Enter(relic); try { action(); } finally { RelicMetricSource.Restore(old); } }
    private static bool EmptyRewardUi(List<Reward> rewards, ref Task __result)
    { Require(rewards.Count == 0, "Fixture only substitutes an empty reward presentation"); __result = Task.CompletedTask; return false; }
    private static bool PickFirst(ref Task<PlayerChoiceResult> __result)
    { __result = Task.FromResult(PlayerChoiceResult.FromIndex(0)); return false; }
    private static bool NoAlternativeUi(ref IReadOnlyList<CardRewardAlternative> __result)
    { __result = []; return false; }
    private static bool CombatOver;
    private static bool EndingGuard(ref bool __result) { __result = CombatOver; return false; }
    private static TaskCompletionSource? removalGate;
    private static bool DelayRemoval(RelicModel __instance, ref Task __result)
    { if (__instance is not SwordOfStone || removalGate == null) return true; __result = removalGate.Task; return false; }
    private static (Player owner, RunState run) Setup(ulong id)
    {
        var owner = NativeConsoleFixture.Player(id);
        NativeConsoleFixture.Set(owner, nameof(owner.Creature), new Creature(owner, 50, 50));
        NativeConsoleFixture.Set(owner, nameof(owner.RelicGrabBag), new RelicGrabBag());
        var run = RunState.CreateForTest(players: [owner], seed: "DEFERREDMETRICS");
        ((List<List<MapPointHistoryEntry>>)AccessTools.Field(typeof(RunState), "_mapPointHistory").GetValue(run)!).Add([new MapPointHistoryEntry(MapPointType.Monster, run)]);
        return (owner, run);
    }
    private static GoldReward ExtraGold(AmethystAubergine source)
    {
        List<Reward> rewards = [];
        Require(source.TryModifyRewards(source.Owner, rewards,
            new CombatRoom(ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable(), source.Owner.RunState)), "Native Amethyst factory rejected fixture");
        return (GoldReward)rewards.Single();
    }
    internal static void Run(Harmony harmony)
    {
        // Use the installed BaseLib's actual native Reward save/packet hooks.
        foreach (var type in typeof(ExtendedSavePatches).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
            .Where(t => t.Name is "PrepExtendedRewardData" or "LoadExtendedRewardData" or "SerializeExtendedRewardData" or "DeserializeExtendedRewardData"))
            harmony.CreateClassProcessor(type).Patch();
        ModelIdSerializationCache.Init();
        // Explicitly supply the post-mod-init gameplay-enabled condition; no engine init.
        var init = typeof(ExtendedSavePatches).Assembly.GetType("BaseLib.Patches.PostModInitPatch")!;
        var canModify = AccessTools.Property(init, "CanModifyGameplay");
        var oldCanModify = canModify.GetValue(null);
        canModify.SetValue(null, true);
        var oldLocal = LocalContext.NetId;
        LocalContext.NetId = null; // No local desktop progression/VFX; real reward mutations remain enabled.
        harmony.Patch(AccessTools.Method(typeof(RewardsCmd), nameof(RewardsCmd.OfferCustom)), prefix: new(typeof(DeferredMetricChecks), nameof(EmptyRewardUi)));
        harmony.Patch(AccessTools.Method(typeof(PlayerChoiceSynchronizer), nameof(PlayerChoiceSynchronizer.WaitForRemoteChoice)), prefix: new(typeof(DeferredMetricChecks), nameof(PickFirst)));
        harmony.Patch(AccessTools.Method(typeof(CardRewardAlternative), nameof(CardRewardAlternative.Generate)), prefix: new(typeof(DeferredMetricChecks), nameof(NoAlternativeUi)));
        harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsOverOrEnding)), prefix: new(typeof(DeferredMetricChecks), nameof(EndingGuard)));
        harmony.Patch(AccessTools.Method(typeof(RelicModel), nameof(RelicModel.AfterRemoved)), prefix: new(typeof(DeferredMetricChecks), nameof(DelayRemoval)));
        List<Exception> failures = [];
        try
        {
            foreach (var (name, action) in new (string, Action)[] {
                ("native reward construction/selection", Rewards), ("rest option provenance/regeneration", RestOptions),
                ("native action boundaries", Actions), ("in-flight self replacement", Replacement),
                ("native reward JSON/packet and removed-source restoration", Persistence) })
            {
                try { action(); Console.WriteLine("PASS: " + name); }
                catch (Exception error) { Console.WriteLine("FAIL: " + name + ": " + error); failures.Add(error); }
            }
        }
        finally { LocalContext.NetId = oldLocal; canModify.SetValue(null, oldCanModify); }
        if (failures.Count != 0) throw new AggregateException("Deferred metric checks failed", failures);
        Console.WriteLine("PASS: targeted deferred/native-boundary checks. Native saves are payload roundtrips, not a complete game save; reward selection runs locally, no transport or live UI.");
    }
    private static void Rewards()
    {
        var (owner, run) = Setup(201);
        var source = Obtain<AmethystAubergine>(owner);
        var reward = ExtraGold(source);
        Require(RelicDeferredMetrics.RewardSource.Get(reward) == RelicMetrics.Acquisition.Get(source), "Native factory did not tag reward source");
        reward.OnSkipped();
        Require(Value(source, "gold_gained") == 0, "Offer or skip counted gold");
        int before = owner.Gold;
        Require(reward.SelectUnsynchronized().GetAwaiter().GetResult() && owner.Gold - before == 15 && Value(source, "gold_gained") == 15,
            "Actual reward gain not attributed exactly once");
        Scoped(source, () => new GoldReward(7, owner).SelectUnsynchronized().GetAwaiter().GetResult());
        Require(Value(source, "gold_gained") == 22, "Nested tagged reward source lost");
        var unrelated = new GoldReward(3, owner);
        Scoped(source, () => unrelated.SelectUnsynchronized().GetAwaiter().GetResult());
        Require(Value(source, "gold_gained") == 22, "Untagged nested reward inherited source");
        PotionReward? potion = null;
        Scoped(source, () => potion = new PotionReward(ModelDb.Potion<BlockPotion>().ToMutable(), owner));
        Require(!potion!.SelectUnsynchronized().GetAwaiter().GetResult() && Value(source, "potions_added") == 0,
            "Full potion bar counted an unreceived potion");
        owner.AddToMaxPotionCount(1);
        Require(potion.SelectUnsynchronized().GetAwaiter().GetResult() && owner.Potions.Count() == 1 && Value(source, "potions_added") == 1,
            "Successful native potion receipt not counted");
        var blackStar = Obtain<BlackStar>(owner);
        List<Reward> relicRewards = [];
        Require(blackStar.TryModifyRewards(owner, relicRewards, new CombatRoom(ModelDb.Encounter<EntomancerElite>().ToMutable(), run)), "BlackStar fixture room rejected");
        var relicReward = (RelicReward)relicRewards.Single();
        AccessTools.Field(typeof(RelicReward), "_relic").SetValue(relicReward, ModelDb.Relic<Anchor>().ToMutable());
        Require(Value(blackStar, "relics_gained") == 0, "BlackStar offer counts as obtained relic");
        Require(relicReward.SelectUnsynchronized().GetAwaiter().GetResult() && owner.Relics.OfType<Anchor>().Any() && Value(blackStar, "relics_gained") == 1,
            "BlackStar actual relic receipt not counted");
        using var synchronizer = new PlayerChoiceSynchronizer(new NetSingleplayerGameService(), run);
        var manager = RunManager.Instance; var oldSync = manager.PlayerChoiceSynchronizer;
        NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), synchronizer);
        try
        {
            var wheel = Obtain<PrayerWheel>(owner); List<Reward> cardRewards = [];
            Require(wheel.TryModifyRewards(owner, cardRewards, new CombatRoom(ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable(), run)), "PrayerWheel fixture rejected");
            var cardReward = (CardReward)cardRewards.Single();
            var card = run.CreateCard<StrikeIronclad>(owner);
            ((List<CardCreationResult>)AccessTools.Field(typeof(CardReward), "_cards").GetValue(cardReward)!).Add(new(card));
            Require(Value(wheel, "cards_added") == 0, "PrayerWheel offer counted as acquired card");
            Require(cardReward.SelectUnsynchronized().GetAwaiter().GetResult() && owner.Deck.Cards.Contains(card) && Value(wheel, "cards_added") == 1,
                "PrayerWheel native chosen-card gain not counted");
            foreach (var item in new Reward[] { cardReward, relicReward }) CheckRewardPayload(item);
        }
        finally { NativeConsoleFixture.Set(manager, nameof(manager.PlayerChoiceSynchronizer), oldSync); }
    }
    private static void Persistence()
    {
        var (owner, run) = Setup(202);
        var source = Obtain<AmethystAubergine>(owner); int ordinal = RelicMetrics.Acquisition.Get(source);
        var reward = ExtraGold(source);
        var loaded = CheckRewardPayload(reward);
        owner.RemoveRelicInternal(source);
        var playerPayload = new SerializablePlayer(); ExtendedSaveHandlers<Player, SerializablePlayer>.ExtendedData[playerPayload] = new(owner);
        var runPayload = new SerializableRun(); ExtendedSaveHandlers<IRunState, SerializableRun>.ExtendedData[runPayload] = new(run);
        string pjson = JsonSerializer.Serialize(playerPayload, JsonSerializationUtility.GetTypeInfo<SerializablePlayer>());
        string rjson = JsonSerializer.Serialize(runPayload, JsonSerializationUtility.GetTypeInfo<SerializableRun>());
        var future = Obtain<Anchor>(owner); RelicMetrics.Archive(future);
        ExtendedSaveHandlers<Player, SerializablePlayer>.Load(JsonSerializer.Deserialize(pjson, JsonSerializationUtility.GetTypeInfo<SerializablePlayer>())!, owner);
        ExtendedSaveHandlers<IRunState, SerializableRun>.Load(JsonSerializer.Deserialize(rjson, JsonSerializationUtility.GetTypeInfo<SerializableRun>())!, run);
        owner.RemoveRelicInternal(future, silent: true);
        var reacquired = Obtain<AmethystAubergine>(owner);
        Require(RelicMetrics.Acquisition.Get(reacquired) == ordinal + 1, "Owner sequence restore failed");
        Require(loaded.SelectUnsynchronized().GetAwaiter().GetResult() && Archived(owner, ordinal, "gold_gained") == 15 && Value(reacquired, "gold_gained") == 0,
            "Removed-source reward lost or redirected to reacquired relic after payload restoration");
    }
    private static Reward CheckRewardPayload(Reward reward)
    {
        int ordinal = RelicDeferredMetrics.RewardSource.Get(reward);
        Require(ordinal > 0, "Reward fixture has no origin");
        var save = reward.ToSerializable();
        string json = JsonSerializer.Serialize(save, JsonSerializationUtility.GetTypeInfo<SerializableReward>());
        var loaded = Reward.FromSerializable(JsonSerializer.Deserialize(json, JsonSerializationUtility.GetTypeInfo<SerializableReward>())!, reward.Player);
        Require(RelicDeferredMetrics.RewardSource.Get(loaded) == ordinal,
            reward.GetType().Name + ".ToSerializable -> native JSON -> FromSerializable loses source ordinal; JSON=" + json);
        var writer = new PacketWriter(); save.Serialize(writer);
        var reader = new PacketReader(); reader.Reset(writer.Buffer);
        var packet = new SerializableReward(); packet.Deserialize(reader);
        var decoded = Reward.FromSerializable(packet, reward.Player);
        Require(reader.BitPosition == writer.BitPosition && RelicDeferredMetrics.RewardSource.Get(decoded) == ordinal,
            $"Native reward packet loses source or leaves unread bits: ordinal={RelicDeferredMetrics.RewardSource.Get(decoded)}/{ordinal}, bits={reader.BitPosition}/{writer.BitPosition}");
        return loaded;
    }
    private static void RestOptions()
    {
        var (owner, _) = Setup(203); var girya = Obtain<Girya>(owner);
        var options = RestSiteOption.Generate(owner);
        Require(Value(girya, "rest_actions_completed") == 0, "Option construction counted completion");
        Require(options.OfType<LiftRestSiteOption>().Single().OnSelect().GetAwaiter().GetResult() && girya.TimesLifted == 1 && Value(girya, "rest_actions_completed") == 1,
            "Native Lift completion not measured once");
        var save = girya.ToSerializable();
        var loaded = (Girya)RelicModel.FromSerializable(JsonSerializer.Deserialize(JsonSerializer.Serialize(save, JsonSerializationUtility.GetTypeInfo<SerializableRelic>()), JsonSerializationUtility.GetTypeInfo<SerializableRelic>())!);
        owner.RemoveRelicInternal(girya, silent: true); owner.AddRelicInternal(loaded, silent: true);
        Require(RestSiteOption.Generate(owner).OfType<LiftRestSiteOption>().Single().OnSelect().GetAwaiter().GetResult() && loaded.TimesLifted == 2 && Value(loaded, "rest_actions_completed") == 2,
            "Regenerated option after relic JSON loses provenance");
        var ordinary = new HealRestSiteOption(owner);
        AccessTools.PropertySetter(typeof(Creature), nameof(Creature.CurrentHp)).Invoke(owner.Creature, [30]);
        Scoped(loaded, () => ordinary.OnSelect().GetAwaiter().GetResult());
        Require(owner.Creature.CurrentHp == 45 && Value(loaded, "rest_actions_completed") == 2 && Value(loaded, "hp_restored") == 0, "Ordinary rest inherited unrelated source");
    }
    private static void Actions()
    {
        var (owner, run) = Setup(204);
        var combat = new CombatState(ModelDb.Encounter<TwoTailedRatsNormal>().ToMutable(), run);
        owner.Creature.CombatState = combat; combat.AddCreature(owner.Creature); owner.ResetCombatState();
        var source = Obtain<Anchor>(owner);
        var blade = run.CreateCard<SovereignBlade>(owner); owner.PlayerCombatState!.Hand.AddInternal(blade);
        Scoped(source, () => { blade.AddDamage(3.5m); blade.AddDamage(0); blade.AddDamage(-1); });
        Require(Value(source, "blade_base_damage_adjustment") == 2.5m && blade.DynamicVars.Damage.BaseValue == 12.5m, "Forge base delta differs");
        CardPlay Play(bool automatic) => new() { Card = blade, Target = null, ResultPile = PileType.Discard, Resources = default, IsAutoPlay = automatic, PlayIndex = 0, PlayCount = 1 };
        Scoped(source, () => { CombatManager.Instance.History.CardPlayFinished(combat, Play(false)); CombatManager.Instance.History.CardPlayFinished(combat, Play(true)); });
        Require(Value(source, "auto_plays_completed") == 1, "Auto-play history completion counts a manual play");
        var pyramid = Obtain<RunicPyramid>(owner);
        var strike = run.CreateCard<StrikeIronclad>(owner); owner.PlayerCombatState.Hand.AddInternal(strike);
        Hook.AfterFlush(combat, owner, new ThrowingPlayerChoiceContext(), [], [strike, blade]).GetAwaiter().GetResult();
        Require(Value(pyramid, "cards_retained") == 1, "Pyramid counts independently retained blade or misses native hand card");
        Obtain<RingingTriangle>(owner);
        Hook.AfterFlush(combat, owner, new ThrowingPlayerChoiceContext(), [], [strike, blade]).GetAwaiter().GetResult();
        Require(Value(pyramid, "cards_retained") == 1, "Ambiguous flush prevention split arbitrarily");
        var orb = ModelDb.Orb<DarkOrb>().ToMutable(); orb.Owner = owner;
        CombatOver = false;
        Scoped(source, () => OrbCmd.Passive(new ThrowingPlayerChoiceContext(), orb, null).GetAwaiter().GetResult());
        Require(orb.EvokeVal == 12 && Value(source, "orb_passives_completed") == 1, "Native extra Dark passive not measured once");
        bool rejected = false;
        try { Scoped(source, () => OrbCmd.Passive(new ThrowingPlayerChoiceContext(), orb, owner.Creature).GetAwaiter().GetResult()); }
        catch (InvalidOperationException) { rejected = true; }
        Require(rejected && Value(source, "orb_passives_completed") == 1, "Failed orb passive counted or exception swallowed");
        CombatOver = true;
        Scoped(source, () => OrbCmd.Passive(new ThrowingPlayerChoiceContext(), orb, null).GetAwaiter().GetResult());
        Require(orb.EvokeVal == 12 && Value(source, "orb_passives_completed") == 1, "Combat-ended no-op counted");
        CombatOver = false;
    }
    private static void Replacement()
    {
        var (owner, run) = Setup(205);
        var sword = Obtain<SwordOfStone>(owner); sword.ElitesDefeated = 4;
        int ordinal = RelicMetrics.Acquisition.Get(sword);
        removalGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var task = sword.AfterCombatVictory(new CombatRoom(ModelDb.Encounter<EntomancerElite>().ToMutable(), run));
            Require(!task.IsCompleted && sword.HasBeenRemovedFromState && RelicMetricSource.Attribution == null, "Replacement did not suspend after native removal or leaked context");
            removalGate.SetResult(); task.GetAwaiter().GetResult();
            Require(owner.Relics.OfType<SwordOfJade>().Count() == 1 && Archived(owner, ordinal, "relics_removed") == 1 && Archived(owner, ordinal, "relics_gained") == 1,
                "Suspended native self replacement loses old source after removal");
        }
        finally { removalGate = null; }
        var old = Obtain<OldCoin>(owner); int oldId = RelicMetrics.Acquisition.Get(old);
        owner.RemoveRelicInternal(old);
        old.AfterObtained().GetAwaiter().GetResult();
        Require(Archived(owner, oldId, "gold_gained") == 0, "New hook on a removed source starts attribution");
    }
}
