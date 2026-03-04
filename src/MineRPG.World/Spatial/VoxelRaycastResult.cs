namespace MineRPG.World.Spatial;

/// <summary>Result of a DDA voxel raycast.</summary>
public sealed record VoxelRaycastResult(
    bool Hit,
    WorldPosition HitPosition,
    WorldPosition AdjacentPosition,
    ushort BlockId,
    float Distance);
