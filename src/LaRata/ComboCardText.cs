namespace LaRata;

internal static class ComboCardText
{
    internal static List<(string, string)> For(string artKey) => artKey switch
    {
        "ser_kevs_thorn_buster" => Text("Ser Kev's Thorn Buster", "Remove Thorns from ALL enemies. Draw {Cards:diff()} cards."),
        "gutter_feint" => Text("Gutter Feint", "Deal {Damage:diff()} damage. Gain {Block:diff()} Block."),
        "ragpickers_guard" => Text("Ragpicker's Guard", "Discard 1 card. Gain {Block:diff()} Block."),
        "cinder_receipt" => Text("Cinder Receipt", "Exhaust 1 card in your hand. Gain {VigorPower:diff()} Vigor."),
        "venomous_encore" => Text("Venomous Encore", "Deal {Damage:diff()} damage twice. If the enemy has Poison, apply {DoomPower:diff()} Doom."),
        "graveyard_shift" => Text("Graveyard Shift", "Lose 2 HP. Draw {Cards:diff()} cards."),
        "scrap_coronation" => Text("Scrap Coronation", "Exhaust 1 card in your hand. Forge {Forge:diff()}."),
        "cold_circuit" => Text("Cold Circuit", "Discard 1 card. Channel 1 Frost. Draw {Cards:diff()} cards."),
        "borrowed_backbone" => Text("Borrowed Backbone", "Gain {Block:diff()} Block. Apply {DoomPower:diff()} Doom."),
        "smugglers_dividend" => Text("Smuggler's Dividend", "The first time you discard a card during your turn, gain {VigorPower:diff()} Vigor."),
        "court_of_cinders" => Text("Court of Cinders", "The first time you Exhaust a card during your turn, gain 1 Energy and apply {PoisonPower:diff()} Poison to ALL enemies. Additional copies increase the Poison, but not the Energy."),
        _ => throw new ArgumentOutOfRangeException(nameof(artKey), artKey, "Unknown La Rata card text.")
    };

    internal static List<(string, string)> ForPower(string key) => key switch
    {
        "smugglers_dividend" => Text("Smuggler's Dividend", "The first time you discard a card during your turn, gain {Amount} Vigor."),
        "court_of_cinders" => Text("Court of Cinders", "The first time you Exhaust a card during your turn, gain 1 Energy and apply {Amount} Poison to ALL enemies. Additional copies increase the Poison, but not the Energy."),
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "Unknown La Rata power text.")
    };

    private static List<(string, string)> Text(string title, string description) =>
        [("title", title), ("description", description)];
}
