using MineRPG.Core.Math;

namespace MineRPG.World.Chunks;

/// <summary>
/// Persistent storage backend for chunk data.
/// Implementations handle actual I/O (file system, database, etc.).
/// </summary>
public interface IChunkStorage
{
    bool Exists(ChunkCoord coord);
    byte[] Load(ChunkCoord coord);
    void Save(ChunkCoord coord, byte[] data);
    void Delete(ChunkCoord coord);
}
