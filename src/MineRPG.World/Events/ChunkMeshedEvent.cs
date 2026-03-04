using MineRPG.Core.Math;

namespace MineRPG.World.Events;

/// <summary>
/// Published when a chunk mesh has been built and is ready for rendering.
/// </summary>
public readonly struct ChunkMeshedEvent
{
    /// <summary>The coordinate of the meshed chunk.</summary>
    public ChunkCoord Coord { get; init; }
}
