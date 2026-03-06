namespace MineRPG.RPG.Loot;

/// <summary>
/// A single entry in a loot table with weight, count range, and conditions.
/// </summary>
public sealed class LootEntry
{
    /// <summary>
    /// The item ID to drop. Null or empty for empty entries (no drop).
    /// </summary>
    public string? ItemId { get; init; }

    /// <summary>Whether this is an empty entry (no item dropped).</summary>
    public bool IsEmpty => string.IsNullOrEmpty(ItemId);

    /// <summary>Relative weight for weighted random selection.</summary>
    public int Weight { get; init; }

    /// <summary>Minimum number of items to drop.</summary>
    public int MinCount { get; init; }

    /// <summary>Maximum number of items to drop.</summary>
    public int MaxCount { get; init; } = 1;

    /// <summary>Conditions that must be met for this entry to be eligible.</summary>
    public IReadOnlyList<LootCondition> Conditions { get; init; } = [];
}
