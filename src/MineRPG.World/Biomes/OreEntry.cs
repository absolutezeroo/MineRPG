using Newtonsoft.Json;

namespace MineRPG.World.Biomes;

/// <summary>
/// Describes ore distribution parameters within a biome.
/// Loaded from biome JSON files. References blocks by namespaced string ID.
/// The runtime ushort ID is resolved by <see cref="MineRPG.World.Generation.BiomeDefinitionResolver"/>.
/// </summary>
public sealed class OreEntry
{
    /// <summary>Namespaced block ID for the ore (e.g., "minerpg:coal_ore").</summary>
    [JsonProperty("block_id")]
    public string BlockId { get; init; } = "";

    /// <summary>Runtime-resolved ushort ID for the ore block. Set by BiomeDefinitionResolver.</summary>
    [JsonIgnore]
    public ushort RuntimeBlockId { get; internal set; }

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
