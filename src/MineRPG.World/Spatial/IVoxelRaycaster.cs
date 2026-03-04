namespace MineRPG.World.Spatial;

/// <summary>
/// DDA voxel raycast interface. Operates on chunk data without the physics engine.
/// </summary>
public interface IVoxelRaycaster
{
    /// <summary>
    /// Casts a ray through the voxel world and returns the first non-transparent block hit.
    /// </summary>
    /// <param name="originX">Ray origin X.</param>
    /// <param name="originY">Ray origin Y.</param>
    /// <param name="originZ">Ray origin Z.</param>
    /// <param name="directionX">Ray direction X (normalized).</param>
    /// <param name="directionY">Ray direction Y (normalized).</param>
    /// <param name="directionZ">Ray direction Z (normalized).</param>
    /// <param name="maxDistance">Maximum ray distance.</param>
    /// <returns>The raycast result with hit information.</returns>
    VoxelRaycastResult Cast(
        float originX, float originY, float originZ,
        float directionX, float directionY, float directionZ,
        float maxDistance);
}
