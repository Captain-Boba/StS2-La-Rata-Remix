namespace LaRata;

internal enum TwistKind
{
    Sturdy, Keen, Armored, Warded, Empowered, Echoing, Hexed, Bristling,
    Regenerating, Cultist, Demonic, Entrenched, Buffered, Furious, Venomous,
    Ominous, Cinders, Disorienting, Lacerating, Phased, Slippery, WoundUp,
    Agile, Fortified, SecondWind, Reservist, Shackling, Sapping, Constricting, Eroding,
    Spiteful, VenomCoated, EmberGuard, GlassCrown, MirrorGuard, BloodFeud,
    Doomweaver, Hotheaded, Afterglow, SkillHunter
}

internal sealed record EnemyTwist(TwistKind Kind, int Target, int Amount, int ActIndex = 0)
{
    // Scale flat defenses from the saved act, before native party scaling. Duration,
    // immunity, card pollution and debuff caps intentionally do not multiply.
    internal int Defense(int amount) => (amount * (2 + Math.Clamp(ActIndex, 0, 3)) + 1) / 2;
    internal bool IsHex => EncounterPlan.IsHex(Kind);
    internal string Description => Kind switch
    {
        TwistKind.Sturdy => $"Sturdy: +{Amount}% HP",
        TwistKind.Keen => $"Keen: +{Amount}% base damage",
        TwistKind.Armored => $"Armored: {Defense(Amount)} base Plating",
        TwistKind.Warded => "Warded: 1 Artifact",
        TwistKind.Empowered => $"Empowered: {Defense(2)} base Plating, stronger minor buffs",
        TwistKind.Echoing => $"Echoing: extra {EncounterPlan.HexName(Amount)} and stronger short debuffs",
        TwistKind.Hexed => $"Hexed: 2 {EncounterPlan.HexName(Amount)}",
        TwistKind.Bristling => "Bristling: 2 Thorns",
        TwistKind.Regenerating => $"Regenerating: {Defense(3)} Regen",
        TwistKind.Cultist => "Cultist: 1 Ritual",
        TwistKind.Demonic => "Demonic: 1 Demon Form",
        TwistKind.Entrenched => $"Entrenched: Barricade and {Defense(8)} base Block",
        TwistKind.Buffered => "Buffered: 1 Buffer",
        TwistKind.Furious => "Furious: 1 Strength",
        TwistKind.Venomous => "Venomous: 2 Poison",
        TwistKind.Ominous => "Ominous: 4 Doom, capped at a quarter of target max HP",
        TwistKind.Cinders => "Cinders: shuffle 1 Burn into the draw pile",
        TwistKind.Disorienting => "Disorienting: shuffle 1 Dazed into the draw pile",
        TwistKind.Lacerating => "Lacerating: add 1 Wound to the discard pile",
        TwistKind.Phased => "Phased: 1 Intangible",
        TwistKind.Slippery => "Slippery: 1 Slippery (native co-op scaling)",
        TwistKind.WoundUp => "Wound Up: 2 Vigor for the next attack",
        TwistKind.Agile => $"Agile: 2 Dexterity and {Defense(6)} base Block",
        TwistKind.Fortified => $"Fortified: {Defense(12)} opening base Block",
        TwistKind.SecondWind => $"Second Wind: {Defense(8)} base Block after the next block clear",
        TwistKind.Reservist => $"Reservist: {Defense(6)} base Block after each of the next two block clears",
        TwistKind.Shackling => "Shackling: temporarily reduce Strength by 1",
        TwistKind.Sapping => "Sapping: reduce Dexterity toward -1",
        TwistKind.Constricting => "Constricting: 2 Constrict, capped at 4",
        TwistKind.Eroding => "Eroding: 1 Disintegration, capped at 2",
        TwistKind.Spiteful => "Spiteful: 1 Rupture; damage on its own turn grants Strength",
        TwistKind.VenomCoated => "Venom Coated: unblocked attacks apply 1 Poison",
        TwistKind.EmberGuard => "Ember Guard: retaliate for 3 damage during the first player turn",
        TwistKind.GlassCrown => "Glass Crown: halve attack damage until the first enemy turn ends",
        TwistKind.MirrorGuard => $"Mirror Guard: {Defense(6)} base Block; reflect blocked attack damage during the first player turn",
        TwistKind.BloodFeud => $"Blood Feud: first ally death grants 2 Strength and {Defense(12)} base Block",
        TwistKind.Doomweaver => $"Doomweaver: inflict 2 Doom (quarter-HP cap), gain {Defense(4)} Block whenever applying Doom",
        TwistKind.Hotheaded => "Hotheaded: 3 temporary Strength for the first enemy turn",
        TwistKind.Afterglow => $"Afterglow: {Defense(10)} base Block, retained through the first enemy turn start",
        TwistKind.SkillHunter => "Skill Hunter: gain 1 Strength whenever a Skill is played",
        _ => throw new ArgumentOutOfRangeException()
    };
}

// Quarter-point units: party-linear budgets grow 40% of the act-one budget per act.
internal sealed record EncounterPlan(int Budget, int Spent, int ReinforcementCount, EnemyTwist[] Twists)
{
    internal static int BaseBudget(int players, int tier) => (tier == 2 ? 2 : tier == 1 ? 3 : 4) * Math.Clamp(players, 1, 4);
    internal static int StageBudget(int players, int tier, int actIndex) => BaseBudget(players, tier) * (5 + 2 * Math.Clamp(actIndex, 0, 3));
    internal string[] Affixes => [.. Enumerable.Repeat("Reinforcement", ReinforcementCount), .. Twists.Select(t => $"enemy[{t.Target}] {t.Description}")];
    internal const int ReinforcementCost = 16;
    internal static int ReinforcementChance(int players, int actIndex) => Math.Min(100, 70 + (Math.Clamp(players, 1, 4) - 1) * 10 + Math.Clamp(actIndex, 0, 3) * 5);
    internal static int ReinforcementLimit(int players, int actIndex) => players >= 3 && actIndex >= 1 ? 3 : players >= 2 || actIndex >= 2 ? 2 : 1;
    internal static bool IsHex(TwistKind k) => k is TwistKind.Echoing or TwistKind.Hexed or >= TwistKind.Venomous and <= TwistKind.Lacerating or >= TwistKind.Shackling and <= TwistKind.Eroding or TwistKind.Doomweaver;
    internal static bool IsOffense(TwistKind k) => k is TwistKind.Keen or TwistKind.Empowered or TwistKind.Cultist or TwistKind.Demonic or TwistKind.Furious or TwistKind.WoundUp or TwistKind.Spiteful or TwistKind.VenomCoated or TwistKind.BloodFeud or TwistKind.Hotheaded or TwistKind.SkillHunter;
    internal static bool IsShield(TwistKind k) => k is TwistKind.Buffered or TwistKind.Phased or TwistKind.Slippery or TwistKind.GlassCrown or TwistKind.MirrorGuard;
    internal static bool IsBlock(TwistKind k) => k is TwistKind.Armored or TwistKind.Empowered or TwistKind.Entrenched or TwistKind.Agile or TwistKind.Fortified or TwistKind.SecondWind or TwistKind.Reservist or TwistKind.MirrorGuard or TwistKind.BloodFeud or TwistKind.Doomweaver or TwistKind.Afterglow;
    internal static bool IsRetaliation(TwistKind k) => k is TwistKind.Bristling or TwistKind.EmberGuard or TwistKind.MirrorGuard;
    internal static int Cost(TwistKind k) => k switch
    {
        TwistKind.Sturdy => 4,
        TwistKind.SkillHunter => 20,
        TwistKind.MirrorGuard or TwistKind.BloodFeud or TwistKind.Doomweaver => 16,
        TwistKind.VenomCoated or TwistKind.GlassCrown => 12,
        TwistKind.Hexed or TwistKind.Cultist or TwistKind.Demonic or TwistKind.Entrenched or TwistKind.Phased or TwistKind.Reservist or TwistKind.Constricting or TwistKind.Eroding => 12,
        _ => 8
    };

    public static EncounterPlan Roll(Func<int, int> next, int players, int tier,
        int maxReinforcements, int enemyCount, bool forceReinforce = false, int actIndex = 0)
    {
        players = Math.Clamp(players, 1, 4);
        actIndex = Math.Clamp(actIndex, 0, 3);
        int budget = StageBudget(players, tier, actIndex);
        int extra = 0;
        int capacity = tier == 0 ? Math.Clamp(Math.Min(Math.Min(maxReinforcements, 4 - enemyCount), budget / ReinforcementCost), 0, ReinforcementLimit(players, actIndex)) : 0;
        if (capacity > 0 && (forceReinforce || next(100) < ReinforcementChance(players, actIndex)))
        {
            extra = 1;
            if (capacity > 1 && next(100) < 60 + actIndex * 5) extra++;
            if (capacity > 2 && extra == 2 && next(100) < 35 + actIndex * 5) extra++;
        }
        int remaining = budget - extra * ReinforcementCost;
        int totalEnemies = enemyCount + extra;
        var twists = new List<EnemyTwist>();
        var candidates = Enum.GetValues<TwistKind>().Where(k => k is not (TwistKind.Sturdy or TwistKind.Keen)).ToList();
        int limit = 3 * players + actIndex - extra;
        for (int i = 0; i < limit; i++)
        {
            int[] Targets(TwistKind k) => Enumerable.Range(0, totalEnemies).Where(target =>
                !twists.Any(t => t.Target == target &&
                    ((IsHex(k) && t.IsHex) || (IsShield(k) && IsShield(t.Kind)) || (IsBlock(k) && IsBlock(t.Kind)) || (IsRetaliation(k) && IsRetaliation(t.Kind))))).ToArray();
            candidates.RemoveAll(k => Cost(k) > remaining ||
                (k == TwistKind.BloodFeud && totalEnemies < 2) ||
                (k == TwistKind.SkillHunter && (actIndex == 0 || tier != 0)) ||
                (IsOffense(k) && twists.Any(t => IsOffense(t.Kind))) ||
                (IsHex(k) && twists.Count(t => t.IsHex) >= (players >= 3 ? 2 : 1)) || Targets(k).Length == 0);
            if (candidates.Count == 0) break;
            var kind = candidates[next(candidates.Count)];
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
            twists.Add(new(kind, targetIndex, amount, actIndex));
            remaining -= Cost(kind);
            if (i == 0) candidates.AddRange([TwistKind.Sturdy, TwistKind.Keen]);
        }
        return new(budget, budget - remaining, extra, twists.ToArray());
    }

    internal static string HexName(int hex) => hex switch { 1 => "Weak", 2 => "Frail", 3 => "Vulnerable", _ => "None" };
    internal static EncounterPlan BossDuo => new(0, 0, 0, []);
}
