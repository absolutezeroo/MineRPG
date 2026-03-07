using Newtonsoft.Json;

namespace MineRPG.World.Biomes;

/// <summary>
/// Describes ore distribution parameters within a biome.
/// Loaded from biome JSON files. References blocks by their stable numeric ID.
/// </summary>
public sealed class OreEntry
{
    /// <summary>Block ID for the ore block (matches BlockDefinition.Id).</summary>
    [JsonProperty("block_id")]
    public ushort BlockId { get; init; }

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
