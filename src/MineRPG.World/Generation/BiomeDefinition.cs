using Newtonsoft.Json;

namespace MineRPG.World.Generation;

/// <summary>
/// Data-driven biome configuration. Loaded from Data/Biomes/*.json.
/// Block IDs reference BlockRegistry. Prefer name-based resolution
/// via BiomeBlockResolver for maintainability.
/// </summary>
public sealed class BiomeDefinition
{
    [JsonProperty("id")]
    public string Id { get; init; } = "";

    [JsonProperty("biomeType")]
    public BiomeType BiomeType { get; init; }

    [JsonProperty("baseHeight")]
    public int BaseHeight { get; init; } = 64;

    [JsonProperty("heightVariance")]
    public int HeightVariance { get; init; } = 20;

    [JsonProperty("surfaceBlock")]
    public ushort SurfaceBlock { get; set; }

    [JsonProperty("subSurfaceBlock")]
    public ushort SubSurfaceBlock { get; set; }

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

    [JsonProperty("minTemperature")]
    public float MinTemperature { get; init; }

    [JsonProperty("maxTemperature")]
    public float MaxTemperature { get; init; }

    [JsonProperty("minHumidity")]
    public float MinHumidity { get; init; }

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

    private HeightSpline? _heightSpline;

    /// <summary>
    /// Lazily computed from <see cref="HeightSplinePoints"/>.
    /// Falls back to a flat zero-offset spline when no points are defined.
    /// </summary>
    [JsonIgnore]
    public HeightSpline HeightSpline => _heightSpline ??= HeightSplinePoints is { Length: >= 2 }
        ? new HeightSpline(HeightSplinePoints)
        : HeightSpline.CreateDefault(0f, 0f);
}
