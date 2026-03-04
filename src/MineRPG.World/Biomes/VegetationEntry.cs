using Newtonsoft.Json;

namespace MineRPG.World.Biomes;

/// <summary>
/// Describes a vegetation type and its density within a biome.
/// </summary>
public sealed class VegetationEntry
{
    /// <summary>Vegetation type identifier (e.g., "oak_tree", "tall_grass").</summary>
    [JsonProperty("type")]
    public string Type { get; init; } = "";

    /// <summary>Spawn density per block column (0.0 to 1.0).</summary>
    [JsonProperty("density")]
    public float Density { get; init; }

    /// <summary>Minimum group size when spawning clusters.</summary>
    [JsonProperty("min_group")]
    public int MinGroup { get; init; } = 1;

    /// <summary>Maximum group size when spawning clusters.</summary>
    [JsonProperty("max_group")]
    public int MaxGroup { get; init; } = 1;
}
