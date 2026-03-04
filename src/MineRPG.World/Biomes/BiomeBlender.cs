using System;
using System.Runtime.CompilerServices;

using MineRPG.World.Biomes.Climate;
using MineRPG.World.Generation;

namespace MineRPG.World.Biomes;

/// <summary>
/// Blends terrain values at biome boundaries for smooth transitions.
/// Samples neighboring positions and interpolates heights and surface blocks
/// weighted by their distance in climate parameter space.
/// Thread-safe: stateless, all inputs are provided per call.
/// </summary>
public sealed class BiomeBlender
{
    private const int DefaultBlendRadius = 4;
    private const int BlendSampleStep = 2;

    private readonly int _blendRadius;

    /// <summary>
    /// Creates a biome blender with the given blend radius.
    /// </summary>
    /// <param name="blendRadius">Radius in blocks for boundary blending.</param>
    public BiomeBlender(int blendRadius = DefaultBlendRadius)
    {
        _blendRadius = blendRadius;
    }

    /// <summary>
    /// Computes a blended terrain height at (worldX, worldZ) by sampling neighboring columns.
    /// Only performs blending when the position is near a biome boundary.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <param name="centerHeight">The unblended height at this position.</param>
    /// <param name="centerBiome">The biome at this position.</param>
    /// <param name="climateSampler">Climate sampler for neighboring positions.</param>
    /// <param name="biomeSelector">Biome selector for neighboring positions.</param>
    /// <param name="terrainShaper">Terrain shaper for computing heights.</param>
    /// <returns>The blended terrain height.</returns>
    public float BlendHeight(
        int worldX,
        int worldZ,
        float centerHeight,
        BiomeDefinition centerBiome,
        ClimateSampler climateSampler,
        BiomeSelector biomeSelector,
        ITerrainHeightProvider terrainShaper)
    {
        float totalWeight = 1f;
        float weightedHeight = centerHeight;

        for (int dx = -_blendRadius; dx <= _blendRadius; dx += BlendSampleStep)
        {
            for (int dz = -_blendRadius; dz <= _blendRadius; dz += BlendSampleStep)
            {
                if (dx == 0 && dz == 0)
                {
                    continue;
                }

                int sampleX = worldX + dx;
                int sampleZ = worldZ + dz;

                ClimateParameters neighborClimate = climateSampler.SampleSurface(sampleX, sampleZ);
                BiomeDefinition neighborBiome = biomeSelector.Select(in neighborClimate);

                // Only blend if the neighbor has a different biome
                if (string.Equals(neighborBiome.Id, centerBiome.Id, StringComparison.Ordinal))
                {
                    continue;
                }

                float neighborHeight = terrainShaper.GetHeight(in neighborClimate);
                float distance = MathF.Sqrt(dx * dx + dz * dz);
                float weight = 1f - Math.Clamp(distance / _blendRadius, 0f, 1f);
                weight *= weight; // Quadratic falloff

                weightedHeight += neighborHeight * weight;
                totalWeight += weight;
            }
        }

        return weightedHeight / totalWeight;
    }
}
