using System.Collections.Generic;

namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Provides per-chunk debug information for the chunk map and tooltips.
/// Implemented by the chunk pipeline layer and consumed by debug UI.
/// </summary>
public interface IChunkDebugProvider
{
    /// <summary>
    /// Gets debug info for a specific chunk by coordinates.
    /// </summary>
    /// <param name="chunkX">The chunk X coordinate.</param>
    /// <param name="chunkZ">The chunk Z coordinate.</param>
    /// <param name="info">The debug info if the chunk exists.</param>
    /// <returns>True if the chunk was found, false otherwise.</returns>
    bool TryGetChunkDebugInfo(int chunkX, int chunkZ, out ChunkDebugInfo info);

    /// <summary>
    /// Gets the state index (for coloring) of all loaded chunks.
    /// Returns pairs of (chunkX, chunkZ, stateIndex) for efficient iteration.
    /// </summary>
    /// <param name="buffer">
    /// Pre-allocated buffer to fill. Each entry is (chunkX, chunkZ, stateIndex).
    /// </param>
    /// <returns>Number of entries written.</returns>
    int GetAllChunkStates(IList<ChunkStateEntry> buffer);
}
