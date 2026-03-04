using MineRPG.World.Chunks;

namespace MineRPG.World.Meshing;

/// <summary>
/// Builds mesh data from chunk block data and its neighbors.
/// Implementations must be thread-safe.
/// </summary>
public interface IChunkMeshBuilder
{
    /// <summary>
    /// Builds mesh data for a chunk.
    /// </summary>
    /// <param name="chunk">The chunk data to mesh.</param>
    /// <param name="neighbors">Data from the 4 cardinal neighbor chunks.</param>
    /// <returns>Separate mesh data for opaque and liquid surfaces.</returns>
    public ChunkMeshResult Build(ChunkData chunk, ChunkData?[] neighbors);
}
