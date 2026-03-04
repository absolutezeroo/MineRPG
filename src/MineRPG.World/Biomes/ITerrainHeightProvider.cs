using MineRPG.World.Biomes.Climate;

namespace MineRPG.World.Biomes;

/// <summary>
/// Provides terrain height from climate parameters.
/// Used by <see cref="BiomeBlender"/> to compute blended heights
/// without coupling to the full terrain shaper implementation.
/// </summary>
public interface ITerrainHeightProvider
{
    /// <summary>
    /// Computes the terrain height for the given climate parameters.
    /// </summary>
    /// <param name="parameters">The sampled climate parameters.</param>
    /// <returns>The terrain height in world Y coordinates.</returns>
    float GetHeight(in ClimateParameters parameters);
}
