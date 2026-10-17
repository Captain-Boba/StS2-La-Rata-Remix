namespace LaRata;

// Pure policy. Runtime supplies the game's seeded Rng; tests can exercise the budget independently.
internal sealed record EncounterPlan(int Budget, int Spent, int HpPercent, int DamagePercent,
    int Plating, int Artifact, int BuffBonus, int DebuffBonus, int Hex, int HexAmount,
    bool Reinforcement, string[] Affixes)
{
    public static EncounterPlan Roll(Func<int, int> next, int players, int tier,
        bool canReinforce, bool forceReinforce = false)
    {
        int budget = tier == 2 ? 2 : tier == 1 ? 3 : 4 + (players >= 3 ? 1 : 0);
        if (canReinforce && (forceReinforce || (tier == 0 && next(4) == 0)))
            return new(budget, budget, 0, 0, 0, 0, 0, 0, 0, 0, true, ["Reinforcement"]);
        int hp = 0, damage = 0, plating = 0, artifact = 0, buff = 0, debuff = 0, hex = 0, hexAmount = 0;
        int remaining = budget;
        var names = new List<string>();
        // At least one tactical affix. No damage + Strength amplification in the same encounter.
        var candidates = new List<int> { 2, 3, 4, 5, 6 };
        int limit = next(3) + 1;
        for (int i = 0; i < limit && candidates.Count > 0; i++)
        {
            candidates.RemoveAll(x => Cost(x) > remaining || (x == 1 && buff > 0) || (x == 4 && damage > 0));
            if (candidates.Count == 0) break;
            int pick = candidates[next(candidates.Count)];
            candidates.Remove(pick);
            remaining -= Cost(pick);
            switch (pick)
            {
                case 0: hp = 8 + next(5); names.Add($"Sturdy: +{hp}% HP"); break;
                case 1: damage = 8 + next(5); names.Add($"Keen: +{damage}% base damage"); break;
                case 2: plating = 2 + next(3); names.Add($"Armored: {plating} Plating on the leader"); break;
                case 3: artifact = 1; names.Add("Warded: 1 Artifact on the leader"); break;
                case 4: buff = 1; plating = 2; names.Add("Empowered: 2 Plating, stronger minor buffs on the leader"); break;
                case 5: debuff = 1; hex = 1 + next(3); hexAmount = 1; names.Add("Echoing: extra hex and +1 to the leader's short debuffs"); break;
                case 6: hex = 1 + next(3); hexAmount = 1 + next(2); names.Add($"Hexed: {hexAmount} {HexName(hex)} on a telegraphed action"); break;
            }
            if (i == 0) candidates.AddRange([0, 1]);
            // Both variants affect the same move; never purchase it twice.
            if (pick is 5 or 6) { candidates.Remove(5); candidates.Remove(6); }
            if (pick is 2 or 4) { candidates.Remove(2); candidates.Remove(4); }
        }
        return new(budget, budget - remaining, hp, damage, plating, artifact, buff, debuff, hex, hexAmount, false, names.ToArray());
    }

    private static int Cost(int option) => option == 0 ? 1 : option == 6 ? 3 : 2;
    internal static string HexName(int hex) => hex switch { 1 => "Weak", 2 => "Frail", _ => "Vulnerable" };
    internal static EncounterPlan BossDuo => new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, ["Boss duo"]);
}
