using MegaCrit.Sts2.Core.Random;

namespace LaRata;

internal enum TwistKind
{
    Sturdy, Keen, Armored, Warded, Empowered, Echoing, Hexed, Bristling,
    Regenerating, Cultist, Demonic, Entrenched, Buffered, Furious, Venomous,
    Ominous, Cinders, Disorienting, Lacerating, Phased, Slippery, WoundUp,
    Agile, Fortified, SecondWind, Reservist, Shackling, Sapping, Constricting, Eroding,
    Spiteful, VenomCoated, EmberGuard, GlassCrown, MirrorGuard, BloodFeud,
    Doomweaver, Hotheaded, Afterglow, SkillHunter,
    Reckless, BrittleWard, ToxicShell, LastStand, FadingMight, Leaden, StaticHex, Crossfire
}

internal sealed record EnemyTwist(TwistKind Kind, int Target, int Amount, int ActIndex = 0, bool Lesser = false)
{
    // Scale flat defenses from the saved act, before native party scaling. Duration,
    // immunity, card pollution and debuff caps intentionally do not multiply.
    internal int Magnitude(int amount) => Lesser ? Math.Max(1, amount / 2) : amount;
    internal int Defense(int amount) => (Magnitude(amount) * (2 + Math.Clamp(ActIndex, 0, 3)) + 1) / 2;
    internal bool IsHex => EncounterPlan.IsHex(Kind);
    internal string Description => (Lesser ? "Lesser " : "") + (Kind switch
    {
        TwistKind.Sturdy => $"Sturdy: +{Amount}% HP",
        TwistKind.Keen => $"Keen: +{Amount}% base damage",
        TwistKind.Armored => $"Armored: {Defense(Amount)} base Plating",
        TwistKind.Warded => "Warded: 1 Artifact",
        TwistKind.Empowered => "Empowered: 2 Vigor, stronger minor Strength buffs",
        TwistKind.Echoing => $"Echoing: extra {EncounterPlan.HexName(Amount)} and stronger short debuffs",
        TwistKind.Hexed => $"Hexed: {Magnitude(2)} {EncounterPlan.HexName(Amount)}",
        TwistKind.Bristling => $"Bristling: {Magnitude(2)} Thorns",
        TwistKind.Regenerating => $"Regenerating: up to {Defense(3)} Regen, capped at 10% max HP (minimum 1); Inklets max 1; no extra party multiplier",
        TwistKind.Cultist => "Cultist: 1 Ritual",
        TwistKind.Demonic => "Demonic: 1 Demon Form",
        TwistKind.Entrenched => $"Entrenched: Barricade and {Defense(8)} base Block",
        TwistKind.Buffered => "Buffered: 1 Buffer",
        TwistKind.Furious => "Furious: 1 Strength",
        TwistKind.Venomous => $"Venomous: {Magnitude(2)} Poison",
        TwistKind.Ominous => $"Ominous: {Magnitude(4)} Doom, capped at a quarter of target max HP",
        TwistKind.Cinders => "Cinders: shuffle 1 Burn into the draw pile",
        TwistKind.Disorienting => "Disorienting: shuffle 1 Dazed into the draw pile",
        TwistKind.Lacerating => "Lacerating: add 1 Wound to the discard pile",
        TwistKind.Phased => "Phased: 1 Intangible",
        TwistKind.Slippery => "Slippery: 1 Slippery (native co-op scaling)",
        TwistKind.WoundUp => $"Wound Up: {Magnitude(2)} Vigor for the next attack",
        TwistKind.Agile => $"Agile: 2 Dexterity and {Defense(6)} base Block",
        TwistKind.Fortified => $"Fortified: {Defense(12)} opening base Block",
        TwistKind.SecondWind => $"Second Wind: {Defense(8)} base Block after the next block clear",
        TwistKind.Reservist => $"Reservist: {Defense(6)} base Block after each of the next two block clears",
        TwistKind.Shackling => "Shackling: temporarily reduce Strength by 1",
        TwistKind.Sapping => "Sapping: reduce Dexterity toward -1",
        TwistKind.Constricting => $"Constricting: {Magnitude(2)} Constrict, capped at 4",
        TwistKind.Eroding => "Eroding: 1 Disintegration, capped at 2",
        TwistKind.Spiteful => "Spiteful: 1 Rupture; damage on its own turn grants Strength",
        TwistKind.VenomCoated => "Venom Coated: unblocked attacks apply 1 Poison",
        TwistKind.EmberGuard => $"Ember Guard: retaliate for {Magnitude(3)} damage during the first player turn",
        TwistKind.GlassCrown => "Glass Crown: halve attack damage until the first enemy turn ends",
        TwistKind.MirrorGuard => $"Mirror Guard: {Defense(6)} base Block; reflect blocked attack damage during the first player turn",
        TwistKind.BloodFeud => $"Blood Feud: first ally death grants 2 Strength and {Defense(12)} base Block",
        TwistKind.Doomweaver => $"Doomweaver: inflict 2 Doom (quarter-HP cap), gain {Defense(4)} Block whenever applying Doom",
        TwistKind.Hotheaded => $"Hotheaded: {Magnitude(3)} temporary Strength for the first enemy turn",
        TwistKind.Afterglow => $"Afterglow: {Defense(10)} base Block, retained through the first enemy turn start",
        TwistKind.SkillHunter => "Skill Hunter: gain 1 Strength whenever a Skill is played",
        TwistKind.Reckless => "Reckless: 2 Strength, but 1 Vulnerable",
        TwistKind.BrittleWard => $"Brittle Ward: {Defense(18)} opening Block, but 2 Vulnerable",
        TwistKind.ToxicShell => "Toxic Shell: 3 Thorns, but starts with 3 Poison",
        TwistKind.LastStand => "Last Stand: 1 Demon Form, but starts with 1 Weak",
        TwistKind.FadingMight => "Fading Might: 5 temporary Strength and 1 Weak for the opening turn",
        TwistKind.Leaden => "Leaden: add 1 Slimed to the discard pile",
        TwistKind.StaticHex => "Static Hex: shuffle 1 Void into the draw pile",
        TwistKind.Crossfire => "Crossfire: 1 Weak and 1 Vulnerable, capped at 2 each",
        _ => throw new ArgumentOutOfRangeException()
    });
}

// Sixteenth-point units preserve exact 25% reductions even for odd elite budgets.
// Party scaling stays exactly linear; each act adds 40% of the act-one budget.
internal sealed record EncounterPlan(int Budget, int Spent, int ReinforcementCount, EnemyTwist[] Twists)
{
    internal bool ScoutReinforcement { get; init; }
    internal int HelperCost => ScoutReinforcement ? 40 : ReinforcementCost;
    internal static int BaseBudget(int players, int tier) => (tier == 2 ? 2 : tier == 1 ? 3 : 4) * Math.Clamp(players, 1, 4);
    internal static int StageBudget(int players, int tier, int actIndex) => 3 * BaseBudget(players, tier) * (5 + 2 * Math.Clamp(actIndex, 0, 3));
    // Keep enum IDs stable for diagnostics; retired families never enter a new plan.
    internal static readonly TwistKind[] ActiveKinds = Enum.GetValues<TwistKind>().Where(k => k is not
        (TwistKind.Armored or TwistKind.Slippery or TwistKind.Spiteful or TwistKind.SkillHunter or TwistKind.Doomweaver)).ToArray();
    internal string[] Affixes => [.. Enumerable.Repeat(ScoutReinforcement ? "Scout reinforcement: 35% HP, 50% attack (Inklet keeps its native attack)" : "Reinforcement", ReinforcementCount), .. Twists.Select(t => $"enemy[{t.Target}] {t.Description}")];
    internal const int ReinforcementCost = 64;
    internal static int ReinforcementChance(int players, int actIndex) => Math.Min(100, 70 + (Math.Clamp(players, 1, 4) - 1) * 10 + Math.Clamp(actIndex, 0, 3) * 5);
    internal static int ReinforcementLimit(int players, int actIndex) => players >= 3 && actIndex >= 1 ? 3 : players >= 2 || actIndex >= 2 ? 2 : 1;
    internal static bool IsHex(TwistKind k) => k is TwistKind.Echoing or TwistKind.Hexed or >= TwistKind.Venomous and <= TwistKind.Lacerating or >= TwistKind.Shackling and <= TwistKind.Eroding or TwistKind.Doomweaver or TwistKind.Leaden or TwistKind.StaticHex or TwistKind.Crossfire;
    internal static bool IsOffense(TwistKind k) => k is TwistKind.Keen or TwistKind.Empowered or TwistKind.Cultist or TwistKind.Demonic or TwistKind.Furious or TwistKind.WoundUp or TwistKind.Spiteful or TwistKind.VenomCoated or TwistKind.BloodFeud or TwistKind.Hotheaded or TwistKind.SkillHunter or TwistKind.Reckless or TwistKind.LastStand or TwistKind.FadingMight;
    internal static bool IsShield(TwistKind k) => k is TwistKind.Buffered or TwistKind.Phased or TwistKind.Slippery or TwistKind.GlassCrown or TwistKind.MirrorGuard;
    internal static bool IsBlock(TwistKind k) => k is TwistKind.Armored or TwistKind.Entrenched or TwistKind.Agile or TwistKind.Fortified or TwistKind.SecondWind or TwistKind.Reservist or TwistKind.MirrorGuard or TwistKind.BloodFeud or TwistKind.Doomweaver or TwistKind.Afterglow or TwistKind.BrittleWard;
    internal static bool IsRetaliation(TwistKind k) => k is TwistKind.Bristling or TwistKind.EmberGuard or TwistKind.MirrorGuard or TwistKind.ToxicShell;
    // Both native Thorns grants have one quarter of the normal selection weight.
    // Retaliation effects that do not grant Thorns keep their existing weight.
    internal static int SelectionWeight(TwistKind kind) => kind is TwistKind.Bristling or TwistKind.ToxicShell ? 1 : 4;
    internal static TwistKind SelectTwist(IReadOnlyList<TwistKind> choices, Func<int, int> next)
    {
        int totalWeight = choices.Sum(SelectionWeight);
        // Midpoints avoid the native weighted selector's inclusive boundaries.
        return Rng.WeightedNextItem((next(totalWeight) + .5f) / totalWeight, choices,
            kind => SelectionWeight(kind), choices[^1]);
    }
    internal static int Cost(TwistKind k) => 4 * (k switch
    {
        TwistKind.Sturdy => 4,
        TwistKind.SkillHunter => 20,
        TwistKind.StaticHex or TwistKind.Crossfire => 16,
        TwistKind.Reckless or TwistKind.LastStand or TwistKind.FadingMight => 12,
        TwistKind.MirrorGuard or TwistKind.BloodFeud or TwistKind.Doomweaver => 16,
        TwistKind.VenomCoated or TwistKind.GlassCrown => 12,
        TwistKind.Hexed or TwistKind.Cultist or TwistKind.Demonic or TwistKind.Entrenched or TwistKind.Phased or TwistKind.Reservist or TwistKind.Constricting or TwistKind.Eroding => 12,
        _ => 8
    });
    internal static bool HasLesser(TwistKind k) => k is TwistKind.Hexed or TwistKind.Bristling or TwistKind.Venomous
        or TwistKind.Ominous or TwistKind.WoundUp or TwistKind.Fortified or TwistKind.SecondWind
        or TwistKind.Constricting or TwistKind.EmberGuard or TwistKind.Hotheaded;
    internal static int Cost(EnemyTwist twist) => Cost(twist.Kind) / (twist.Lesser ? 2 : 1);

    public static EncounterPlan Roll(Func<int, int> next, int players, int tier,
        int maxReinforcements, int enemyCount, bool forceReinforce = false, int actIndex = 0)
    {
        players = Math.Clamp(players, 1, 4);
        actIndex = Math.Clamp(actIndex, 0, 3);
        int budget = StageBudget(players, tier, actIndex);
        bool smallBudget = budget < 96;
        bool scout = tier == 0 && budget < ReinforcementCost;
        int helperCost = scout ? 40 : ReinforcementCost;
        int Price(TwistKind kind) => Cost(kind) / (smallBudget && HasLesser(kind) ? 2 : 1);
        int extra = 0;
        int capacity = tier == 0 ? Math.Clamp(Math.Min(Math.Min(maxReinforcements, 4 - enemyCount), budget / helperCost), 0, ReinforcementLimit(players, actIndex)) : 0;
        if (capacity > 0 && (forceReinforce || next(100) < ReinforcementChance(players, actIndex)))
        {
            extra = 1;
            if (capacity > 1 && next(100) < 60 + actIndex * 5) extra++;
            if (capacity > 2 && extra == 2 && next(100) < 35 + actIndex * 5) extra++;
        }
        int remaining = budget - extra * helperCost;
        int totalEnemies = enemyCount + extra;
        var twists = new List<EnemyTwist>();
        var candidates = ActiveKinds.Where(k => k is not (TwistKind.Sturdy or TwistKind.Keen)).ToList();
        int limit = 3 * players + actIndex - extra;
        for (int i = 0; i < limit; i++)
        {
            int[] Targets(TwistKind k) => Enumerable.Range(0, totalEnemies).Where(target =>
                !twists.Any(t => t.Target == target &&
                    ((IsHex(k) && t.IsHex) || (IsShield(k) && IsShield(t.Kind)) || (IsBlock(k) && IsBlock(t.Kind)) || (IsRetaliation(k) && IsRetaliation(t.Kind))))).ToArray();
            candidates.RemoveAll(k => Price(k) > remaining ||
                (k == TwistKind.BloodFeud && totalEnemies < 2) ||
                (k == TwistKind.SkillHunter && (actIndex == 0 || tier != 0)) ||
                (IsOffense(k) && twists.Any(t => IsOffense(t.Kind))) ||
                (IsHex(k) && twists.Count(t => t.IsHex) >= (players >= 3 ? 2 : 1)) || Targets(k).Length == 0);
            if (candidates.Count == 0) break;
            // In a small unreinforced encounter, leave room for a second effect
            // when an affordable first effect exists. Larger-party rolls stay unchanged.
            var choices = smallBudget && i == 0 && extra == 0
                ? candidates.Where(k => Price(k) <= remaining / 2).ToList() : candidates;
            if (choices.Count == 0) choices = candidates;
            var kind = SelectTwist(choices, next);
            candidates.Remove(kind);
            var eligible = Targets(kind);
            int load = eligible.Min(target => twists.Count(t => t.Target == target));
            var leastLoaded = eligible.Where(target => twists.Count(t => t.Target == target) == load).ToArray();
            int targetIndex = leastLoaded[next(leastLoaded.Length)];
            int amount = kind switch
            {
                TwistKind.Sturdy => 10 + 4 * actIndex + next(9), TwistKind.Keen => 8 + 2 * actIndex + next(9),
                TwistKind.Armored => 2 + next(3), TwistKind.Echoing or TwistKind.Hexed => 1 + next(3), _ => 1
            };
            twists.Add(new(kind, targetIndex, amount, actIndex, smallBudget && HasLesser(kind)));
            remaining -= Price(kind);
            if (i == (smallBudget ? 1 : 0)) candidates.AddRange([TwistKind.Sturdy, TwistKind.Keen]);
        }
        return new(budget, budget - remaining, extra, twists.ToArray()) { ScoutReinforcement = scout && extra > 0 };
    }

    internal static string HexName(int hex) => hex switch { 1 => "Weak", 2 => "Frail", 3 => "Vulnerable", _ => "None" };
    internal static EncounterPlan BossDuo => new(0, 0, 0, []);
}
