using MineRPG.Core.Math;

namespace MineRPG.World.Events;

/// <summary>
/// Published when a chunk has been persisted to storage.
/// </summary>
public readonly struct ChunkSavedEvent
{
    /// <summary>The coordinate of the saved chunk.</summary>
    public ChunkCoord Coord { get; init; }

    /// <summary>The serialized size in bytes.</summary>
    public int ByteSize { get; init; }
}
