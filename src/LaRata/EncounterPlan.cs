namespace LaRata;

internal enum TwistKind
{
    Sturdy, Keen, Armored, Warded, Empowered, Echoing, Hexed, Bristling,
    Regenerating, Cultist, Demonic, Entrenched, Buffered, Furious, Venomous,
    Ominous, Cinders, Disorienting, Lacerating, Phased, Slippery, WoundUp,
    Agile, Fortified, SecondWind, Reservist, Shackling, Sapping, Constricting, Eroding
}

internal sealed record EnemyTwist(TwistKind Kind, int Target, int Amount)
{
    internal bool IsHex => EncounterPlan.IsHex(Kind);
    internal string Description => Kind switch
    {
        TwistKind.Sturdy => $"Sturdy: +{Amount}% HP",
        TwistKind.Keen => $"Keen: +{Amount}% base damage",
        TwistKind.Armored => $"Armored: {Amount} Plating",
        TwistKind.Warded => "Warded: 1 Artifact",
        TwistKind.Empowered => "Empowered: 2 Plating, stronger minor buffs",
        TwistKind.Echoing => $"Echoing: extra {EncounterPlan.HexName(Amount)} and stronger short debuffs",
        TwistKind.Hexed => $"Hexed: 2 {EncounterPlan.HexName(Amount)}",
        TwistKind.Bristling => "Bristling: 2 Thorns",
        TwistKind.Regenerating => "Regenerating: 3 Regen",
        TwistKind.Cultist => "Cultist: 1 Ritual",
        TwistKind.Demonic => "Demonic: 1 Demon Form",
        TwistKind.Entrenched => "Entrenched: Barricade and 8 Block",
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
        TwistKind.Agile => "Agile: 2 Dexterity and 6 Block",
        TwistKind.Fortified => "Fortified: 12 opening Block",
        TwistKind.SecondWind => "Second Wind: 8 Block after the next block clear",
        TwistKind.Reservist => "Reservist: 6 Block after each of the next two block clears",
        TwistKind.Shackling => "Shackling: temporarily reduce Strength by 1",
        TwistKind.Sapping => "Sapping: reduce Dexterity toward -1",
        TwistKind.Constricting => "Constricting: 2 Constrict, capped at 4",
        TwistKind.Eroding => "Eroding: 1 Disintegration, capped at 2",
        _ => throw new ArgumentOutOfRangeException()
    };
}

// Quarter-point units retain the previous solo +25% budget, now multiplied by party size.
internal sealed record EncounterPlan(int Budget, int Spent, int ReinforcementCount, EnemyTwist[] Twists)
{
    internal static int BaseBudget(int players, int tier) => (tier == 2 ? 2 : tier == 1 ? 3 : 4) * Math.Clamp(players, 1, 4);
    internal string[] Affixes => [.. Enumerable.Repeat("Reinforcement", ReinforcementCount), .. Twists.Select(t => $"enemy[{t.Target}] {t.Description}")];
    internal static bool IsHex(TwistKind k) => k is TwistKind.Echoing or TwistKind.Hexed or >= TwistKind.Venomous and <= TwistKind.Lacerating or >= TwistKind.Shackling;
    internal static bool IsOffense(TwistKind k) => k is TwistKind.Keen or TwistKind.Empowered or TwistKind.Cultist or TwistKind.Demonic or TwistKind.Furious or TwistKind.WoundUp;
    internal static bool IsShield(TwistKind k) => k is TwistKind.Buffered or TwistKind.Phased or TwistKind.Slippery;
    internal static bool IsBlock(TwistKind k) => k is TwistKind.Armored or TwistKind.Empowered or TwistKind.Entrenched or TwistKind.Agile or TwistKind.Fortified or TwistKind.SecondWind or TwistKind.Reservist;
    internal static int Cost(TwistKind k) => k switch
    {
        TwistKind.Sturdy => 4,
        TwistKind.Hexed or TwistKind.Cultist or TwistKind.Demonic or TwistKind.Entrenched or TwistKind.Phased or TwistKind.Reservist or TwistKind.Constricting or TwistKind.Eroding => 12,
        _ => 8
    };

    public static EncounterPlan Roll(Func<int, int> next, int players, int tier,
        int maxReinforcements, int enemyCount, bool forceReinforce = false)
    {
        players = Math.Clamp(players, 1, 4);
        int budget = BaseBudget(players, tier) * 5;
        int extra = 0;
        int capacity = tier == 0 ? Math.Clamp(Math.Min(maxReinforcements, 4 - enemyCount), 0, players >= 3 ? 2 : 1) : 0;
        if (capacity > 0 && (forceReinforce || next(100) < 50 + (players - 1) * 15))
        {
            extra = 1;
            if (capacity > 1 && next(2) == 0) extra++;
        }
        int remaining = budget - extra * 20;
        int totalEnemies = enemyCount + extra;
        var twists = new List<EnemyTwist>();
        var candidates = Enum.GetValues<TwistKind>().Where(k => k is not (TwistKind.Sturdy or TwistKind.Keen)).ToList();
        int limit = 3 * players - extra;
        for (int i = 0; i < limit; i++)
        {
            int[] Targets(TwistKind k) => Enumerable.Range(0, totalEnemies).Where(target =>
                !twists.Any(t => t.Target == target &&
                    ((IsHex(k) && t.IsHex) || (IsShield(k) && IsShield(t.Kind)) || (IsBlock(k) && IsBlock(t.Kind))))).ToArray();
            candidates.RemoveAll(k => Cost(k) > remaining ||
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
                TwistKind.Sturdy => 10 + next(9), TwistKind.Keen => 8 + next(9),
                TwistKind.Armored => 2 + next(3), TwistKind.Echoing or TwistKind.Hexed => 1 + next(3), _ => 1
            };
            twists.Add(new(kind, targetIndex, amount));
            remaining -= Cost(kind);
            if (i == 0) candidates.AddRange([TwistKind.Sturdy, TwistKind.Keen]);
        }
        return new(budget, budget - remaining, extra, twists.ToArray());
    }

    internal static string HexName(int hex) => hex switch { 1 => "Weak", 2 => "Frail", 3 => "Vulnerable", _ => "None" };
    internal static EncounterPlan BossDuo => new(0, 0, 0, []);
}
