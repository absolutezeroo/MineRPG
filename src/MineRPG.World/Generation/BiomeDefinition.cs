using Newtonsoft.Json;

namespace MineRPG.World.Generation;

/// <summary>
/// Data-driven biome configuration. Loaded from Data/Biomes/*.json.
/// Block IDs reference BlockRegistry. Prefer name-based resolution
/// via BiomeBlockResolver for maintainability.
/// </summary>
public sealed class BiomeDefinition
{
    private const int MinSplinePoints = 2;
    private const float DefaultSplineOffset = 0f;

    private HeightSpline? _heightSpline;

    /// <summary>Unique biome identifier.</summary>
    [JsonProperty("id")]
    public string Id { get; init; } = "";

    /// <summary>The category of biome.</summary>
    [JsonProperty("biomeType")]
    public BiomeType BiomeType { get; init; }

    /// <summary>Base terrain height for this biome.</summary>
    [JsonProperty("baseHeight")]
    public int BaseHeight { get; init; } = 64;

    /// <summary>Maximum height variance from the base.</summary>
    [JsonProperty("heightVariance")]
    public int HeightVariance { get; init; } = 20;

    /// <summary>Block ID for the surface layer.</summary>
    [JsonProperty("surfaceBlock")]
    public ushort SurfaceBlock { get; set; }

    /// <summary>Block ID for the sub-surface layer.</summary>
    [JsonProperty("subSurfaceBlock")]
    public ushort SubSurfaceBlock { get; set; }

    /// <summary>Block ID for the stone layer.</summary>
    [JsonProperty("stoneBlock")]
    public ushort StoneBlock { get; set; }

    /// <summary>Optional: name-based block reference resolved at startup.</summary>
    [JsonProperty("surfaceBlockName")]
    public string? SurfaceBlockName { get; init; }

    /// <summary>Optional: name-based block reference resolved at startup.</summary>
    [JsonProperty("subSurfaceBlockName")]
    public string? SubSurfaceBlockName { get; init; }

    /// <summary>Optional: name-based block reference resolved at startup.</summary>
    [JsonProperty("stoneBlockName")]
    public string? StoneBlockName { get; init; }

    /// <summary>Minimum temperature for this biome to be selected.</summary>
    [JsonProperty("minTemperature")]
    public float MinTemperature { get; init; }

    /// <summary>Maximum temperature for this biome to be selected.</summary>
    [JsonProperty("maxTemperature")]
    public float MaxTemperature { get; init; }

    /// <summary>Minimum humidity for this biome to be selected.</summary>
    [JsonProperty("minHumidity")]
    public float MinHumidity { get; init; }

    /// <summary>Maximum humidity for this biome to be selected.</summary>
    [JsonProperty("maxHumidity")]
    public float MaxHumidity { get; init; }

    /// <summary>Thickness of the sub-surface layer (dirt/sand). Default 4.</summary>
    [JsonProperty("subSurfaceDepth")]
    public int SubSurfaceDepth { get; init; } = 4;

    /// <summary>
    /// Optional biome-local height offset spline points.
    /// Maps PV noise to a small Y offset on top of the global terrain height.
    /// If null, a flat spline with zero offset is used.
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
}
