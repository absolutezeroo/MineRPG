using System.Collections.Concurrent;
using System.Collections.Generic;

using MineRPG.Core.Events;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.World.Events;

namespace MineRPG.World.Chunks;

/// <summary>
/// Central owner of all loaded ChunkEntry instances.
/// Thread-safe: reads and writes use ConcurrentDictionary.
/// </summary>
public sealed class ChunkManager : IChunkManager
{
    private const int NeighborCount = 4;
    private const int EastIndex = 0;
    private const int WestIndex = 1;
    private const int SouthIndex = 2;
    private const int NorthIndex = 3;

    private readonly ConcurrentDictionary<ChunkCoord, ChunkEntry> _chunks = new();
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new chunk manager.
    /// </summary>
    /// <param name="eventBus">Event bus for publishing chunk events.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ChunkManager(IEventBus eventBus, ILogger logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>Number of currently loaded chunks.</summary>
    public int Count => _chunks.Count;

    /// <summary>
    /// Attempts to get a chunk entry by coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <param name="entry">The found entry, or null.</param>
    /// <returns>True if the chunk exists.</returns>
    public bool TryGet(ChunkCoord coord, out ChunkEntry? entry)
        => _chunks.TryGetValue(coord, out entry);

    /// <summary>
    /// Gets or creates a chunk entry for the given coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <returns>The existing or newly created chunk entry.</returns>
    public ChunkEntry GetOrCreate(ChunkCoord coord)
        => _chunks.GetOrAdd(coord, static c => new ChunkEntry(c));

    /// <summary>
    /// Removes and unloads a chunk at the given coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate to remove.</param>
    public void Remove(ChunkCoord coord)
    {
        if (_chunks.TryRemove(coord, out _))
        {
            _logger.Debug("Chunk unloaded: {0}", coord);
            _eventBus.Publish(new ChunkUnloadedEvent { Coord = coord });
        }
    }

    /// <summary>
    /// Returns all currently loaded chunk entries.
    /// </summary>
    /// <returns>An enumerable of all chunk entries.</returns>
    public IEnumerable<ChunkEntry> GetAll() => _chunks.Values;

    /// <summary>
    /// Return all chunk coords within Chebyshev distance of center,
    /// sorted by distance (nearest first) for load priority.
    /// </summary>
    /// <param name="center">The center chunk coordinate.</param>
    /// <param name="renderDistance">The render distance in chunks.</param>
    /// <returns>A sorted list of chunk coordinates.</returns>
    public IReadOnlyList<ChunkCoord> GetCoordsInRange(ChunkCoord center, int renderDistance)
    {
        int sideLength = renderDistance * 2 + 1;
        List<ChunkCoord> result = new(sideLength * sideLength);

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                result.Add(new ChunkCoord(center.X + x, center.Z + z));
            }
        }

        result.Sort((a, b) =>
            a.ChebyshevDistance(center).CompareTo(b.ChebyshevDistance(center)));
        return result;
    }

    /// <summary>
    /// Returns neighbor chunk data for the 4 cardinal directions.
    /// Index: [0]=+X(East), [1]=-X(West), [2]=+Z(South), [3]=-Z(North).
    /// </summary>
    /// <param name="coord">The center chunk coordinate.</param>
    /// <returns>An array of 4 neighbor chunk data references (nullable).</returns>
    public ChunkData?[] GetNeighborData(ChunkCoord coord)
    {
        ChunkData?[] neighbors = new ChunkData?[NeighborCount];
        ChunkCoord[] neighborCoords = [coord.East, coord.West, coord.South, coord.North];

        for (int i = 0; i < NeighborCount; i++)
        {
            if (_chunks.TryGetValue(neighborCoords[i], out ChunkEntry? entry)
                && entry.State >= ChunkState.Generated)
            {
                neighbors[i] = entry.Data;
            }
        }

        return neighbors;
    }
}
