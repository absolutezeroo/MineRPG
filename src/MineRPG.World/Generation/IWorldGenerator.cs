using System.Threading;

using MineRPG.World.Chunks;

namespace MineRPG.World.Generation;

/// <summary>
/// Generates terrain data for a chunk. Implementations must be thread-safe.
/// </summary>
public interface IWorldGenerator
{
    /// <summary>
    /// Generates terrain blocks for the given chunk entry.
    /// </summary>
    /// <param name="entry">The chunk entry to populate with terrain data.</param>
    /// <param name="cancellationToken">Token to cancel the generation if no longer needed.</param>
    public void Generate(ChunkEntry entry, CancellationToken cancellationToken);
}
