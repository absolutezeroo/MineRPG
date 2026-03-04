using MineRPG.Core.Math;
using MineRPG.World.Meshing;

namespace MineRPG.World.Chunks;

/// <summary>
/// Runtime container combining chunk data with its current lifecycle state.
/// </summary>
public sealed class ChunkEntry(ChunkCoord coord)
{
    public ChunkCoord Coord { get; } = coord;
    public ChunkData Data { get; } = new(coord);

    private volatile ChunkState _state = ChunkState.Queued;
    public ChunkState State => _state;

    public ChunkMeshResult? PendingMesh { get; set; }
    public bool HasCollision { get; set; }

    /// <summary>
    /// Set to true when a block is modified. Used by persistence to know
    /// which chunks need saving. Thread-safe via volatile.
    /// </summary>
    public volatile bool IsModified;

    /// <summary>
    /// Sub-chunk metadata computed after generation. Null until generation completes.
    /// Used by the mesher to skip empty/solid sub-chunks and by occlusion culling.
    /// </summary>
    public SubChunkInfo[]? SubChunks { get; set; }

    /// <summary>
    /// Highest Y coordinate with a non-air block. -1 if the chunk is all air.
    /// Computed after generation. Used for occlusion culling.
    /// </summary>
    public int HighestBlockY { get; set; } = -1;

    public void SetState(ChunkState state) => _state = state;

    /// <summary>
    /// Recomputes sub-chunk metadata and highest block Y.
    /// Call after generation or after block modifications.
    /// </summary>
    public void RecomputeSubChunkInfo()
    {
        SubChunks = Data.ComputeSubChunkInfo();
        HighestBlockY = Data.GetHighestNonAirY();
    }
}
