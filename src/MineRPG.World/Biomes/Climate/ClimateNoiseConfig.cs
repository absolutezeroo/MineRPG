using System.Collections.Generic;

using MineRPG.World.Noise;

using Newtonsoft.Json;

namespace MineRPG.World.Biomes.Climate;

/// <summary>
/// Data-driven configuration for all climate noise channels.
/// Loaded from Data/WorldGen/noise_settings.json.
/// </summary>
public sealed class ClimateNoiseConfig
{
    /// <summary>Noise settings for the continentalness channel.</summary>
    [JsonProperty("continentalness")]
    public NoiseSettings Continentalness { get; init; } = new();

    /// <summary>Noise settings for the erosion channel.</summary>
    [JsonProperty("erosion")]
    public NoiseSettings Erosion { get; init; } = new();

    /// <summary>Noise settings for the weirdness channel (derives PeaksAndValleys).</summary>
    [JsonProperty("weirdness")]
    public NoiseSettings Weirdness { get; init; } = new();

    /// <summary>Noise settings for the temperature channel.</summary>
    [JsonProperty("temperature")]
    public NoiseSettings Temperature { get; init; } = new();

    /// <summary>Noise settings for the humidity channel.</summary>
    [JsonProperty("humidity")]
    public NoiseSettings Humidity { get; init; } = new();

    /// <summary>Noise settings for cheese caves (large caverns).</summary>
    [JsonProperty("cave_cheese")]
    public NoiseSettings CaveCheese { get; init; } = new();

    /// <summary>Noise settings for spaghetti cave tunnel A.</summary>
    [JsonProperty("cave_spaghetti_1")]
    public NoiseSettings CaveSpaghetti1 { get; init; } = new();

    /// <summary>Noise settings for spaghetti cave tunnel B.</summary>
    [JsonProperty("cave_spaghetti_2")]
    public NoiseSettings CaveSpaghetti2 { get; init; } = new();

    /// <summary>
    /// Creates a default configuration with sensible noise parameters.
    /// </summary>
    /// <returns>A default climate noise config.</returns>
    public static ClimateNoiseConfig CreateDefault()
    {
        return new ClimateNoiseConfig
        {
            Continentalness = new NoiseSettings
            {
                SeedOffset = 0,
                Frequency = 0.0005f,
                Octaves = 4,
                Lacunarity = 2.0f,
                Gain = 0.45f,
            },
            Erosion = new NoiseSettings
            {
                SeedOffset = 10000,
                Frequency = 0.005f,
                Octaves = 5,
                Lacunarity = 2.2f,
                Gain = 0.45f,
            },
            Weirdness = new NoiseSettings
            {
                SeedOffset = 20000,
                Frequency = 0.004f,
                Octaves = 4,
                Lacunarity = 2.0f,
                Gain = 0.5f,
            },
            Temperature = new NoiseSettings
            {
                SeedOffset = 30000,
                Frequency = 0.002f,
                Octaves = 3,
                Lacunarity = 2.5f,
                Gain = 0.4f,
            },
            Humidity = new NoiseSettings
            {
                SeedOffset = 40000,
                Frequency = 0.002f,
                Octaves = 3,
                Lacunarity = 2.5f,
                Gain = 0.4f,
            },
            CaveCheese = new NoiseSettings
            {
                SeedOffset = 50000,
                Frequency = 0.015f,
                Octaves = 3,
                Lacunarity = 2.0f,
                Gain = 0.5f,
            },
            CaveSpaghetti1 = new NoiseSettings
            {
                SeedOffset = 60000,
                Frequency = 0.03f,
                Octaves = 2,
                Lacunarity = 2.0f,
                Gain = 0.5f,
            },
            CaveSpaghetti2 = new NoiseSettings
            {
                SeedOffset = 70000,
                Frequency = 0.03f,
                Octaves = 2,
                Lacunarity = 2.0f,
                Gain = 0.5f,
            },
        };
    }
}
