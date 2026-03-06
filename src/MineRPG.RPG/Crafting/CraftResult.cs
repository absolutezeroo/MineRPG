using MineRPG.RPG.Items;

namespace MineRPG.RPG.Crafting;

/// <summary>
/// Result of executing a crafting operation.
/// </summary>
public readonly struct CraftResult
{
    /// <summary>Whether the crafting operation succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>The crafted item instance. Null on failure.</summary>
    public ItemInstance? ResultItem { get; init; }

    /// <summary>Experience reward for completing the craft.</summary>
    public float ExperienceReward { get; init; }

    /// <summary>Reason for failure, or null on success.</summary>
    public string? FailReason { get; init; }
}
