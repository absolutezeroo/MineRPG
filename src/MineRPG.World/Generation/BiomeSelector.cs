using MineRPG.Core.Math;

namespace MineRPG.World.Generation;

/// <summary>
/// Selects a biome at a world (x,z) position using two noise channels:
/// temperature and humidity. Chooses the biome definition whose range
/// best matches the sampled values.
/// </summary>
public sealed class BiomeSelector(IReadOnlyList<BiomeDefinition> biomes, int seed)
{
    private readonly FastNoise _temperatureNoise = biomes.Count > 0
        ? new(seed ^ 0x12345678)
        : throw new InvalidOperationException("BiomeSelector requires at least one BiomeDefinition. Check Data/Biomes/.");
    private readonly FastNoise _humidityNoise = new(seed ^ unchecked((int)0x87654321));

    public BiomeDefinition Select(int worldX, int worldZ)
    {
        var temperature = (_temperatureNoise.Sample2D(worldX * 0.001f, worldZ * 0.001f) + 1f) * 0.5f;
        var humidity = (_humidityNoise.Sample2D(worldX * 0.001f, worldZ * 0.001f) + 1f) * 0.5f;

        BiomeDefinition? best = null;
        var bestScore = float.MaxValue;

        foreach (var biome in biomes)
        {
            if (temperature < biome.MinTemperature || temperature > biome.MaxTemperature)
                continue;
            if (humidity < biome.MinHumidity || humidity > biome.MaxHumidity)
                continue;

            var tCenter = (biome.MinTemperature + biome.MaxTemperature) * 0.5f;
            var hCenter = (biome.MinHumidity + biome.MaxHumidity) * 0.5f;
            var score = MathF.Abs(temperature - tCenter) + MathF.Abs(humidity - hCenter);
            if (score < bestScore)
            {
                bestScore = score;
                best = biome;
            }
        }

        return best ?? biomes[0];
    }

    /// <summary>
    /// Returns the primary and secondary biomes with a blend weight in [0, 1].
    /// Weight 0 = 100% primary. Used for smooth transitions at biome boundaries.
    /// </summary>
    public (BiomeDefinition Primary, BiomeDefinition Secondary, float BlendWeight) SelectWeighted(int worldX, int worldZ)
    {
        var temperature = (_temperatureNoise.Sample2D(worldX * 0.001f, worldZ * 0.001f) + 1f) * 0.5f;
        var humidity = (_humidityNoise.Sample2D(worldX * 0.001f, worldZ * 0.001f) + 1f) * 0.5f;

        BiomeDefinition? first = null;
        BiomeDefinition? second = null;
        var firstRank = float.MaxValue;
        var secondRank = float.MaxValue;
        var firstRawDist = float.MaxValue;
        var secondRawDist = float.MaxValue;

        foreach (var biome in biomes)
        {
            var tCenter = (biome.MinTemperature + biome.MaxTemperature) * 0.5f;
            var hCenter = (biome.MinHumidity + biome.MaxHumidity) * 0.5f;
            var rawDist = MathF.Abs(temperature - tCenter) + MathF.Abs(humidity - hCenter);

            // In-range biomes get priority for ranking only
            var inside = temperature >= biome.MinTemperature && temperature <= biome.MaxTemperature
                         && humidity >= biome.MinHumidity && humidity <= biome.MaxHumidity;
            var rank = inside ? rawDist * 0.5f : rawDist;

            if (rank < firstRank)
            {
                second = first;
                secondRank = firstRank;
                secondRawDist = firstRawDist;
                first = biome;
                firstRank = rank;
                firstRawDist = rawDist;
            }
            else if (rank < secondRank)
            {
                second = biome;
                secondRank = rank;
                secondRawDist = rawDist;
            }
        }

        first ??= biomes[0];
        second ??= first;

        // Blend weight uses raw (un-halved) distances for consistent behavior
        var distDiff = secondRawDist - firstRawDist;
        var blendWeight = 1f - Math.Clamp(distDiff * 4f, 0f, 1f);
        blendWeight *= blendWeight; // smooth quadratic falloff

        return (first, second, blendWeight);
    }
}
