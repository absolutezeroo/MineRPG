using MineRPG.Core.Math;

namespace MineRPG.World.Events;

/// <summary>
/// Published when terrain generation for a chunk is complete.
/// </summary>
public readonly struct ChunkGeneratedEvent
{
    /// <summary>The coordinate of the generated chunk.</summary>
    public ChunkCoord Coord { get; init; }
}
