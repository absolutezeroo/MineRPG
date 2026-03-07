using System.Collections.Generic;

using MineRPG.World.Biomes;
using MineRPG.World.Biomes.Climate;

using Newtonsoft.Json;

namespace MineRPG.World.Generation;

/// <summary>
/// Data-driven biome configuration supporting 6D climate targeting.
/// Loaded from Data/Biomes/*.json. Adding a new biome requires only a JSON file.
/// Block references use namespaced string IDs (e.g., "minerpg:grass") in JSON.
/// Runtime ushort IDs are resolved by <see cref="BiomeDefinitionResolver"/> after load.
/// </summary>
public sealed class BiomeDefinition
{
    private const int MinSplinePoints = 2;
    private const float DefaultSplineOffset = 0f;

    private HeightSpline? _heightSpline;

    /// <summary>Unique biome identifier (e.g., "plains", "snowy_mountains").</summary>
    [JsonProperty("id")]
    public string Id { get; init; } = "";

    /// <summary>Display name for the biome.</summary>
    [JsonProperty("displayName")]
    public string DisplayName { get; init; } = "";

    /// <summary>The geographical category of this biome.</summary>
    [JsonProperty("category")]
    public BiomeCategory Category { get; init; }

    /// <summary>Legacy biome type enum for backwards compatibility.</summary>
    [JsonProperty("biomeType")]
    public BiomeType BiomeType { get; init; }

    /// <summary>The 6D climate target for biome selection.</summary>
    [JsonProperty("climateTarget")]
    public BiomeClimateTarget ClimateTarget { get; init; }

    /// <summary>Base terrain height for this biome.</summary>
    [JsonProperty("baseHeight")]
    public int BaseHeight { get; init; } = 64;

    /// <summary>Maximum height variance from the base.</summary>
    [JsonProperty("heightVariation")]
    public float HeightVariation { get; init; } = 8f;

    /// <summary>Scale factor for local terrain detail noise.</summary>
    [JsonProperty("terrainScale")]
    public float TerrainScale { get; init; } = 1f;

    /// <summary>Namespaced block ID for the top surface layer (e.g., "minerpg:grass").</summary>
    [JsonProperty("surfaceBlock")]
    public string SurfaceBlockId { get; init; } = "";

    /// <summary>Namespaced block ID for the sub-surface layer (e.g., "minerpg:dirt").</summary>
    [JsonProperty("subSurfaceBlock")]
    public string SubSurfaceBlockId { get; init; } = "";

    /// <summary>Namespaced block ID for the stone layer (e.g., "minerpg:stone").</summary>
    [JsonProperty("stoneBlock")]
    public string StoneBlockId { get; init; } = "";

    /// <summary>Namespaced block ID for underwater floor surfaces.</summary>
    [JsonProperty("underwaterBlock")]
    public string UnderwaterBlockId { get; init; } = "";

    /// <summary>Runtime-resolved ushort ID for the surface block. Set by BiomeDefinitionResolver.</summary>
    [JsonIgnore]
    public ushort SurfaceBlock { get; internal set; }

    /// <summary>Runtime-resolved ushort ID for the sub-surface block. Set by BiomeDefinitionResolver.</summary>
    [JsonIgnore]
    public ushort SubSurfaceBlock { get; internal set; }

    /// <summary>Runtime-resolved ushort ID for the stone block. Set by BiomeDefinitionResolver.</summary>
    [JsonIgnore]
    public ushort StoneBlock { get; internal set; }

    /// <summary>Runtime-resolved ushort ID for the underwater block. Set by BiomeDefinitionResolver.</summary>
    [JsonIgnore]
    public ushort UnderwaterBlock { get; internal set; }

    /// <summary>Thickness of the sub-surface layer (dirt/sand). Default 4.</summary>
    [JsonProperty("subSurfaceDepth")]
    public int SubSurfaceDepth { get; init; } = 4;

    /// <summary>Legacy minimum temperature for backwards compatibility.</summary>
    [JsonProperty("minTemperature")]
    public float MinTemperature { get; init; }

    /// <summary>Legacy maximum temperature for backwards compatibility.</summary>
    [JsonProperty("maxTemperature")]
    public float MaxTemperature { get; init; }

    /// <summary>Legacy minimum humidity for backwards compatibility.</summary>
    [JsonProperty("minHumidity")]
    public float MinHumidity { get; init; }

    /// <summary>Legacy maximum humidity for backwards compatibility.</summary>
    [JsonProperty("maxHumidity")]
    public float MaxHumidity { get; init; }

    /// <summary>Vegetation entries for this biome.</summary>
    [JsonProperty("vegetation")]
    public IReadOnlyList<VegetationEntry> Vegetation { get; init; } = [];

    /// <summary>Ore distribution entries for this biome.</summary>
    [JsonProperty("ores")]
    public IReadOnlyList<OreEntry> Ores { get; init; } = [];

    /// <summary>Structure generation entries for this biome.</summary>
    [JsonProperty("structures")]
    public IReadOnlyList<StructureEntry> Structures { get; init; } = [];

    /// <summary>Visual ambiance settings for this biome.</summary>
    [JsonProperty("ambiance")]
    public BiomeAmbiance Ambiance { get; init; } = new();

    /// <summary>Gameplay settings for this biome.</summary>
    [JsonProperty("gameplay")]
    public BiomeGameplay Gameplay { get; init; } = new();

    /// <summary>
    /// Optional biome-local height offset spline points.
    /// Maps PV noise to a small Y offset on top of the global terrain height.
    /// </summary>
    [JsonProperty("heightSplinePoints")]
    public SplinePoint[]? HeightSplinePoints { get; init; }

    /// <summary>
    /// Lazily computed from <see cref="HeightSplinePoints"/>.
    /// Falls back to a flat zero-offset spline when no points are defined.
    /// </summary>
    [JsonIgnore]
    public HeightSpline HeightSpline => _heightSpline ??= HeightSplinePoints is { Length: >= MinSplinePoints }
        ? new HeightSpline(HeightSplinePoints)
        : HeightSpline.CreateDefault(DefaultSplineOffset, DefaultSplineOffset);

    /// <summary>
    /// Explicit flag indicating whether this biome uses 6D climate targeting.
    /// Set to true in JSON for biomes with climate targets. Defaults to false for legacy biomes.
    /// </summary>
    [JsonProperty("hasClimateTarget")]
    public bool HasClimateTarget { get; init; }
}
