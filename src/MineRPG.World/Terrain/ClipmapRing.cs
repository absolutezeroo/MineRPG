namespace MineRPG.World.Terrain;

/// <summary>
/// Defines a single concentric ring of the geometry clipmap.
/// Each ring covers an annular area around the player at a fixed resolution.
/// </summary>
public sealed class ClipmapRing
{
    /// <summary>Inner radius of this ring in chunks. The inner hole is filled by a closer ring or voxels.</summary>
    public int InnerRadiusChunks { get; init; }

    /// <summary>Outer radius of this ring in chunks.</summary>
    public int OuterRadiusChunks { get; init; }

    /// <summary>Horizontal distance in blocks between adjacent vertices. Higher = fewer vertices.</summary>
    public int BlocksPerVertex { get; init; }
}
