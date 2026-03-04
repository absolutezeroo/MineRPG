using MineRPG.World.Biomes.Climate;

namespace MineRPG.World.Generation;

/// <summary>
/// Precomputed per-column (X,Z) terrain values used by <see cref="WorldGenerator"/>
/// to fill blocks without recomputing noise for each Y level.
/// </summary>
public readonly struct TerrainColumn
{
    /// <summary>Final blended surface Y after spline mapping.</summary>
    public int SurfaceY { get; init; }

    /// <summary>Depth of the subsurface layer (dirt/sand layer thickness).</summary>
    public int SubSurfaceDepth { get; init; }

    /// <summary>Blended primary biome for surface block selection.</summary>
    public BiomeDefinition PrimaryBiome { get; init; }

    /// <summary>Secondary biome for smooth transitions at boundaries.</summary>
    public BiomeDefinition SecondaryBiome { get; init; }

    /// <summary>
    /// Blend weight in [0, 1]. 0 = 100% primary, 1 = 100% secondary.
    /// </summary>
    public float BlendWeight { get; init; }

    /// <summary>
    /// Raw continentalness value in [-1, 1] for cave suppression near oceans.
    /// </summary>
    public float Continentalness { get; init; }

    /// <summary>
    /// Full climate parameters sampled at this column.
    /// </summary>
    public ClimateParameters Climate { get; init; }
}
