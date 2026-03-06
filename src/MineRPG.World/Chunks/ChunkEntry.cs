using System.Threading;

using MineRPG.Core.Math;
using MineRPG.World.Meshing;
using MineRPG.World.Spatial;

namespace MineRPG.World.Chunks;

/// <summary>
/// Runtime container combining chunk data with its current lifecycle state.
/// </summary>
public sealed class ChunkEntry
{
    private volatile ChunkState _state = ChunkState.Queued;

    /// <summary>The chunk coordinate in the world grid.</summary>
    public ChunkCoord Coord { get; }

    /// <summary>The block data for this chunk.</summary>
    public ChunkData Data { get; }

    /// <summary>Current lifecycle state of the chunk.</summary>
    public ChunkState State => _state;

    /// <summary>
    /// Pending mesh result waiting to be applied on the main thread.
    /// Written by background workers, read by main thread - volatile for cross-thread visibility.
    /// </summary>
    public volatile ChunkMeshResult? PendingMesh;

    /// <summary>Whether collision shapes have been built for this chunk.</summary>
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

    /// <summary>
    /// Face-to-face visibility matrix for BFS occlusion culling.
    /// Describes which faces of this chunk can see which other faces.
    /// Computed after generation/remesh on background threads.
    /// </summary>
    public ChunkVisibilityMatrix? VisibilityMatrix { get; set; }

    /// <summary>
    /// Current LOD level for this chunk. LOD 0 = full detail.
    /// Updated when the player moves, compared via hysteresis.
    /// Volatile for cross-thread visibility (written by main thread, read by workers).
    /// </summary>
    public volatile LodLevel CurrentLod = LodLevel.Lod0;

    /// <summary>
    /// Last applied mesh result, kept for region batching rebuilds.
    /// Null until the first mesh is applied. Written on main thread only.
    /// </summary>
    public ChunkMeshResult? LastMeshResult { get; set; }

    /// <summary>
    /// Creates a new chunk entry for the given coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    public ChunkEntry(ChunkCoord coord)
    {
        Coord = coord;
        Data = new ChunkData(coord);
    }

    /// <summary>
    /// Updates the chunk lifecycle state.
    /// </summary>
    /// <param name="state">The new state.</param>
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
