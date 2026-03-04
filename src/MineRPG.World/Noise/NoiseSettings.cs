using Newtonsoft.Json;

namespace MineRPG.World.Noise;

/// <summary>
/// Data-driven configuration for a single noise sampler.
/// Loaded from Data/WorldGen/noise_settings.json.
/// </summary>
public sealed class NoiseSettings
{
    /// <summary>Type of noise (simplex, perlin). Currently only simplex is supported.</summary>
    [JsonProperty("type")]
    public string Type { get; init; } = "simplex";

    /// <summary>Offset added to the world seed for this noise channel.</summary>
    [JsonProperty("seed_offset")]
    public int SeedOffset { get; init; }

    /// <summary>Base frequency of the first octave.</summary>
    [JsonProperty("frequency")]
    public float Frequency { get; init; } = 0.003f;

    /// <summary>Number of fractal octaves.</summary>
    [JsonProperty("octaves")]
    public int Octaves { get; init; } = 4;

    /// <summary>Frequency multiplier per octave.</summary>
    [JsonProperty("lacunarity")]
    public float Lacunarity { get; init; } = 2.0f;

    /// <summary>Amplitude multiplier per octave (also called persistence or gain).</summary>
    [JsonProperty("gain")]
    public float Gain { get; init; } = 0.5f;
}
