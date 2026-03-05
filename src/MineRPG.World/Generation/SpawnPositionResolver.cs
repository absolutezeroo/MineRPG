using System;

using MineRPG.World.Chunks;

namespace MineRPG.World.Generation;

/// <summary>
/// Computes the safe above-surface spawn position for new worlds.
/// Samples the terrain column at the canonical spawn block (8, 8)
/// and returns the surface Y raised by <see cref="SpawnHeightOffset"/>.
/// Clamps to valid chunk bounds to prevent out-of-range spawns.
/// Thread-safe: all state is readonly after construction.
/// </summary>
public sealed class SpawnPositionResolver
{
    /// <summary>The default world X coordinate for player spawn.</summary>
    public const int SpawnWorldX = 8;

    /// <summary>The default world Z coordinate for player spawn.</summary>
    public const int SpawnWorldZ = 8;

    /// <summary>Number of blocks above the surface to place the player.</summary>
    public const int SpawnHeightOffset = 2;

    /// <summary>Fallback Y when terrain sampling returns an invalid surface.</summary>
    public const int FallbackSpawnY = 66;

    private readonly TerrainSampler _terrainSampler;

    /// <summary>
    /// Initializes a new instance of <see cref="SpawnPositionResolver"/>.
    /// </summary>
    /// <param name="terrainSampler">Terrain sampler used to evaluate surface height.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="terrainSampler"/> is null.</exception>
    public SpawnPositionResolver(TerrainSampler terrainSampler)
    {
        _terrainSampler = terrainSampler ?? throw new ArgumentNullException(nameof(terrainSampler));
    }

    /// <summary>
    /// Computes the spawn Y coordinate for a new world.
    /// Samples the terrain column at world position (8, 8)
    /// and returns <c>SurfaceY + SpawnHeightOffset</c>.
    /// Returns <see cref="FallbackSpawnY"/> if the surface is invalid.
    /// </summary>
    /// <returns>The Y coordinate at which the player should spawn.</returns>
    public int ComputeSpawnY()
    {
        TerrainColumn column = _terrainSampler.SampleColumn(SpawnWorldX, SpawnWorldZ);
        int spawnY = column.SurfaceY + SpawnHeightOffset;

        // Guard against underground/void/overflow spawns
        if (spawnY < 0 || spawnY >= ChunkData.SizeY)
        {
            return FallbackSpawnY;
        }

        return spawnY;
    }
}
