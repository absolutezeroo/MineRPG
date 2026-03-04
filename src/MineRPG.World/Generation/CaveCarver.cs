using System;

namespace MineRPG.World.Generation;

/// <summary>
/// Determines whether a given underground voxel should be carved into a cave.
/// Delegates all noise sampling to <see cref="TerrainSampler"/>.
/// Thread-safe.
/// </summary>
public sealed class CaveCarver
{
    private const int BedrockY = 0;
    private const int SurfaceMargin = 4;

    private readonly TerrainSampler _sampler;

    /// <summary>
    /// Creates a cave carver backed by the given terrain sampler.
    /// </summary>
    /// <param name="sampler">Terrain sampler providing cave density noise.</param>
    public CaveCarver(TerrainSampler sampler)
    {
        _sampler = sampler ?? throw new ArgumentNullException(nameof(sampler));
    }

    /// <summary>
    /// Returns true if the voxel at (worldX, worldY, worldZ) should be carved.
    /// Never carves bedrock (y==0) or blocks near the surface.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldY">World Y coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <param name="surfaceY">Surface Y height at this column.</param>
    /// <param name="continentalness">Continentalness noise value for ocean suppression.</param>
    /// <returns>True if the voxel should be carved out.</returns>
    public bool ShouldCarve(int worldX, int worldY, int worldZ, int surfaceY, float continentalness)
    {
        if (worldY <= BedrockY || worldY >= surfaceY - SurfaceMargin)
        {
            return false;
        }

        float density = _sampler.SampleCaveDensity(worldX, worldY, worldZ, surfaceY, continentalness);
        return density < 0f;
    }
}
