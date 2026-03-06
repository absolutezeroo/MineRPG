namespace MineRPG.RPG.Loot;

/// <summary>
/// A condition that must be met for a loot entry to be eligible.
/// </summary>
public sealed class LootCondition
{
    /// <summary>Type of condition (e.g., "randomChanceWithLooting").</summary>
    public string Type { get; init; } = "";

    /// <summary>Base probability for random chance conditions (0.0 to 1.0).</summary>
    public float Chance { get; init; }

    /// <summary>Additional chance per looting level.</summary>
    public float LootingMultiplier { get; init; }
}
