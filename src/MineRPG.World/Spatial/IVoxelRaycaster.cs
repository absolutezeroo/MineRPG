namespace MineRPG.World.Spatial;

/// <summary>
/// DDA voxel raycast interface. Operates on chunk data without the physics engine.
/// </summary>
public interface IVoxelRaycaster
{
    VoxelRaycastResult Cast(
        float ox, float oy, float oz,
        float dx, float dy, float dz,
        float maxDistance);
}
