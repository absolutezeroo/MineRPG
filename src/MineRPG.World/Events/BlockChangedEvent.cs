using MineRPG.World.Spatial;

namespace MineRPG.World.Events;

/// <summary>
/// Published when a block is placed or removed in the world.
/// </summary>
public readonly struct BlockChangedEvent
{
    /// <summary>The world position of the changed block.</summary>
    public WorldPosition Position { get; init; }

    /// <summary>The previous block ID at this position.</summary>
    public ushort OldBlockId { get; init; }

    /// <summary>The new block ID at this position.</summary>
    public ushort NewBlockId { get; init; }
}
