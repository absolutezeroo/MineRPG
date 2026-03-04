using MineRPG.Core.Math;

namespace MineRPG.World.Chunks;

/// <summary>
/// Persistent storage backend for chunk data.
/// Implementations handle actual I/O (file system, database, etc.).
/// </summary>
public interface IChunkStorage
{
    /// <summary>
    /// Checks whether a saved chunk exists at the given coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <returns>True if the chunk exists in storage.</returns>
    public bool Exists(ChunkCoord coord);

    /// <summary>
    /// Loads serialized chunk data from storage.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <returns>The raw serialized bytes.</returns>
    public byte[] Load(ChunkCoord coord);

    /// <summary>
    /// Saves serialized chunk data to storage.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <param name="data">The serialized bytes to save.</param>
    public void Save(ChunkCoord coord, byte[] data);

    /// <summary>
    /// Deletes a saved chunk from storage.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    public void Delete(ChunkCoord coord);
}
