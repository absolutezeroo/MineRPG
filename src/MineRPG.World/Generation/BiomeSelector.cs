using System;
using System.Collections.Generic;

using MineRPG.Core.Math;
using MineRPG.World.Biomes.Climate;

namespace MineRPG.World.Generation;

/// <summary>
/// Selects a biome at a world position using 6D climate parameters.
/// Uses squared Euclidean distance in climate space to find the closest match.
/// Falls back to legacy temperature/humidity matching for old biome definitions.
/// Thread-safe: all state is readonly after construction.
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
    private readonly FastNoise? _temperatureNoise;
    private readonly FastNoise? _humidityNoise;
    private readonly bool _hasClimateTargets;

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

        // Check if any biomes use the new 6D climate targets
        _hasClimateTargets = false;
        for (int i = 0; i < biomes.Count; i++)
        {
            if (biomes[i].HasClimateTarget)
            {
                _hasClimateTargets = true;
                break;
            }
        }

        // Only create legacy noise if needed
        if (!_hasClimateTargets)
        {
            _temperatureNoise = new FastNoise(seed ^ TemperatureSeedMask);
            _humidityNoise = new FastNoise(seed ^ HumiditySeedMask);
        }
    }

    /// <summary>
    /// Selects the best-matching biome for the given 6D climate parameters.
    /// </summary>
    /// <param name="parameters">The sampled climate parameters.</param>
    /// <returns>The biome definition with the smallest 6D distance.</returns>
    public BiomeDefinition Select(in ClimateParameters parameters)
    {
        if (!_hasClimateTargets)
        {
            return SelectLegacyFromClimate(in parameters);
        }

        BiomeDefinition? best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < _biomes.Count; i++)
        {
            BiomeDefinition biome = _biomes[i];

            if (!biome.HasClimateTarget)
            {
                continue;
            }

            float distance = biome.ClimateTarget.SquaredDistanceTo(in parameters);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = biome;
            }
        }

        return best ?? _biomes[0];
    }

    /// <summary>
    /// Returns the primary and secondary biomes with a blend weight for smooth transitions.
    /// </summary>
    /// <param name="parameters">The sampled climate parameters.</param>
    /// <returns>Primary biome, secondary biome, and blend weight in [0, 1].</returns>
    public (BiomeDefinition Primary, BiomeDefinition Secondary, float BlendWeight) SelectWeighted(
        in ClimateParameters parameters)
    {
        if (!_hasClimateTargets)
        {
            BiomeDefinition fallback = SelectLegacyFromClimate(in parameters);
            return (fallback, fallback, 0f);
        }

        BiomeDefinition? first = null;
        BiomeDefinition? second = null;
        float firstDistance = float.MaxValue;
        float secondDistance = float.MaxValue;

        for (int i = 0; i < _biomes.Count; i++)
        {
            BiomeDefinition biome = _biomes[i];

            if (!biome.HasClimateTarget)
            {
                continue;
            }

            float distance = biome.ClimateTarget.SquaredDistanceTo(in parameters);

            if (distance < firstDistance)
            {
                second = first;
                secondDistance = firstDistance;
                first = biome;
                firstDistance = distance;
            }
            else if (distance < secondDistance)
            {
                second = biome;
                secondDistance = distance;
            }
        }

        first ??= _biomes[0];
        second ??= first;

        float distanceDiff = MathF.Sqrt(secondDistance) - MathF.Sqrt(firstDistance);
        float blendWeight = 1f - Math.Clamp(distanceDiff * BlendDistanceMultiplier, 0f, 1f);
        blendWeight *= blendWeight;

        return (first, second, blendWeight);
    }

    /// <summary>
    /// Legacy: selects biome at world position using internal temperature/humidity noise.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>The best-matching biome definition.</returns>
    public BiomeDefinition Select(int worldX, int worldZ)
    {
        if (_temperatureNoise == null || _humidityNoise == null)
        {
            return _biomes[0];
        }

        float temperature = (_temperatureNoise.Sample2D(worldX * NoiseScale, worldZ * NoiseScale)
                             + NoiseNormalizationOffset) * NoiseNormalizationScale;
        float humidity = (_humidityNoise.Sample2D(worldX * NoiseScale, worldZ * NoiseScale)
                          + NoiseNormalizationOffset) * NoiseNormalizationScale;

        return SelectLegacyByTempHumidity(temperature, humidity);
    }

    /// <summary>
    /// Legacy: returns weighted biome pair at world position using internal noise.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>Primary, secondary biomes and blend weight.</returns>
    public (BiomeDefinition Primary, BiomeDefinition Secondary, float BlendWeight) SelectWeighted(
        int worldX, int worldZ)
    {
        if (_temperatureNoise == null || _humidityNoise == null)
        {
            return (_biomes[0], _biomes[0], 0f);
        }

        float temperature = (_temperatureNoise.Sample2D(worldX * NoiseScale, worldZ * NoiseScale)
                             + NoiseNormalizationOffset) * NoiseNormalizationScale;
        float humidity = (_humidityNoise.Sample2D(worldX * NoiseScale, worldZ * NoiseScale)
                          + NoiseNormalizationOffset) * NoiseNormalizationScale;

        return SelectWeightedLegacy(temperature, humidity);
    }

    private BiomeDefinition SelectLegacyFromClimate(in ClimateParameters parameters)
    {
        float temperature = (parameters.Temperature + 1f) * NoiseNormalizationScale;
        float humidity = (parameters.Humidity + 1f) * NoiseNormalizationScale;
        return SelectLegacyByTempHumidity(temperature, humidity);
    }

    private BiomeDefinition SelectLegacyByTempHumidity(float temperature, float humidity)
    {
        BiomeDefinition? best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < _biomes.Count; i++)
        {
            BiomeDefinition biome = _biomes[i];

            if (temperature < biome.MinTemperature || temperature > biome.MaxTemperature)
            {
                continue;
            }

            if (humidity < biome.MinHumidity || humidity > biome.MaxHumidity)
            {
                continue;
            }

            float tempCenter = (biome.MinTemperature + biome.MaxTemperature) * NoiseNormalizationScale;
            float humidCenter = (biome.MinHumidity + biome.MaxHumidity) * NoiseNormalizationScale;
            float score = MathF.Abs(temperature - tempCenter) + MathF.Abs(humidity - humidCenter);

            if (score < bestScore)
            {
                bestScore = score;
                best = biome;
            }
        }

        return best ?? _biomes[0];
    }

    private (BiomeDefinition Primary, BiomeDefinition Secondary, float BlendWeight) SelectWeightedLegacy(
        float temperature, float humidity)
    {
        BiomeDefinition? first = null;
        BiomeDefinition? second = null;
        float firstRank = float.MaxValue;
        float secondRank = float.MaxValue;
        float firstRawDistance = float.MaxValue;
        float secondRawDistance = float.MaxValue;

        for (int i = 0; i < _biomes.Count; i++)
        {
            BiomeDefinition biome = _biomes[i];
            float tempCenter = (biome.MinTemperature + biome.MaxTemperature) * NoiseNormalizationScale;
            float humidCenter = (biome.MinHumidity + biome.MaxHumidity) * NoiseNormalizationScale;
            float rawDistance = MathF.Abs(temperature - tempCenter) + MathF.Abs(humidity - humidCenter);

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

        float distanceDifference = secondRawDistance - firstRawDistance;
        float blendWeight = 1f - Math.Clamp(distanceDifference * BlendDistanceMultiplier, 0f, 1f);
        blendWeight *= blendWeight;

        return (first, second, blendWeight);
    }
}
