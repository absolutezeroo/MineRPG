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

    /// <summary>Default voxel render distance in chunks, matching ChunkLoadingScheduler.</summary>
    public const int DefaultVoxelRenderDistance = 16;

    /// <summary>
    /// Inner radius of ring 0 in chunks. Voxel LOD chunks fill everything
    /// inside this distance; the clipmap renders beyond it.
    /// Must match the actual voxel render distance to avoid gaps or overlap.
    /// </summary>
    public int VoxelCutoffChunks { get; init; } = DefaultVoxelRenderDistance;

    /// <summary>
    /// Ring definitions ordered from nearest to farthest.
    /// </summary>
    public ClipmapRing[] Rings { get; init; } = CreateDefaultRings(DefaultVoxelRenderDistance);

    /// <summary>
    /// How many blocks the player must move before the clipmap meshes are rebuilt.
    /// Prevents thrashing when the player sways slightly.
    /// </summary>
    public int RebuildThresholdBlocks { get; init; } = 16;

    /// <summary>
    /// Creates ring definitions anchored to the given voxel render distance.
    /// Ring 0 starts where voxel chunks end. Each subsequent ring doubles the range.
    /// </summary>
    /// <param name="voxelCutoff">Voxel render distance in chunks.</param>
    /// <returns>Array of ring definitions.</returns>
    public static ClipmapRing[] CreateDefaultRings(int voxelCutoff)
    {
        return
        [
            new ClipmapRing
            {
                InnerRadiusChunks = voxelCutoff,
                OuterRadiusChunks = voxelCutoff * 4,
                BlocksPerVertex = 4,
            },
            new ClipmapRing
            {
                InnerRadiusChunks = voxelCutoff * 4,
                OuterRadiusChunks = voxelCutoff * 8,
                BlocksPerVertex = 16,
            },
            new ClipmapRing
            {
                InnerRadiusChunks = voxelCutoff * 8,
                OuterRadiusChunks = voxelCutoff * 16,
                BlocksPerVertex = 64,
            },
        ];
    }
}
