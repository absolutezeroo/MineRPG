namespace MineRPG.World.Generation;

/// <summary>
/// Computes the safe above-surface spawn position for new worlds.
/// Samples the terrain column at the canonical spawn block (8, 8)
/// and returns the surface Y raised by <see cref="SpawnHeightOffset"/>.
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

    private readonly TerrainSampler _terrainSampler;

    /// <summary>
    /// Initializes a new instance of <see cref="SpawnPositionResolver"/>.
    /// </summary>
    /// <param name="terrainSampler">Terrain sampler used to evaluate surface height.</param>
    public SpawnPositionResolver(TerrainSampler terrainSampler)
    {
        _terrainSampler = terrainSampler;
    }

    /// <summary>
    /// Computes the spawn Y coordinate for a new world.
    /// Samples the terrain column at world position (8, 8)
    /// and returns <c>SurfaceY + SpawnHeightOffset</c>.
    /// </summary>
    /// <returns>The Y coordinate at which the player should spawn.</returns>
    public int ComputeSpawnY()
    {
        TerrainColumn column = _terrainSampler.SampleColumn(SpawnWorldX, SpawnWorldZ);
        return column.SurfaceY + SpawnHeightOffset;
    }
}
