namespace MineRPG.World.Terrain;

/// <summary>
/// Configuration for geometry clipmap rings that render the distant terrain
/// horizon as a simplified 2D heightmap instead of full voxel chunks.
/// Each ring covers an annular region at progressively lower resolution.
/// </summary>
public sealed class ClipmapConfig
{
    /// <summary>Number of concentric clipmap rings.</summary>
    public const int RingCount = 3;

    /// <summary>
    /// Inner radius of ring 0 in chunks. Voxel LOD chunks fill everything
    /// inside this distance; the clipmap renders beyond it.
    /// </summary>
    public int VoxelCutoffChunks { get; init; } = 32;

    /// <summary>
    /// Ring definitions ordered from nearest to farthest.
    /// </summary>
    public ClipmapRing[] Rings { get; init; } = CreateDefaultRings();

    /// <summary>
    /// How many blocks the player must move before the clipmap meshes are rebuilt.
    /// Prevents thrashing when the player sways slightly.
    /// </summary>
    public int RebuildThresholdBlocks { get; init; } = 16;

    private static ClipmapRing[] CreateDefaultRings()
    {
        return
        [
            new ClipmapRing
            {
                InnerRadiusChunks = 32,
                OuterRadiusChunks = 64,
                BlocksPerVertex = 4,
            },
            new ClipmapRing
            {
                InnerRadiusChunks = 64,
                OuterRadiusChunks = 128,
                BlocksPerVertex = 16,
            },
            new ClipmapRing
            {
                InnerRadiusChunks = 128,
                OuterRadiusChunks = 256,
                BlocksPerVertex = 64,
            },
        ];
    }
}
