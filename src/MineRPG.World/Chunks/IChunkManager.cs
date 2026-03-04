using System.Collections.Generic;

using MineRPG.Core.Math;

namespace MineRPG.World.Chunks;

/// <summary>
/// Central owner of all loaded ChunkEntry instances.
/// Thread-safe: implementations must support concurrent access.
/// </summary>
public interface IChunkManager
{
    /// <summary>Number of currently loaded chunks.</summary>
    public int Count { get; }

    /// <summary>
    /// Attempts to get a chunk entry by coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <param name="entry">The found entry, or null.</param>
    /// <returns>True if the chunk exists.</returns>
    public bool TryGet(ChunkCoord coord, out ChunkEntry? entry);

    /// <summary>
    /// Gets or creates a chunk entry for the given coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <returns>The existing or newly created chunk entry.</returns>
    public ChunkEntry GetOrCreate(ChunkCoord coord);

    /// <summary>
    /// Removes and unloads a chunk at the given coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate to remove.</param>
    public void Remove(ChunkCoord coord);

    /// <summary>
    /// Returns all currently loaded chunk entries.
    /// </summary>
    /// <returns>An enumerable of all chunk entries.</returns>
    public IEnumerable<ChunkEntry> GetAll();

    /// <summary>
    /// Return all chunk coords within Chebyshev distance of center,
    /// sorted by distance (nearest first) for load priority.
    /// </summary>
    /// <param name="center">The center chunk coordinate.</param>
    /// <param name="renderDistance">The render distance in chunks.</param>
    /// <returns>A sorted list of chunk coordinates.</returns>
    public IReadOnlyList<ChunkCoord> GetCoordsInRange(ChunkCoord center, int renderDistance);

    /// <summary>
    /// Returns neighbor chunk data for the 4 cardinal directions.
    /// </summary>
    /// <param name="coord">The center chunk coordinate.</param>
    /// <returns>An array of 4 neighbor chunk data references (nullable).</returns>
    public ChunkData?[] GetNeighborData(ChunkCoord coord);
}
