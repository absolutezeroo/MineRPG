namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published every physics frame while the player is mining a block,
/// and once more when mining is cancelled with IsActive set to false.
/// Subscribers use this to update visual crack overlays.
/// </summary>
public readonly struct MiningProgressChangedEvent
{
    /// <summary>World X coordinate of the block being mined.</summary>
    public int X { get; init; }

    /// <summary>World Y coordinate of the block being mined.</summary>
    public int Y { get; init; }

    /// <summary>World Z coordinate of the block being mined.</summary>
    public int Z { get; init; }

    /// <summary>Mining progress from 0.0 to 1.0.</summary>
    public float Progress { get; init; }

    /// <summary>Visual crack stage from 0 (no crack) to 10 (fully broken).</summary>
    public int CrackStage { get; init; }

    /// <summary>
    /// False when mining was cancelled (player looked away, moved, released attack).
    /// Subscribers should hide the overlay when this is false.
    /// </summary>
    public bool IsActive { get; init; }
}
