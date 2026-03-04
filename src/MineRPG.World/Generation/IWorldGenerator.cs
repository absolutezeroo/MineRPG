using MineRPG.World.Chunks;

namespace MineRPG.World.Generation;

/// <summary>
/// Generates terrain data for a chunk. Implementations must be thread-safe.
/// </summary>
public interface IWorldGenerator
{
    void Generate(ChunkEntry entry, CancellationToken cancellationToken);
}
