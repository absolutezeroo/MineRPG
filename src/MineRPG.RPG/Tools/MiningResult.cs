namespace MineRPG.RPG.Tools;

/// <summary>
/// Result of a mining calculation, describing timing, drops, and durability cost.
/// </summary>
public readonly struct MiningResult
{
    /// <summary>Time in seconds required to break the block.</summary>
    public float BreakTimeSeconds { get; init; }

    /// <summary>Whether the block can be harvested with the current tool.</summary>
    public bool CanHarvest { get; init; }

    /// <summary>Item ID that is dropped when the block is broken.</summary>
    public string? DropItemId { get; init; }

    /// <summary>Number of items dropped.</summary>
    public int DropCount { get; init; }

    /// <summary>Experience points gained from breaking the block.</summary>
    public int ExperienceDrop { get; init; }

    /// <summary>Durability damage dealt to the tool (typically 1 per block).</summary>
    public int DurabilityDamage { get; init; }
}
