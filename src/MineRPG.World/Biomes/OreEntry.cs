using Newtonsoft.Json;

namespace MineRPG.World.Biomes;

/// <summary>
/// Describes ore distribution parameters within a biome.
/// Loaded from biome JSON files. The <see cref="BlockName"/> is resolved
/// to a <see cref="BlockId"/> at startup by the block resolver.
/// </summary>
public sealed class OreEntry
{
    /// <summary>Block name reference resolved at startup (e.g., "Coal Ore").</summary>
    [JsonProperty("block_name")]
    public string BlockName { get; init; } = "";

    /// <summary>Resolved block ID, set at startup by BiomeBlockResolver.</summary>
    [JsonIgnore]
    public ushort BlockId { get; set; }

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
