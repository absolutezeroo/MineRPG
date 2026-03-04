using System;
using System.Collections.Generic;

using MineRPG.Core.Math;

namespace MineRPG.World.Generation;

/// <summary>
/// Selects a biome at a world (x,z) position using two noise channels:
/// temperature and humidity. Chooses the biome definition whose range
/// best matches the sampled values.
/// </summary>
public sealed class BiomeSelector
{
    private const float NoiseScale = 0.001f;
    private const float NoiseNormalizationOffset = 1f;
    private const float NoiseNormalizationScale = 0.5f;
    private const int TemperatureSeedMask = 0x12345678;
    private const float InRangeRankMultiplier = 0.5f;
    private const float BlendDistanceMultiplier = 4f;

    private static readonly int HumiditySeedMask = unchecked((int)0x87654321);

    private readonly IReadOnlyList<BiomeDefinition> _biomes;
    private readonly FastNoise _temperatureNoise;
    private readonly FastNoise _humidityNoise;

    /// <summary>
    /// Creates a biome selector with the given definitions and world seed.
    /// </summary>
    /// <param name="biomes">Available biome definitions (must contain at least one).</param>
    /// <param name="seed">World seed for noise generation.</param>
    public BiomeSelector(IReadOnlyList<BiomeDefinition> biomes, int seed)
    {
        if (biomes.Count == 0)
        {
            throw new InvalidOperationException(
                "BiomeSelector requires at least one BiomeDefinition. Check Data/Biomes/.");
        }

        _biomes = biomes;
        _temperatureNoise = new FastNoise(seed ^ TemperatureSeedMask);
        _humidityNoise = new FastNoise(seed ^ HumiditySeedMask);
    }

    /// <summary>
    /// Selects the best-matching biome for the given world position.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>The best-matching biome definition.</returns>
    public BiomeDefinition Select(int worldX, int worldZ)
    {
        float temperature = (_temperatureNoise.Sample2D(worldX * NoiseScale, worldZ * NoiseScale)
                             + NoiseNormalizationOffset) * NoiseNormalizationScale;
        float humidity = (_humidityNoise.Sample2D(worldX * NoiseScale, worldZ * NoiseScale)
                          + NoiseNormalizationOffset) * NoiseNormalizationScale;

        BiomeDefinition? best = null;
        float bestScore = float.MaxValue;

        foreach (BiomeDefinition biome in _biomes)
        {
            if (temperature < biome.MinTemperature || temperature > biome.MaxTemperature)
            {
                continue;
            }

            if (humidity < biome.MinHumidity || humidity > biome.MaxHumidity)
            {
                continue;
            }

            float temperatureCenter = (biome.MinTemperature + biome.MaxTemperature) * NoiseNormalizationScale;
            float humidityCenter = (biome.MinHumidity + biome.MaxHumidity) * NoiseNormalizationScale;
            float score = MathF.Abs(temperature - temperatureCenter) + MathF.Abs(humidity - humidityCenter);

            if (score < bestScore)
            {
                bestScore = score;
                best = biome;
            }
        }

        return best ?? _biomes[0];
    }

    /// <summary>
    /// Returns the primary and secondary biomes with a blend weight in [0, 1].
    /// Weight 0 = 100% primary. Used for smooth transitions at biome boundaries.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>A tuple of (Primary, Secondary, BlendWeight).</returns>
    public (BiomeDefinition Primary, BiomeDefinition Secondary, float BlendWeight) SelectWeighted(
        int worldX, int worldZ)
    {
        float temperature = (_temperatureNoise.Sample2D(worldX * NoiseScale, worldZ * NoiseScale)
                             + NoiseNormalizationOffset) * NoiseNormalizationScale;
        float humidity = (_humidityNoise.Sample2D(worldX * NoiseScale, worldZ * NoiseScale)
                          + NoiseNormalizationOffset) * NoiseNormalizationScale;

        BiomeDefinition? first = null;
        BiomeDefinition? second = null;
        float firstRank = float.MaxValue;
        float secondRank = float.MaxValue;
        float firstRawDistance = float.MaxValue;
        float secondRawDistance = float.MaxValue;

        foreach (BiomeDefinition biome in _biomes)
        {
            float temperatureCenter = (biome.MinTemperature + biome.MaxTemperature) * NoiseNormalizationScale;
            float humidityCenter = (biome.MinHumidity + biome.MaxHumidity) * NoiseNormalizationScale;
            float rawDistance = MathF.Abs(temperature - temperatureCenter)
                                + MathF.Abs(humidity - humidityCenter);

            // In-range biomes get priority for ranking only
            bool isInside = temperature >= biome.MinTemperature && temperature <= biome.MaxTemperature
                            && humidity >= biome.MinHumidity && humidity <= biome.MaxHumidity;
            float rank = isInside ? rawDistance * InRangeRankMultiplier : rawDistance;

            if (rank < firstRank)
            {
                second = first;
                secondRank = firstRank;
                secondRawDistance = firstRawDistance;
                first = biome;
                firstRank = rank;
                firstRawDistance = rawDistance;
            }
            else if (rank < secondRank)
            {
                second = biome;
                secondRank = rank;
                secondRawDistance = rawDistance;
            }
        }

        first ??= _biomes[0];
        second ??= first;

        // Blend weight uses raw (un-halved) distances for consistent behavior
        float distanceDifference = secondRawDistance - firstRawDistance;
        float blendWeight = 1f - Math.Clamp(distanceDifference * BlendDistanceMultiplier, 0f, 1f);
        blendWeight *= blendWeight; // smooth quadratic falloff

        return (first, second, blendWeight);
    }
}
