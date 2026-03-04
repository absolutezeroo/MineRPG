using System.Collections.Concurrent;
using MineRPG.Core.Events;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.World.Events;

namespace MineRPG.World.Chunks;

/// <summary>
/// Central owner of all loaded ChunkEntry instances.
/// Thread-safe: reads and writes use ConcurrentDictionary.
/// </summary>
public sealed class ChunkManager(IEventBus eventBus, ILogger logger) : IChunkManager
{
    private readonly ConcurrentDictionary<ChunkCoord, ChunkEntry> _chunks = new();
    private readonly ILogger _logger = logger;

    public bool TryGet(ChunkCoord coord, out ChunkEntry? entry)
        => _chunks.TryGetValue(coord, out entry);

    public ChunkEntry GetOrCreate(ChunkCoord coord)
        => _chunks.GetOrAdd(coord, static c => new ChunkEntry(c));

    public void Remove(ChunkCoord coord)
    {
        if (_chunks.TryRemove(coord, out _))
        {
            _logger.Debug("Chunk unloaded: {0}", coord);
            eventBus.Publish(new ChunkUnloadedEvent { Coord = coord });
        }
    }

    public IEnumerable<ChunkEntry> GetAll() => _chunks.Values;

    public int Count => _chunks.Count;

    /// <summary>
    /// Return all chunk coords within Chebyshev distance of center,
    /// sorted by distance (nearest first) for load priority.
    /// </summary>
    public IReadOnlyList<ChunkCoord> GetCoordsInRange(ChunkCoord center, int renderDistance)
    {
        var result = new List<ChunkCoord>((renderDistance * 2 + 1) * (renderDistance * 2 + 1));
        for (var x = -renderDistance; x <= renderDistance; x++)
            for (var z = -renderDistance; z <= renderDistance; z++)
                result.Add(new ChunkCoord(center.X + x, center.Z + z));

        result.Sort((a, b) =>
            a.ChebyshevDistance(center).CompareTo(b.ChebyshevDistance(center)));
        return result;
    }

    /// <summary>
    /// Returns neighbor chunk data for the 4 cardinal directions.
    /// Index: [0]=+X(East), [1]=-X(West), [2]=+Z(South), [3]=-Z(North).
    /// </summary>
    public ChunkData?[] GetNeighborData(ChunkCoord coord)
    {
        var neighbors = new ChunkData?[4];
        ChunkCoord[] neighborCoords = [coord.East, coord.West, coord.South, coord.North];
        for (var i = 0; i < 4; i++)
        {
            if (_chunks.TryGetValue(neighborCoords[i], out var entry)
                && entry.State >= ChunkState.Generated)
            {
                neighbors[i] = entry.Data;
            }
        }

        return neighbors;
    }
}
