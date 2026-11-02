using System.Reflection;
using System.Text.Json;
using BaseLib.Patches.Saves;
using BaseLib.Utils;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game.Sync;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;

// Own switch: --content-wave-relics (Program.cs registration belongs to K1/K2).
// Uses real commands/packets with the existing desktop-free native-model fixture.
internal static class ContentWaveRelicChecks
{
    private static int assertions;
    private static readonly PlayerChoiceContext Context = new BlockingPlayerChoiceContext();
    private static readonly List<RewardObtainedMessage> Rewards = [];
    private static readonly List<int> Debits = [];
    private static bool RejectCard;
    private static TaskCompletionSource? PurchaseGate;
    private static void Check(bool value, string why)
    { assertions++; if (!value) throw new Exception(why); }
    private static bool Active(ref bool __result) { __result = false; return false; }
    private static bool Progress(ref bool __result)
    { __result = RunManager.Instance.DebugOnlyGetState()?.CurrentRoom is not MerchantRoom; return false; }
    private static bool ObserveReward(RewardObtainedMessage message) { Rewards.Add(message); return false; }
    private static bool ObserveGold(GoldLostMessage message) { Debits.Add(message.goldLost); return false; }
    private static bool CanAdd(CardModel card, ref AbstractModel? preventer, ref bool __result)
    { if (!RejectCard) return true; preventer = card.Owner.Relics[0]; __result = false; return false; }
    private static bool PauseDeckAdd(CardModel card, ref Task __result)
    {
        if (PurchaseGate is not { } gate || card.Pile?.Type != PileType.Deck) return true;
        PurchaseGate = null; __result = gate.Task; return false;
    }
    private static T Add<T>(Player owner) where T : RelicModel, new()
    { var r = (T)ModelDb.Relic<T>().ToMutable(); owner.AddRelicInternal(r); return r; }
    private static (RunState run, Player a, Player b, Creature[] enemies) Setup()
    {
        var result = ((RunState, Player, Player, Creature[]))AccessTools.Method(typeof(NightRelicChecks), "Setup").Invoke(null, null)!;
        foreach (var enemy in result.Item4) { enemy.SetMaxHpInternal(100); enemy.SetCurrentHpInternal(100); }
        return result;
    }
    private static T Card<T>(Player owner) where T : CardModel, new() =>
        (T)owner.Creature.CombatState!.CreateCard(ModelDb.Card<T>(), owner);
    private static T Restore<T>(T relic, Player owner) where T : RelicModel
    {
        var json = JsonSerializer.Serialize(relic.ToSerializable(), JsonSerializationUtility.GetTypeInfo<SerializableRelic>());
        var result = (T)RelicModel.FromSerializable(JsonSerializer.Deserialize(json,
            JsonSerializationUtility.GetTypeInfo<SerializableRelic>())!);
        result.Owner = owner;
        return result;
    }

    internal static void Run()
    {
        assertions = 0;
        TestMode.IsOn = true;
        var harmony = new Harmony("LaRata.ContentChecks.ContentWaveRelics");
        var oldRun = RunManager.Instance.DebugOnlyGetState();
        var oldAscension = RunManager.Instance.AscensionManager;
        var state = AccessTools.Field(typeof(CombatManager), "_state");
        var oldState = state.GetValue(CombatManager.Instance);
        var oldSync = RunManager.Instance.RewardSynchronizer;
        var oldNet = RunManager.Instance.NetService;
        try
        {
            NativeConsoleFixture.PatchDesktop(harmony);
            harmony.CreateClassProcessor(typeof(BaseLib.Patches.Content.PrefixIdPatch)).Patch();
            foreach (var type in typeof(AbstractModel).Assembly.GetTypes().Concat(typeof(ModEntry).Assembly.GetTypes())
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(AbstractModel)) && t.GetConstructor(Type.EmptyTypes) != null))
            {
                ModelDb.Inject(type);
                if (type.Assembly == typeof(ModEntry).Assembly) SavedPropertiesTypeCache.InjectTypeIntoCache(type);
            }
            RelicMetrics.Register();
            ModEntry.ApplyPatches(harmony);
            var saved = typeof(SavedSpireField<,>).Assembly.GetType("BaseLib.Patches.Utils.SavedSpireFieldPatch")!;
            harmony.CreateClassProcessor(saved).Patch();
            harmony.CreateClassProcessor(typeof(ExtendedSaveTypes)).Patch();
            ExtendedSavePatches.Patch(harmony);
            AccessTools.Method(saved, "AddFieldsSorted").Invoke(null, null);
            ModelIdSerializationCache.Init();
            harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsInProgress)),
                prefix: new(typeof(ContentWaveRelicChecks), nameof(Progress)));
            harmony.Patch(AccessTools.PropertyGetter(typeof(CombatManager), nameof(CombatManager.IsOverOrEnding)),
                prefix: new(typeof(ContentWaveRelicChecks), nameof(Active)));
            RelicMetricsDisabledChecks.CheckPatchSet(harmony);

            // Reuse the independent pre-existing acceptance suite, now with ALL
            // statistics categories still OFF, including native costs/X/caps,
            // owner isolation, exceptions, nested callbacks and modifier awaits.
            AccessTools.Method(typeof(FollowupCardChecks), "Reset").Invoke(null, null);
            AccessTools.Method(typeof(FollowupCardChecks), "ResetAwaitBoundaries").Invoke(null, [harmony]);
            ResetEndedPlayer();
            Seal(); Testament(); Tooth(); Scavenger(); Ticket(harmony); Stairwell();
            Check(ModelDb.Relic<RelicRemover>().Rarity == MegaCrit.Sts2.Core.Entities.Relics.RelicRarity.Uncommon,
                "Relic Remover must be Uncommon");
            foreach (var type in new[] { typeof(CounterfeitSeal), typeof(PlagueTestament), typeof(UsurpersTooth) })
                Check(ModelDb.RelicPool<SharedRelicPool>().AllRelics.Count(r => r.GetType() == type) == 1,
                    "Missing/duplicated shared relic: " + type.Name);
            Console.WriteLine($"PASS: content-wave relics {assertions} assertions plus existing Reset/await contract; statistics OFF. Native commands/payloads, supplied combat/desktop boundaries; no live UI, clients, disk-save or install claim.");
        }
        finally
        {
            RejectCard = false;
            NativeConsoleFixture.Set(RunManager.Instance, "State", oldRun);
            NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.AscensionManager), oldAscension);
            NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.RewardSynchronizer), oldSync);
            NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.NetService), oldNet);
            state.SetValue(CombatManager.Instance, oldState);
            harmony.UnpatchAll(harmony.Id);
        }
    }

    private static CardPlay Play(CardModel card, int energy, bool auto = false, int index = 0) => new()
    {
        Card = card, Target = null, ResultPile = PileType.Discard,
        Resources = new() { EnergySpent = energy, EnergyValue = energy, StarsSpent = 0, StarValue = 0 },
        IsAutoPlay = auto, PlayIndex = index, PlayCount = index + 1
    };
    private static T Hand<T>(Player player) where T : CardModel, new()
    { var card = Card<T>(player); player.PlayerCombatState!.Hand.AddInternal(card); return card; }
    private static void StartTurn(ICombatState combat, CombatSide side, IReadOnlyList<Creature> participants)
    {
        var previous = LocalContext.NetId; LocalContext.NetId = combat.Players[0].NetId;
        try { Hook.BeforeSideTurnStart(combat, side, participants).GetAwaiter().GetResult(); }
        finally { LocalContext.NetId = previous; }
    }

    private static void Stairwell()
    {
        int beforeAssertions = assertions;
        {
            var (_, a, b, _) = Setup(); var key = Add<StairwellKey>(a); var otherKey = Add<StairwellKey>(b);
            var first = Hand<Bash>(a); var next = Hand<Bash>(a); var foreign = Hand<Bash>(b);
            int updates = 0; next.EnergyCostChanged += () => updates++;
            for (int i = 0; i < 5; i++) Check(first.EnergyCost.GetAmountToSpend() == 1, "First card preview not discounted");
            Check(!key.TriggeredThisTurn && Restore(key, a).TriggeredThisTurn == false, "Preview consumed or changed saved availability");
            var payment = first.SpendResources().GetAwaiter().GetResult();
            Check(payment.Item1 == 1 && a.PlayerCombatState!.Energy == 9 && !key.TriggeredThisTurn,
                "Real payment differs from preview or used the old Energy payout");
            CardPileCmd.Add(first, PileType.Play, skipVisuals: true).GetAwaiter().GetResult();
            Hook.BeforeCardPlayed(a.Creature.CombatState!, Play(first, payment.Item1)).GetAwaiter().GetResult();
            Check(key.TriggeredThisTurn && !otherKey.TriggeredThisTurn && next.EnergyCost.GetAmountToSpend() == 2 &&
                foreign.EnergyCost.GetAmountToSpend() == 1 && updates == 1, "Consumption/owner/UI cost notification mismatch");
            Check(Restore(key, a).TriggeredThisTurn, "Used first-card state lost in native JSON");
            int energy = a.PlayerCombatState!.Energy, cards = a.PlayerCombatState.Hand.Cards.Count;
            foreach (int paid in new[] { 2, 1, 0 })
                Hook.AfterCardPlayed(a.Creature.CombatState!, Context, Play(first, paid)).GetAwaiter().GetResult();
            Check(a.PlayerCombatState.Energy == energy && a.PlayerCombatState.Hand.Cards.Count == cards,
                "Old descending-cost Energy/Draw effect remains");
            StartTurn(a.Creature.CombatState!, CombatSide.Player, [a.Creature, b.Creature]);
            Check(!key.TriggeredThisTurn && next.EnergyCost.GetAmountToSpend() == 1 && updates == 2,
                "New turn failed to restore discount and notify native cost UI");
            Hook.BeforeCardPlayed(a.Creature.CombatState!, Play(foreign, 1)).GetAwaiter().GetResult();
            Check(!key.TriggeredThisTurn && otherKey.TriggeredThisTurn, "Foreign card consumed another owner's turn");
            Hook.BeforeCardPlayed(a.Creature.CombatState!, Play(next, 0, index: 1)).GetAwaiter().GetResult();
            Check(!key.TriggeredThisTurn, "A replay callback consumed an unrelated first play");
            foreach (bool used in new[] { false, true })
            {
                var legacy = key.ToSerializable();
                legacy.Props = new SavedProperties
                {
                    ints = [new("LastEnergySpent", 1), new("DescendingCount", 2)],
                    bools = [new(nameof(StairwellKey.TriggeredThisTurn), used)]
                };
                string oldJson = JsonSerializer.Serialize(legacy, JsonSerializationUtility.GetTypeInfo<SerializableRelic>());
                var loaded = (StairwellKey)RelicModel.FromSerializable(JsonSerializer.Deserialize(oldJson,
                    JsonSerializationUtility.GetTypeInfo<SerializableRelic>())!);
                loaded.Owner = a;
                Check(loaded.TriggeredThisTurn == used, "Native legacy relic JSON did not preserve first-play availability");
                string newJson = JsonSerializer.Serialize(loaded.ToSerializable(), JsonSerializationUtility.GetTypeInfo<SerializableRelic>());
                Check(!newJson.Contains("DescendingCount") && !newJson.Contains("LastEnergySpent"), "Old chain fields survived redesign serialization");
            }
        }
        foreach (bool auto in new[] { false, true })
        {
            var (_, a, _, _) = Setup(); var key = Add<StairwellKey>(a);
            var zero = Hand<Bash>(a); zero.EnergyCost.SetUntilPlayed(0); var next = Hand<Bash>(a);
            int beforeEnergy = a.PlayerCombatState!.Energy;
            Check(zero.EnergyCost.GetAmountToSpend() == 0 && !key.TriggeredThisTurn, "Zero cost became negative or preview consumed first play");
            if (!auto) zero.SpendResources().GetAwaiter().GetResult();
            Hook.BeforeCardPlayed(a.Creature.CombatState!, Play(zero, 0, auto)).GetAwaiter().GetResult();
            Check(key.TriggeredThisTurn && a.PlayerCombatState.Energy == beforeEnergy && next.EnergyCost.GetAmountToSpend() == 2,
                "Zero-cost/auto first play deferred discount to a later card");
            StartTurn(a.Creature.CombatState!, CombatSide.Enemy, a.Creature.CombatState!.Enemies);
            Check(!key.TriggeredThisTurn, "Enemy-side turn did not reset each-turn first play");
        }
        foreach (int energy in new[] { 0, 5 })
        {
            var (_, a, _, _) = Setup(); var key = Add<StairwellKey>(a); var x = Hand<Whirlwind>(a); var next = Hand<Bash>(a);
            a.PlayerCombatState!.Energy = energy;
            Check(x.EnergyCost.CostsX && x.EnergyCost.GetAmountToSpend() == energy, "Native X cost was rewritten");
            var paid = x.SpendResources().GetAwaiter().GetResult();
            Check(paid.Item1 == energy && a.PlayerCombatState.Energy == 0 && x.EnergyCost.CapturedXValue == energy,
                "Native X debit/captured effect value changed");
            Hook.BeforeCardPlayed(a.Creature.CombatState!, Play(x, paid.Item1)).GetAwaiter().GetResult();
            Check(key.TriggeredThisTurn && next.EnergyCost.GetAmountToSpend() == 2, "First X card deferred discount");
        }
        {
            var (_, a, _, _) = Setup(); var key = Add<StairwellKey>(a); var star = Hand<Stardust>(a); var next = Hand<Bash>(a);
            a.PlayerCombatState!.Stars = 5;
            var paid = star.SpendResources().GetAwaiter().GetResult();
            Check(paid.Item2 == 5 && a.PlayerCombatState.Stars == 0, "Energy discount altered native Stars-X payment");
            Hook.BeforeCardPlayed(a.Creature.CombatState!, Play(star, paid.Item1)).GetAwaiter().GetResult();
            Check(key.TriggeredThisTurn && next.EnergyCost.GetAmountToSpend() == 2, "Stars-X first card deferred discount");
        }
        foreach (bool melt in new[] { false, true })
        {
            var (_, a, _, _) = Setup(); var key = Add<StairwellKey>(a); var card = Hand<Bash>(a);
            int updates = 0; card.EnergyCostChanged += () => updates++;
            if (melt) { key.IsWax = true; RelicCmd.Melt(key).GetAwaiter().GetResult(); }
            else RelicCmd.Remove(key).GetAwaiter().GetResult();
            Check(card.EnergyCost.GetAmountToSpend() == 2 && updates == 1, "Remove/Melt retained stale first-card cost");
        }
        Console.WriteLine($"PASS Stairwell revision: {assertions - beforeAssertions} assertions; native cost preview/debit/signals, own turn state, 0-cost/auto/X, nested-play reservation boundary, JSON/legacy fields, Remove/Melt. Card-play hook boundary supplied; no live UI/client claim.");
    }

    private static void ResetEndedPlayer()
    {
        var (_, a, b, _) = Setup(); var combat = a.Creature.CombatState!;
        var paid = Card<Bash>(b); b.PlayerCombatState!.Hand.AddInternal(paid);
        paid.SpendResources().GetAwaiter().GetResult();
        var ended = (HashSet<Player>)AccessTools.Field(typeof(CombatManager), "_playersReadyToEndTurn").GetValue(CombatManager.Instance)!;
        ended.Add(b);
        try
        {
            ResetEnergyLedger.Refund(combat).GetAwaiter().GetResult();
            Check(b.PlayerCombatState!.Energy == 10 && ended.Contains(b), "Ended player refund missing or end-turn status changed");
            ResetEnergyLedger.Refund(combat).GetAwaiter().GetResult();
            Check(b.PlayerCombatState.Energy == 10, "Repeat refund granted energy twice");
        }
        finally { ended.Remove(b); }
        a.PlayerCombatState!.LoseEnergy(1);
        Check(ResetEnergyLedger.Pending(combat).Values.All(x => x == 0), "Non-card loss recorded as payment");
    }

    private static void Seal()
    {
        var (_, a, b, _) = Setup(); var seal = Add<CounterfeitSeal>(a); var other = Add<CounterfeitSeal>(b);
        var moved = Card<DefendIronclad>(a); a.PlayerCombatState!.DiscardPile.AddInternal(moved);
        CardPileCmd.Add(moved, PileType.Hand, skipVisuals: true).GetAwaiter().GetResult();
        Check(!seal.TriggeredThisTurn, "Moving/drawing consumed generated-card upgrade");
        var made = Card<Bash>(a);
        CardPileCmd.AddGeneratedCardToCombat(made, PileType.Hand, a).GetAwaiter().GetResult();
        Check(made.IsUpgraded && seal.TriggeredThisTurn && !other.TriggeredThisTurn, "Generation upgrade/owner isolation failed");
        var second = Card<Bash>(a);
        CardPileCmd.AddGeneratedCardToCombat(second, PileType.Hand, a).GetAwaiter().GetResult();
        Check(!second.IsUpgraded, "Second creation upgraded");
        Check(Restore(seal, a).TriggeredThisTurn, "Seal usage lost in JSON");
        seal.BeforeSideTurnStart(Context, CombatSide.Enemy, [], a.Creature.CombatState!).GetAwaiter().GetResult();
        var unavailable = Card<Bash>(a); CardCmd.Upgrade(unavailable);
        CardPileCmd.AddGeneratedCardToCombat(unavailable, PileType.Hand, a).GetAwaiter().GetResult();
        var next = Card<Bash>(a); CardPileCmd.AddGeneratedCardToCombat(next, PileType.Hand, a).GetAwaiter().GetResult();
        Check(seal.TriggeredThisTurn && !next.IsUpgraded, "Unupgradable first creation secretly deferred trigger");
    }

    private static void Testament()
    {
        var (_, a, b, enemies) = Setup(); var one = Add<PlagueTestament>(a); Add<PlagueTestament>(b);
        var previousLocal = LocalContext.NetId;
        LocalContext.NetId = a.NetId;
        try
        {
        var dead = enemies[0];
        ModelDb.Power<PoisonPower>().ToMutable().ApplyInternal(dead, 7, true);
        // Actual native kill executes BeforeDeath, confirmed AfterDeath, and only
        // then removes the enemy/powers. Both owners must snapshot before removal.
        CreatureCmd.Kill(dead, force: true).GetAwaiter().GetResult();
        Check(enemies.Skip(1).Sum(e => e.GetPowerAmount<PoisonPower>()) == 14, "Poison lost/doubled per owner on actual death");
        int sum = enemies.Skip(1).Sum(e => e.GetPowerAmount<PoisonPower>());
        one.AfterDeath(Context, dead, false, 0).GetAwaiter().GetResult();
        Check(enemies.Skip(1).Sum(e => e.GetPowerAmount<PoisonPower>()) == sum, "Repeated death hook transferred twice");
        one.BeforeDeath(enemies[1]).GetAwaiter().GetResult();
        one.AfterDeath(Context, enemies[1], true, 0).GetAwaiter().GetResult();
        Check(enemies.Skip(1).Sum(e => e.GetPowerAmount<PoisonPower>()) == sum, "Prevented death transferred Poison");
        // Mutable clones must not share another holder's pending death snapshots.
        one.BeforeDeath(enemies[1]).GetAwaiter().GetResult();
        var isolated = (PlagueTestament)ModelDb.Relic<PlagueTestament>().ToMutable(); isolated.Owner = a;
        int targetPoison = enemies[2].GetPowerAmount<PoisonPower>();
        enemies[1].SetCurrentHpInternal(0);
        isolated.AfterDeath(Context, enemies[1], false, 0).GetAwaiter().GetResult();
        Check(enemies[2].GetPowerAmount<PoisonPower>() == targetPoison, "Pending poison snapshot leaked through canonical clone");
        }
        finally { LocalContext.NetId = previousLocal; }
    }

    private static void Tooth()
    {
        var (_, a, b, enemies) = Setup(); Add<UsurpersTooth>(a); Add<UsurpersTooth>(b);
        PowerCmd.Apply<StrengthPower>(Context, enemies[0], 3, enemies[0], null).GetAwaiter().GetResult();
        Check(a.Creature.GetPowerAmount<VigorPower>() == 2 && b.Creature.GetPowerAmount<VigorPower>() == 2,
            "Enemy Strength gain must grant 2 Vigor to EACH relic holder");
        PowerCmd.Apply<StrengthPower>(Context, a.Creature, 2, a.Creature, null).GetAwaiter().GetResult();
        PowerCmd.Apply<StrengthPower>(Context, enemies[0], -1, null, null).GetAwaiter().GetResult();
        Check(a.Creature.GetPowerAmount<VigorPower>() == 2, "Player gain or enemy reduction triggered tooth");
        PowerCmd.Apply<StrengthPower>(Context, enemies[1], 0.5m, null, null).GetAwaiter().GetResult();
        Check(a.Creature.GetPowerAmount<VigorPower>() == 2, "Fractional zero Strength triggered tooth");
        PowerCmd.Apply<StrengthPower>(Context, enemies[0], 1, null, null).GetAwaiter().GetResult();
        Check(a.Creature.GetPowerAmount<VigorPower>() == 4, "Tooth gained an undocumented per-turn cap");
        enemies[0].GetPower<StrengthPower>()!.SetAmount(999999999, true);
        PowerCmd.Apply<StrengthPower>(Context, enemies[0], 1, null, null).GetAwaiter().GetResult();
        Check(a.Creature.GetPowerAmount<VigorPower>() == 4, "Capped Strength without actual gain triggered tooth");
        enemies[0].GetPower<StrengthPower>()!.SetAmount(-5, true);
        PowerCmd.Apply<StrengthPower>(Context, enemies[0], 1, null, null).GetAwaiter().GetResult();
        Check(a.Creature.GetPowerAmount<VigorPower>() == 6, "Actual negative-to-less-negative Strength gain was ignored");
    }

    private static void Scavenger()
    {
        var (_, a, _, enemies) = Setup(); var relic = Add<BrassScavenger>(a); int gold = a.Gold;
        enemies[0].GainBlockInternal(25);
        CreatureCmd.Damage(Context, enemies[0], 30, ValueProp.Unpowered, a.Creature, Card<Bash>(a)).GetAwaiter().GetResult();
        Check(relic.UsedThisCombat && a.Gold == gold + 25, "Scavenger still caps Gold at 12 or wrong actual Block damage");
        enemies[1].GainBlockInternal(20);
        CreatureCmd.Damage(Context, enemies[1], 30, ValueProp.Unpowered, a.Creature, Card<Bash>(a)).GetAwaiter().GetResult();
        Check(a.Gold == gold + 25 && Restore(relic, a).UsedThisCombat, "Once-per-combat cap/save lost");
    }

    private static MerchantCardEntry Entry(RunState run, Player player)
    {
        var entry = new MerchantCardEntry(player, null, [ModelDb.Card<TwinStrike>()], CardType.Attack);
        NativeConsoleFixture.Set(entry, nameof(entry.CreationResult), new CardCreationResult(run.CreateCard<Bash>(player)));
        AccessTools.Field(typeof(MerchantEntry), "_cost").SetValue(entry, 120);
        return entry;
    }
    private static void Ticket(Harmony harmony)
    {
        var (run, a, b, _) = Setup(); var ticket = Add<SootboundTicket>(a); var remoteTicket = Add<SootboundTicket>(b);
        run.PushRoom(new MerchantRoom()); run.AppendToMapPointHistory(MapPointType.Shop, RoomType.Shop, null);
        var service = new NetSingleplayerGameService();
        NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.NetService), service);
        using var sync = new RewardSynchronizer(new RunLocationTargetedMessageBuffer(service), service, run, a.NetId);
        NativeConsoleFixture.Set(RunManager.Instance, nameof(RunManager.RewardSynchronizer), sync);
        var send = typeof(NetSingleplayerGameService).GetMethods().Single(m => m.Name == "SendMessage" && m.GetParameters().Length == 1);
        harmony.Patch(send.MakeGenericMethod(typeof(RewardObtainedMessage)), prefix: new(typeof(ContentWaveRelicChecks), nameof(ObserveReward)));
        harmony.Patch(send.MakeGenericMethod(typeof(GoldLostMessage)), prefix: new(typeof(ContentWaveRelicChecks), nameof(ObserveGold)));
        harmony.Patch(AccessTools.Method(typeof(Hook), nameof(Hook.ShouldAddToDeck)), prefix: new(typeof(ContentWaveRelicChecks), nameof(CanAdd)));
        Rewards.Clear(); Debits.Clear(); a.Gold = 0;
        var entry = Entry(run, a);
        Check(ticket.Rarity == MegaCrit.Sts2.Core.Entities.Relics.RelicRarity.Rare && entry.Cost == 0, "Ticket rarity/visible free price");
        RejectCard = true;
        Check(!entry.OnTryPurchaseWrapper(null).GetAwaiter().GetResult() && ticket.SpentMerchant == "" && Rewards.Count == 0,
            "Failed purchase consumed ticket or sent reward");
        RejectCard = false;
        Check(entry.OnTryPurchaseWrapper(null).GetAwaiter().GetResult(), "Free native purchase failed with zero Gold");
        Check(a.Gold == 0 && Debits.SequenceEqual([0]) && Rewards.Count == 1 && ticket.SpentMerchant.Length > 0,
            "Free purchase debit/native packet/usage mismatch");
        Check(Entry(run, a).Cost == 120 && Entry(run, b).Cost == 0, "Second card free or owner contamination");
        Check(new MerchantCardRemovalEntry(a).Cost > 0, "Ticket discounted removal");
        Check(Restore(ticket, a).SpentMerchant == ticket.SpentMerchant, "Ticket usage lost on save/load");

        // Exact native purchased-card packet roundtrip, then native receiver path.
        var writer = new PacketWriter(); Rewards[0].Serialize(writer);
        var reader = new PacketReader(); reader.Reset(writer.Buffer);
        var received = new RewardObtainedMessage { rewardType = default, location = default, wasSkipped = false }; received.Deserialize(reader);
        // Model the remote copy of this same owner's relic, not a different owner.
        ticket.SpentMerchant = "";
        AccessTools.Method(typeof(RewardSynchronizer), "HandleRewardObtainedMessage").Invoke(sync, [received, a.NetId]);
        Check(ticket.SpentMerchant == SootboundMerchantPurchase.MerchantKey(a) && remoteTicket.SpentMerchant == "",
            "Native card receipt did not synchronize only its owner");
        run.CurrentMapPointHistoryEntry!.Rooms.Add(new() { RoomType = RoomType.Shop });
        Check(Entry(run, a).Cost == 0, "Second merchant at same map point did not reset allowance");
        Add<TheCourier>(a); Add<MembershipCard>(a);
        var courier = Entry(run, a); bool completedAtNormalPrice = false;
        courier.PurchaseCompleted += (_, entry) => completedAtNormalPrice = entry.IsStocked && entry.Cost > 0;
        harmony.Patch(AccessTools.Method(typeof(Hook), nameof(Hook.AfterCardChangedPiles)),
            prefix: new(typeof(ContentWaveRelicChecks), nameof(PauseDeckAdd)));
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        PurchaseGate = gate;
        var buying = courier.OnTryPurchaseWrapper(null);
        try
        {
            Check(!buying.IsCompleted && Entry(run, a).Cost > 0, "Pending free purchase did not reserve allowance");
            Check(!Entry(run, a).OnTryPurchaseWrapper(null).GetAwaiter().GetResult(), "Concurrent card purchase bypassed reservation");
        }
        finally { gate.TrySetResult(); PurchaseGate = null; }
        Check(buying.GetAwaiter().GetResult() && a.Gold == 0 && completedAtNormalPrice,
            "Courier/discounts did not commit free purchase before restock/UI completion");
        RejectCard = false;
    }
}
