namespace MineRPG.World.Spatial;

/// <summary>
/// Result of a DDA voxel raycast.
/// </summary>
/// <param name="Hit">Whether the ray hit a solid block.</param>
/// <param name="HitPosition">World position of the hit block.</param>
/// <param name="AdjacentPosition">World position adjacent to the hit (for block placement).</param>
/// <param name="BlockId">The block ID that was hit.</param>
/// <param name="Distance">Distance from origin to the hit point.</param>
public readonly record struct VoxelRaycastResult(
    bool Hit,
    WorldPosition HitPosition,
    WorldPosition AdjacentPosition,
    ushort BlockId,
    float Distance);
