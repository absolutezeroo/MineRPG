using Newtonsoft.Json;

namespace MineRPG.World.Biomes;

/// <summary>
/// Describes a structure type and its generation rules within a biome.
/// </summary>
public sealed class StructureEntry
{
    /// <summary>Structure type identifier (e.g., "village", "dungeon").</summary>
    [JsonProperty("type")]
    public string Type { get; init; } = "";

    /// <summary>Chance per chunk to attempt placement (0.0 to 1.0).</summary>
    [JsonProperty("chance")]
    public float Chance { get; init; }

    /// <summary>Minimum distance between instances of this structure.</summary>
    [JsonProperty("min_distance_between")]
    public int MinDistanceBetween { get; init; } = 256;
}
