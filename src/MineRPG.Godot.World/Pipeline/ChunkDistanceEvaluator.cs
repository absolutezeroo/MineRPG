using System.Collections.Generic;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.Godot.World.Pipeline;

/// <summary>
/// Determines which chunks need loading and unloading based on the player's
/// current chunk position and the configured render distance.
/// Pure logic — no Godot dependency.
/// </summary>
internal sealed class ChunkDistanceEvaluator
{
    private readonly IChunkManager _chunkManager;

    /// <summary>
    /// Creates an evaluator backed by the given chunk manager.
    /// </summary>
    /// <param name="chunkManager">Chunk manager for range queries and iteration.</param>
    public ChunkDistanceEvaluator(IChunkManager chunkManager)
    {
        _chunkManager = chunkManager;
    }

    /// <summary>
    /// Computes the set of chunks that should be loaded (newly queued) and unloaded
    /// based on the player's current chunk position and render distance.
    /// </summary>
    /// <param name="center">The player's current chunk coordinate.</param>
    /// <param name="renderDistance">The render distance in chunks.</param>
    /// <param name="toLoad">Populated with chunk entries that need scheduling.</param>
    /// <param name="toUnload">Populated with chunk coordinates outside render distance.</param>
    public void Evaluate(
        ChunkCoord center,
        int renderDistance,
        List<ChunkEntry> toLoad,
        List<ChunkCoord> toUnload)
    {
        toLoad.Clear();
        toUnload.Clear();

        IReadOnlyList<ChunkCoord> needed = _chunkManager.GetCoordsInRange(center, renderDistance);
        HashSet<ChunkCoord> neededSet = new(needed);

        List<ChunkEntry> snapshot = new(_chunkManager.GetAll());

        foreach (ChunkEntry entry in snapshot)
        {
            if (!neededSet.Contains(entry.Coord))
            {
                toUnload.Add(entry.Coord);
            }
        }

        foreach (ChunkCoord coord in needed)
        {
            ChunkEntry entry = _chunkManager.GetOrCreate(coord);

            if (entry.State == ChunkState.Queued)
            {
                toLoad.Add(entry);
            }
        }
    }
}
