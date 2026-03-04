namespace MineRPG.World.Generation;

/// <summary>
/// Determines whether a given underground voxel should be carved into a cave.
/// Delegates all noise sampling to <see cref="TerrainSampler"/>.
/// Thread-safe.
/// </summary>
public sealed class CaveCarver(TerrainSampler sampler)
{
    private readonly TerrainSampler _sampler = sampler ?? throw new ArgumentNullException(nameof(sampler));

    /// <summary>
    /// Returns true if the voxel at (worldX, worldY, worldZ) should be carved.
    /// Never carves bedrock (y==0) or blocks near the surface.
    /// </summary>
    public bool ShouldCarve(int worldX, int worldY, int worldZ, int surfaceY, float continentalness)
    {
        if (worldY <= 0 || worldY >= surfaceY - 4)
            return false;

        var density = _sampler.SampleCaveDensity(worldX, worldY, worldZ, surfaceY, continentalness);
        return density < 0f;
    }
}
