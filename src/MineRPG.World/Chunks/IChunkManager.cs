using MineRPG.Core.Math;

namespace MineRPG.World.Chunks;

/// <summary>
/// Central owner of all loaded ChunkEntry instances.
/// Thread-safe: implementations must support concurrent access.
/// </summary>
public interface IChunkManager
{
    bool TryGet(ChunkCoord coord, out ChunkEntry? entry);
    ChunkEntry GetOrCreate(ChunkCoord coord);
    void Remove(ChunkCoord coord);
    IEnumerable<ChunkEntry> GetAll();
    int Count { get; }
    IReadOnlyList<ChunkCoord> GetCoordsInRange(ChunkCoord center, int renderDistance);
    ChunkData?[] GetNeighborData(ChunkCoord coord);
}
