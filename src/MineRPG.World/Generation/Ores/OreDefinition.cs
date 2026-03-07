using Newtonsoft.Json;

namespace MineRPG.World.Generation.Ores;

/// <summary>
/// Data-driven definition of an ore type and its distribution parameters.
/// References blocks by their stable numeric ID.
/// </summary>
public sealed class OreDefinition
{
    /// <summary>Unique ore identifier (e.g., "coal_ore").</summary>
    [JsonProperty("id")]
    public string Id { get; init; } = "";

    /// <summary>Block ID for the ore block (matches BlockDefinition.Id).</summary>
    [JsonProperty("block_id")]
    public ushort BlockId { get; init; }

    /// <summary>Minimum Y for ore generation.</summary>
    [JsonProperty("min_height")]
    public int MinHeight { get; init; }

    /// <summary>Maximum Y for ore generation.</summary>
    [JsonProperty("max_height")]
    public int MaxHeight { get; init; } = 128;

    /// <summary>Y level with peak probability (for triangle distribution).</summary>
    [JsonProperty("peak_height")]
    public int PeakHeight { get; init; } = 32;

    /// <summary>Maximum blocks per vein.</summary>
    [JsonProperty("vein_size")]
    public int VeinSize { get; init; } = 8;

    /// <summary>Number of vein attempts per chunk.</summary>
    [JsonProperty("frequency")]
    public int Frequency { get; init; } = 10;

    /// <summary>Height distribution type.</summary>
    [JsonProperty("distribution")]
    public OreDistribution Distribution { get; init; } = OreDistribution.Triangle;

    /// <summary>Whether to reduce spawns near air exposure (0.0 to 1.0).</summary>
    [JsonProperty("air_exposure_reduction")]
    public float AirExposureReduction { get; init; }
}
