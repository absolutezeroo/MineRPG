using Newtonsoft.Json;

namespace MineRPG.World.Biomes;

/// <summary>
/// Visual ambiance settings for a biome (sky, fog, water, vegetation colors).
/// Used by the Godot bridge layer for rendering.
/// </summary>
public sealed class BiomeAmbiance
{
    /// <summary>Sky color hex string (e.g., "#78A7FF").</summary>
    [JsonProperty("sky_color")]
    public string SkyColor { get; init; } = "#78A7FF";

    /// <summary>Fog color hex string.</summary>
    [JsonProperty("fog_color")]
    public string FogColor { get; init; } = "#C0D8FF";

    /// <summary>Water color hex string.</summary>
    [JsonProperty("water_color")]
    public string WaterColor { get; init; } = "#3F76E4";

    /// <summary>Grass tint color hex string.</summary>
    [JsonProperty("grass_color")]
    public string GrassColor { get; init; } = "#7CBD6B";

    /// <summary>Foliage/leaf tint color hex string.</summary>
    [JsonProperty("foliage_color")]
    public string FoliageColor { get; init; } = "#59AE30";

    /// <summary>Fog density (0.0 = no fog, 1.0 = maximum fog).</summary>
    [JsonProperty("fog_density")]
    public float FogDensity { get; init; } = 0.0005f;
}
