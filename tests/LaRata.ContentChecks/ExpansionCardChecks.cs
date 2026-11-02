using System.Text.Json;
using BaseLib.Patches.Content;
using HarmonyLib;
using LaRata;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using static NovemberCardChecks;

internal static class ExpansionCardChecks
{
    private static readonly PlayerChoiceContext Ctx = new ThrowingPlayerChoiceContext();
    private static int checks;
    private static void Check(bool value, string reason) { checks++; if (!value) throw new Exception(reason); }
    private static void Await(Task task) => task.GetAwaiter().GetResult();
    private static bool NoTicks(ref ulong __result) { __result = 0; return false; }
    private static bool NoPowerAnimation(ref Task __result) { __result = Task.CompletedTask; return false; }
    internal static void Run() => NovemberCardChecks.Run(() =>
    {
        var harmony = new Harmony("LaRata.ContentChecks.ExpansionCards");
        // Pure presentation/time boundaries only. CardCmd.AutoPlay,
        // OnPlayWrapper, GeneratePlayCount, resource capture and piles are real.
        harmony.Patch(AccessTools.Method(typeof(Godot.Time), nameof(Godot.Time.GetTicksMsec)), prefix: new(typeof(ExpansionCardChecks), nameof(NoTicks)));
        harmony.Patch(AccessTools.Method(typeof(CardModel), "PlayPowerCardFlyVfx"), prefix: new(typeof(ExpansionCardChecks), nameof(NoPowerAnimation)));
        ModelDb.Inject(typeof(ExpansionProbePower)); ComboTurnPower.Register();
        try
        {
            foreach (var test in new Action[] { CatalogAndSave, Tribute, Coffin, Sale, Smith, Table, Text })
            { test(); Console.WriteLine("PASS Expansion " + test.Method.Name); }
            Console.WriteLine($"PASS: {checks} expansion card assertions; native card effects, Orb passives, actual HP/Exhaust hooks, AutoPlay wrapper/replays and serialization. Supplied combat fixture and presentation/time substitutions; no engine UI/live network/disk-save acceptance.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
    });
    private static readonly Type[] Types = [typeof(StaticTribute), typeof(GildedCoffin), typeof(BonfireSale), typeof(Bloodsmith), typeof(UnderTheTable)];
    private static void CatalogAndSave()
    {
        var entries = LaRataCardCatalog.Entries.Where(e => Types.Contains(e.Model.GetType())).ToArray();
        Check(entries.Length == 5 && entries.All(e => e.PlayerPlayable && e.BossId == null && !e.BossEligible && e.BoosterEligibility == LaRataBoosterEligibility.Neutral), "New cards leaked into boss/prize roles");
        Check(LaRataCardCatalog.Entries.Count == 71 && LaRataCardCatalog.Entries.Count(e => e.PlayerPlayable) == 46, "Total/player card count");
        Check(RoyalDeck.ActiveCards.Length == 41 && LaRataCardCatalog.BossCards.Count == 48 && LaRataCardCatalog.BossPrizes.Length == 10, "Boss identity/activity/prize counts changed");
        Check(LaRataCardCatalog.Neutrals.Length == 34 && LaRataCardCatalog.Neutrals.Count(c => c.Rarity == CardRarity.Uncommon) == 21 && LaRataCardCatalog.Neutrals.Count(c => c.Rarity == CardRarity.Rare) == 13, "Neutral rarity counts");
        Check(CustomContentDictionary.RegisteredTypes.Contains(typeof(BloodsmithPower)), "Bloodsmith registration");
        foreach (var entry in entries) foreach (bool upgraded in new[] { false, true })
        {
            var card = entry.Model.ToMutable(); if (upgraded) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
            Check(card.Pool is ColorlessCardPool && card.MultiplayerConstraint == CardMultiplayerConstraint.None, "Native pool/SP-MP admission");
            Check(card.EnergyCost.GetWithModifiers(CostModifiers.None) == (card is Bloodsmith ? 2 : card is BonfireSale && upgraded ? 0 : 1), "Cost " + card.Id);
            Check(card.Keywords.Contains(CardKeyword.Exhaust) == (card is BonfireSale or UnderTheTable), "Exhaust " + card.Id);
            Check(card.Rarity == (card is StaticTribute or GildedCoffin ? CardRarity.Uncommon : CardRarity.Rare), "Rarity " + card.Id);
            var json = JsonSerializer.Serialize(card.ToSerializable(), JsonSerializationUtility.GetTypeInfo<SerializableCard>());
            var loaded = CardModel.FromSerializable(JsonSerializer.Deserialize(json, JsonSerializationUtility.GetTypeInfo<SerializableCard>())!);
            Check(loaded.Id == card.Id && loaded.CurrentUpgradeLevel == card.CurrentUpgradeLevel && loaded.TargetType == card.TargetType && loaded.EnergyCost.GetWithModifiers(CostModifiers.None) == card.EnergyCost.GetWithModifiers(CostModifiers.None), "Native card JSON/upgrade/target roundtrip");
        }
        foreach (int party in new[] { 1, 2, 3, 4 })
        {
            var (_, _, a, _, _) = Setup(party); var seen = new HashSet<ModelId>();
            for (uint seed = 0; seed < 128; seed++)
                foreach (var result in BoosterRelic.GenerateContents(a, 5, new Rng(seed))) seen.Add(result.Card.Id);
            Check(entries.All(e => seen.Contains(e.ModelId)), "Expansion cards unreachable in production Royal booster generator");
        }
    }
    private static void Tribute()
    {
        foreach (bool up in new[] { false, true }) foreach (bool orbs in new[] { false, true })
        {
            var (_, _, a, b, enemy) = Setup();
            if (orbs)
            {
                a.PlayerCombatState!.OrbQueue.AddCapacity(4);
                Await(OrbCmd.Channel<FrostOrb>(Ctx, a)); Await(OrbCmd.Channel<LightningOrb>(Ctx, a));
                Await(OrbCmd.Channel<DarkOrb>(Ctx, a)); Await(OrbCmd.Channel<PlasmaOrb>(Ctx, a));
            }
            b.PlayerCombatState!.OrbQueue.AddCapacity(1); Await(OrbCmd.Channel<FrostOrb>(Ctx, b));
            Resolve(Card<StaticTribute>(a, upgraded: up), enemy);
            Check(enemy.CurrentHp == 1000 - (up ? 10 : 7) - (orbs ? 3 : 0), "Tribute attack and Lightning passive");
            Check(a.Creature.Block == (orbs ? 2 : 0) && a.PlayerCombatState!.Energy == (orbs ? 4 : 3) && b.Creature.Block == 0, "Tribute Frost/Plasma/owner");
            if (orbs) Check(a.PlayerCombatState!.OrbQueue.Orbs.OfType<DarkOrb>().Single().EvokeVal == 12 && a.PlayerCombatState.OrbQueue.Orbs.Count == 4, "Tribute Dark passive/no evoke");
        }
    }
    private static Creature Enemy(CombatState combat)
    {
        var enemy = new Creature(ModelDb.Monster<TwoTailedRat>().ToMutable(), CombatSide.Enemy, "extra") { CombatState = combat };
        combat.AddCreature(enemy); Hp(enemy, 100); return enemy;
    }
    private static void Coffin()
    {
        foreach (bool up in new[] { false, true }) foreach (int block in new[] { 0, 17 })
        {
            var (_, combat, a, b, first) = Setup(); var second = Enemy(combat); var artifact = Enemy(combat);
            Power<ArtifactPower>(artifact); a.Creature.GainBlockInternal(block); b.Creature.GainBlockInternal(9);
            Resolve(Card<GildedCoffin>(a, upgraded: up), first);
            Check(a.Creature.Block == 0 && b.Creature.Block == 9 && first.GetPowerAmount<DoomPower>() == block, "Coffin block conversion/owner");
            Check(second.GetPowerAmount<DoomPower>() == (up ? block : 0) && artifact.GetPowerAmount<DoomPower>() == 0, "Coffin whole amount per enemy/native Artifact");
            Check(artifact.GetPowerAmount<ArtifactPower>() == (up && block > 0 ? 0 : 1), "Coffin zero must not consume Artifact");
        }
        {
            var (_, _, a, _, target) = Setup(); a.Creature.GainBlockInternal(12);
            Power<ExpansionProbePower>(a.Creature).Broken = creature => { creature.GainBlockInternal(3); return Task.CompletedTask; };
            Resolve(Card<GildedCoffin>(a), target);
            Check(a.Creature.Block == 3 && target.GetPowerAmount<DoomPower>() == 12, "Coffin must capture/remove original Block once despite callbacks");
        }
    }
    private static void Sale()
    {
        foreach (bool up in new[] { false, true }) foreach (int count in new[] { 0, 1, 4 })
        {
            var (_, _, a, b, _) = Setup(); var attacks = Enumerable.Range(0, count).Select(_ => Card<StrikeIronclad>(a, PileType.Discard)).ToArray();
            var skill = Card<DefendIronclad>(a, PileType.Discard); var status = Card<Wound>(a, PileType.Discard); var other = Card<StrikeIronclad>(b, PileType.Discard);
            Resolve(Card<BonfireSale>(a, upgraded: up));
            Check(a.PlayerCombatState!.Energy == 3 + count && attacks.All(c => c.Pile?.Type == PileType.Exhaust), "Bonfire actual Attack exhaust/energy");
            Check(skill.Pile?.Type == PileType.Discard && status.Pile?.Type == PileType.Discard && other.Pile?.Type == PileType.Discard && b.PlayerCombatState!.Energy == 3, "Bonfire type/owner scope");
        }
        {
            var (_, _, a, b, _) = Setup(); var first = Card<StrikeIronclad>(a, PileType.Discard); var second = Card<Bash>(a, PileType.Discard); var foreign = Card<Bash>(b, PileType.Discard);
            Power<ExpansionProbePower>(a.Creature).Exhausted = async (ctx, card) =>
            { if (card == first) { await CardCmd.Exhaust(ctx, second); await CardCmd.Exhaust(ctx, foreign); Card<StrikeIronclad>(a, PileType.Discard); } };
            Resolve(Card<BonfireSale>(a));
            Check(a.PlayerCombatState!.Energy == 5 && a.PlayerCombatState.DiscardPile.Cards.Count == 1, "Bonfire nested actual events/snapshot/foreign exclusion");
        }
        {
            var (_, _, a, _, _) = Setup(); var first = Card<StrikeIronclad>(a, PileType.Discard); var moved = Card<Bash>(a, PileType.Discard);
            Power<ExpansionProbePower>(a.Creature).Exhausted = async (_, card) => { if (card == first) await CardPileCmd.Add(moved, PileType.Hand); };
            Resolve(Card<BonfireSale>(a)); Check(a.PlayerCombatState!.Energy == 4 && moved.Pile?.Type == PileType.Hand, "Bonfire stale snapshot must not exhaust moved Attack");
        }
    }
    private static void Smith()
    {
        foreach (bool up in new[] { false, true }) foreach (CombatSide side in new[] { CombatSide.Player, CombatSide.Enemy })
        {
            var (_, combat, a, b, enemy) = Setup(); Resolve(Card<Bloodsmith>(a, upgraded: up)); combat.CurrentSide = side;
            int events = 0; decimal forged = 0;
            Power<ExpansionProbePower>(a.Creature).Forged = (amount, player) => { Check(player == a, "Bloodsmith forged for wrong owner"); events++; forged += amount; return Task.CompletedTask; };
            Await(CreatureCmd.Damage(Ctx, a.Creature, 1, ValueProp.Unpowered, enemy));
            Await(CreatureCmd.Damage(Ctx, a.Creature, 12, ValueProp.Unpowered, enemy));
            Check(events == 2 && forged == (up ? 10 : 6), "Bloodsmith once per HP event, both sides");
            a.Creature.GainBlockInternal(20); Await(CreatureCmd.Damage(Ctx, a.Creature, 3, ValueProp.Unpowered, enemy));
            Await(CreatureCmd.LoseBlock(a.Creature, 17));
            Power<BufferPower>(a.Creature); Await(CreatureCmd.Damage(Ctx, a.Creature, 3, ValueProp.Unpowered, enemy));
            Await(CreatureCmd.Damage(Ctx, b.Creature, 2, ValueProp.Unpowered, enemy));
            Check(events == 2, "Bloodsmith triggered on Block/Buffer/partner");
            Resolve(Card<Bloodletting>(a));
            var poison = Power<PoisonPower>(a.Creature, 2); Await(poison.AfterSideTurnStart(side, [a.Creature], combat));
            Check(events == 4 && forged == 4 * (up ? 5 : 3), "Bloodsmith native HP cost/Poison");
            Check(a.PlayerCombatState!.AllCards.OfType<SovereignBlade>().Count() == 1, "Bloodsmith created repeated blades instead of native Forge");
            Await(CreatureCmd.Heal(a.Creature, 1)); Check(events == 4, "Healing triggered Bloodsmith");
        }
    }
    private static int Plays(CardModel card) => CombatManager.Instance.History.Entries.OfType<CardPlayFinishedEntry>().Count(e => e.CardPlay.Card == card);
    private static void Table()
    {
        foreach (bool up in new[] { false, true }) foreach (int party in new[] { 1, 2, 3, 4 })
        {
            var (_, _, a, _, enemy) = Setup(party); var top = Card<DefendIronclad>(a, PileType.Draw); var bottom = Card<StrikeIronclad>(a, PileType.Draw);
            Resolve(Card<UnderTheTable>(a, upgraded: up));
            Check(enemy.CurrentHp == 1000 - (up ? 18 : 12) && Plays(bottom) == (up ? 3 : 2), "Table native bottom/replay count");
            Check(top.Pile?.Type == PileType.Draw && bottom.Pile?.Type == PileType.Discard && bottom.BaseReplayCount == 0 && a.Deck.Cards.Count == 0, "Table must preserve identity/base replay/permanent deck");
            Check(a.PlayerCombatState!.Energy == 3, "Table autoplay spent Energy");
        }
        {
            var (_, _, a, _, _) = Setup(); Resolve(Card<UnderTheTable>(a)); Check(a.PlayerCombatState!.AllCards.Count() == 1, "Table empty pile generated cards");
        }
        {
            var (_, _, a, _, enemy) = Setup(); var x = Card<Malaise>(a, PileType.Draw); Resolve(Card<UnderTheTable>(a));
            Check(Plays(x) == 2 && enemy.GetPowerAmount<WeakPower>() == 6 && enemy.GetPowerAmount<StrengthPower>() == -6 && a.PlayerCombatState!.Energy == 3, "Native X capture/free replay");
            Check(x.Pile?.Type == PileType.Exhaust && CombatManager.Instance.History.Entries.OfType<CardExhaustedEntry>().Count(e => e.Card == x) == 1, "Replayed Exhaust card must exhaust once");
        }
        {
            var (_, _, a, _, enemy) = Setup(); a.PlayerCombatState!.Stars = 3; var stars = Card<Stardust>(a, PileType.Draw); Resolve(Card<UnderTheTable>(a));
            Check(Plays(stars) == 2 && enemy.CurrentHp == 970 && a.PlayerCombatState.Stars == 3, "Native X-Stars capture/replay/free autoplay");
        }
        {
            var (_, _, a, _, _) = Setup(); var power = Card<Bloodsmith>(a, PileType.Draw); Resolve(Card<UnderTheTable>(a));
            Check(Plays(power) == 2 && a.Creature.GetPowerAmount<BloodsmithPower>() == 6 && power.Pile == null && power.BaseReplayCount == 0, "Power replay then one native removal");
        }
        {
            var (_, _, a, _, _) = Setup(); var status = Card<Wound>(a, PileType.Draw); Resolve(Card<UnderTheTable>(a));
            Check(Plays(status) == 0 && status.Pile?.Type == PileType.Discard && status.BaseReplayCount == 0, "Unplayable native result handling");
        }
        {
            var (_, _, a, _, enemy) = Setup(); var card = Card<StrikeIronclad>(a, PileType.Draw); card.BaseReplayCount = 1; Power<DuplicationPower>(a.Creature);
            Resolve(Card<UnderTheTable>(a));
            Check(Plays(card) == 4 && enemy.CurrentHp == 976 && card.BaseReplayCount == 1 && !a.Creature.HasPower<DuplicationPower>(), "Existing native replay/duplication composition and restore");
        }
        {
            var (_, _, a, _, enemy) = Setup(); var strike = Card<StrikeIronclad>(a, PileType.Draw); var nested = Card<UnderTheTable>(a, PileType.Draw);
            Resolve(Card<UnderTheTable>(a));
            Check(Plays(nested) == 2 && Plays(strike) == 2 && nested.Pile?.Type == PileType.Exhaust && enemy.CurrentHp == 988, "Nested Table must select once per actual play and terminate on empty draw");
        }
        {
            var (_, combat, a, _, first) = Setup(); var second = Enemy(combat); Hp(first, 1); Hp(second, 1);
            var strike = Card<StrikeIronclad>(a, PileType.Draw); Resolve(Card<UnderTheTable>(a, upgraded: true));
            Check(new[] { first, second }.Count(e => e.IsDead) == 1 && Plays(strike) == 3,
                "Native single-target replay must retain its original target after death");
            Check(strike.Pile?.Type == PileType.Discard && strike.BaseReplayCount == 0, "Target death replay cleanup");
        }
        {
            var (_, _, a, _, enemy) = Setup(); Hp(a.Creature, 1); Power<ThornsPower>(enemy, 2);
            var strike = Card<StrikeIronclad>(a, PileType.Draw); Resolve(Card<UnderTheTable>(a));
            Check(a.Creature.IsDead && Plays(strike) == 0 &&
                CombatManager.Instance.History.Entries.OfType<CardPlayStartedEntry>().Count(e => e.CardPlay.Card == strike) == 1,
                "Owner death must stop the native sequence inside the first play");
            Check(strike.BaseReplayCount == 0, "Owner death must restore temporary replay count");
        }
        {
            var (_, _, a, _, _) = Setup(); var card = Card<Malaise>(a, PileType.Draw);
            Power<ExpansionProbePower>(a.Creature).Exhausted = (_, exhausted) => exhausted == card
                ? throw new InvalidOperationException("expansion-fixture-exhaust") : Task.CompletedTask;
            bool observed = false;
            try { Resolve(Card<UnderTheTable>(a)); }
            catch (InvalidOperationException error) when (error.Message == "expansion-fixture-exhaust") { observed = true; }
            Check(observed && card.BaseReplayCount == 0, "Native hook exception must propagate and restore temporary replay count");
        }
    }
    private static void Text()
    {
        foreach (var (language, culture) in new[] { ("eng", "en-US"), ("deu", "de-DE"), ("zhs", "zh-CN") })
        {
            LoadLocale(LocManager.Instance, language, culture);
            foreach (var type in Types) foreach (bool up in new[] { false, true })
            {
                var card = ModelDb.GetById<CardModel>(ModelDb.GetId(type)).ToMutable(); if (up) { card.UpgradeInternal(); card.FinalizeUpgradeInternal(); }
                var text = card.GetDescriptionForPile(PileType.None);
                Check(!text.Contains('{') && !text.Contains('}'), $"Unresolved text {language}/{card.Id}");
                if (language == "eng" && card is GildedCoffin) Check(text.Contains("ALL enemies") == up, "Coffin upgraded-only ALL enemies text");
            }
            var textPower = ModelDb.Power<BloodsmithPower>().Description; textPower.Add("Amount", 5);
            Check(!textPower.GetFormattedText().Contains('{'), "Bloodsmith power formatter");
        }
    }
}

internal sealed class ExpansionProbePower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    internal Func<PlayerChoiceContext, CardModel, Task>? Exhausted;
    internal Func<Creature, Task>? Broken;
    internal Func<decimal, Player, Task>? Forged;
    public override Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal) => Exhausted?.Invoke(ctx, card) ?? Task.CompletedTask;
    public override Task AfterBlockBroken(Creature creature) => Broken?.Invoke(creature) ?? Task.CompletedTask;
    public override Task AfterForge(decimal amount, Player player, AbstractModel? source) => Forged?.Invoke(amount, player) ?? Task.CompletedTask;
}
