using System.Collections.Generic;

using Newtonsoft.Json;

namespace MineRPG.World.Generation.CaveFeatures;

/// <summary>
/// Data-driven configuration for cave decorative features.
/// </summary>
public sealed class CaveFeatureConfig
{
    /// <summary>Minimum floor-to-ceiling distance for pillar generation.</summary>
    [JsonProperty("pillar_min_height")]
    public int PillarMinHeight { get; init; } = 15;

    /// <summary>Chance per eligible column to generate a pillar (0.0 to 1.0).</summary>
    [JsonProperty("pillar_chance")]
    public float PillarChance { get; init; } = 0.05f;

    /// <summary>Pillar width in blocks (1 to 4).</summary>
    [JsonProperty("pillar_width")]
    public int PillarWidth { get; init; } = 2;

    /// <summary>Chance per eligible ceiling block for a stalactite.</summary>
    [JsonProperty("stalactite_chance")]
    public float StalactiteChance { get; init; } = 0.02f;

    /// <summary>Maximum stalactite length in blocks.</summary>
    [JsonProperty("stalactite_max_length")]
    public int StalactiteMaxLength { get; init; } = 8;

    /// <summary>Chance per eligible floor block for a stalagmite.</summary>
    [JsonProperty("stalagmite_chance")]
    public float StalagmiteChance { get; init; } = 0.02f;

    /// <summary>Maximum stalagmite height in blocks.</summary>
    [JsonProperty("stalagmite_max_height")]
    public int StalagmiteMaxHeight { get; init; } = 6;

    /// <summary>Block name for pillar/formation material.</summary>
    [JsonProperty("formation_block_name")]
    public string FormationBlockName { get; init; } = "Stone";

    /// <summary>Biome IDs where cave features are active (empty = all cave biomes).</summary>
    [JsonProperty("eligible_biomes")]
    public IReadOnlyList<string> EligibleBiomes { get; init; } = [];

    /// <summary>
    /// Creates a default configuration.
    /// </summary>
    /// <returns>A new default config.</returns>
    public static CaveFeatureConfig CreateDefault() => new();
}
