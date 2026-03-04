using MineRPG.World.Chunks;

namespace MineRPG.World.Meshing;

/// <summary>
/// Builds mesh data from chunk block data and its neighbors.
/// Implementations must be thread-safe.
/// </summary>
public interface IChunkMeshBuilder
{
    ChunkMeshResult Build(ChunkData chunk, ChunkData?[] neighbors);
}
