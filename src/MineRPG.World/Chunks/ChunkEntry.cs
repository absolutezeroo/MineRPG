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

    public void SetState(ChunkState state) => _state = state;
}
