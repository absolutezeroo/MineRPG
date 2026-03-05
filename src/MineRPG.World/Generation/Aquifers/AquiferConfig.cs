using Newtonsoft.Json;

namespace MineRPG.World.Generation.Aquifers;

/// <summary>
/// Data-driven configuration for the aquifer system.
/// Loaded from Data/WorldGen/aquifer_config.json.
/// </summary>
public sealed class AquiferConfig
{
    /// <summary>Y level at or below which aquifers contain lava instead of water.</summary>
    [JsonProperty("lava_level")]
    public int LavaLevel { get; init; } = -55;

    /// <summary>Sea level used as default water level for shallow aquifers.</summary>
    [JsonProperty("sea_level")]
    public int SeaLevel { get; init; } = 62;

    /// <summary>Minimum distance below surface for aquifer activation.</summary>
    [JsonProperty("surface_clearance")]
    public int SurfaceClearance { get; init; } = 12;

    /// <summary>Y threshold below which aquifer water levels are randomized.</summary>
    [JsonProperty("deep_aquifer_threshold")]
    public int DeepAquiferThreshold { get; init; } = 31;

    /// <summary>Noise frequency for the floodedness sampling grid.</summary>
    [JsonProperty("floodedness_frequency")]
    public float FloodednessFrequency { get; init; } = 0.015f;

    /// <summary>Threshold above which a cavity is flooded (range [-1, 1]).</summary>
    [JsonProperty("floodedness_threshold")]
    public float FloodednessThreshold { get; init; } = 0.3f;

    /// <summary>Spacing of the aquifer level sampling grid in blocks.</summary>
    [JsonProperty("grid_spacing")]
    public int GridSpacing { get; init; } = 16;

    /// <summary>Noise frequency for aquifer water level variation.</summary>
    [JsonProperty("level_noise_frequency")]
    public float LevelNoiseFrequency { get; init; } = 0.01f;

    /// <summary>Maximum vertical variation of aquifer water levels.</summary>
    [JsonProperty("level_variation")]
    public int LevelVariation { get; init; } = 10;

    /// <summary>Width of the barrier generated between adjacent aquifers.</summary>
    [JsonProperty("barrier_width")]
    public int BarrierWidth { get; init; } = 2;

    /// <summary>
    /// Creates a default aquifer configuration.
    /// </summary>
    /// <returns>A new default configuration.</returns>
    public static AquiferConfig CreateDefault() => new();
}
