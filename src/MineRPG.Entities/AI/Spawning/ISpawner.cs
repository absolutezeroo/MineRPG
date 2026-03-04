using System;
using System.Collections.Generic;

namespace MineRPG.Entities.AI.Spawning;

/// <summary>
/// Evaluates spawn rules against world conditions and produces spawn requests.
/// Does not instantiate Godot nodes — that is handled by the bridge layer.
/// </summary>
public interface ISpawner
{
    /// <summary>
    /// Evaluate which mobs should spawn near the given world position.
    /// </summary>
    /// <param name="worldX">X coordinate in world space.</param>
    /// <param name="worldY">Y coordinate in world space.</param>
    /// <param name="worldZ">Z coordinate in world space.</param>
    /// <param name="currentBiome">The biome identifier at the spawn location.</param>
    /// <param name="lightLevel">The light level at the spawn location.</param>
    /// <param name="rng">Random number generator for spawn chance evaluation.</param>
    /// <returns>A list of spawn requests with mob IDs and spawn positions.</returns>
    IReadOnlyList<SpawnRequest> Evaluate(
        float worldX, float worldY, float worldZ,
        string currentBiome,
        int lightLevel,
        Random rng);
}
