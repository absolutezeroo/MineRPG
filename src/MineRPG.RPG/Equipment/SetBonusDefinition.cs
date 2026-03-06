namespace MineRPG.RPG.Equipment;

/// <summary>
/// Data-driven definition of an equipment set with tiered bonuses.
/// Loaded from Data/Equipment/*.json.
/// </summary>
public sealed class SetBonusDefinition
{
    /// <summary>Unique identifier for this equipment set.</summary>
    public string SetId { get; init; } = "";

    /// <summary>Display name of the set.</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>Item IDs that belong to this set.</summary>
    public IReadOnlyList<string> Pieces { get; init; } = [];

    /// <summary>Tiered bonuses that activate at different piece counts.</summary>
    public IReadOnlyList<SetBonus> Bonuses { get; init; } = [];
}
