using MineRPG.Core.Math;

namespace MineRPG.World.Events;

/// <summary>
/// Published when a chunk is removed from the active set.
/// </summary>
public readonly struct ChunkUnloadedEvent
{
    /// <summary>The coordinate of the unloaded chunk.</summary>
    public ChunkCoord Coord { get; init; }
}
