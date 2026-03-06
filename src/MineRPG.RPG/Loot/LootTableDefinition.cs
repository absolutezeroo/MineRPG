namespace MineRPG.RPG.Loot;

/// <summary>
/// Data-driven loot table definition loaded from Data/Loot/*.json.
/// Defines the entries, roll range, and bonus rolls for loot generation.
/// </summary>
public sealed class LootTableDefinition
{
    /// <summary>Unique identifier for this loot table.</summary>
    public string LootTableId { get; init; } = "";

    /// <summary>Minimum number of rolls on this table.</summary>
    public int MinRolls { get; init; } = 1;

    /// <summary>Maximum number of rolls on this table.</summary>
    public int MaxRolls { get; init; } = 1;

    /// <summary>Weighted entries that can be selected on each roll.</summary>
    public IReadOnlyList<LootEntry> Entries { get; init; } = [];

    /// <summary>Additional rolls per looting level.</summary>
    public int BonusRollsPerLootingLevel { get; init; }
}
