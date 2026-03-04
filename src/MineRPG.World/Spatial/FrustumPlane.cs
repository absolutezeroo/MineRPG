using System.Runtime.CompilerServices;

namespace MineRPG.World.Spatial;

/// <summary>
/// A plane in 3D space represented by normal (NormalX, NormalY, NormalZ) and Distance.
/// Equation: NormalX*x + NormalY*y + NormalZ*z + Distance = 0.
/// Used for frustum culling — tests whether AABBs are inside a camera frustum.
/// </summary>
public readonly struct FrustumPlane(float normalX, float normalY, float normalZ, float distance)
{
    public float NormalX { get; } = normalX;
    public float NormalY { get; } = normalY;
    public float NormalZ { get; } = normalZ;
    public float Distance { get; } = distance;

    /// <summary>
    /// Tests whether an AABB is completely outside the negative half-space of this plane.
    /// Returns true if the AABB is entirely outside (should be culled).
    /// Uses the p-vertex optimization: test only the corner most aligned with the plane normal.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBoxOutside(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
    {
        // Select the p-vertex (the corner furthest in the direction of the plane normal)
        var px = NormalX >= 0 ? maxX : minX;
        var py = NormalY >= 0 ? maxY : minY;
        var pz = NormalZ >= 0 ? maxZ : minZ;

        return NormalX * px + NormalY * py + NormalZ * pz + Distance < 0;
    }
}
