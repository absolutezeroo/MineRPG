using MineRPG.World.Biomes.Climate;
using MineRPG.World.Generation;

namespace MineRPG.World.Generation.Surface;

/// <summary>
/// Context data passed to surface rules for block determination.
/// Contains all the information a rule needs to decide which block to place.
/// </summary>
public readonly struct SurfaceContext
{
    /// <summary>World X coordinate.</summary>
    public int WorldX { get; init; }

    /// <summary>World Y coordinate of the block being evaluated.</summary>
    public int WorldY { get; init; }

    /// <summary>World Z coordinate.</summary>
    public int WorldZ { get; init; }

    /// <summary>Surface height at this column.</summary>
    public int SurfaceY { get; init; }

    /// <summary>Whether this block position is at the surface (top exposed block).</summary>
    public bool IsSurface { get; init; }

    /// <summary>Whether this block is on a ceiling (cave ceiling).</summary>
    public bool IsCeiling { get; init; }

    /// <summary>Depth below the surface (0 at surface, positive going down).</summary>
    public int DepthBelowSurface { get; init; }

    /// <summary>Slope steepness (max height difference to 4 neighbors).</summary>
    public int SlopeGradient { get; init; }

    /// <summary>Primary biome at this column.</summary>
    public BiomeDefinition Biome { get; init; }

    /// <summary>Full climate parameters at this column.</summary>
    public ClimateParameters Climate { get; init; }

    /// <summary>Sea level of the world.</summary>
    public int SeaLevel { get; init; }

    /// <summary>Whether this column is underwater (surface below sea level).</summary>
    public bool IsUnderwater { get; init; }

    /// <summary>Local noise value for pattern generation (e.g., badlands strata).</summary>
    public float PatternNoise { get; init; }
}
