using Newtonsoft.Json;

namespace MineRPG.World.Biomes;

/// <summary>
/// Describes ore distribution parameters within a biome.
/// </summary>
public sealed class OreEntry
{
    /// <summary>Ore type identifier (e.g., "coal_ore", "iron_ore").</summary>
    [JsonProperty("type")]
    public string Type { get; init; } = "";

    /// <summary>Minimum Y height for ore generation.</summary>
    [JsonProperty("min_height")]
    public int MinHeight { get; init; }

    /// <summary>Maximum Y height for ore generation.</summary>
    [JsonProperty("max_height")]
    public int MaxHeight { get; init; } = 128;

    /// <summary>Maximum number of blocks per vein.</summary>
    [JsonProperty("vein_size")]
    public int VeinSize { get; init; } = 8;

    /// <summary>Number of vein attempts per chunk.</summary>
    [JsonProperty("frequency")]
    public int Frequency { get; init; } = 10;
}
