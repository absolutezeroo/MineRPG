using System.Runtime.CompilerServices;

namespace MineRPG.World.Spatial;

/// <summary>
/// A plane in 3D space represented by normal (NormalX, NormalY, NormalZ) and Distance.
/// Equation: NormalX*x + NormalY*y + NormalZ*z + Distance = 0.
/// Used for frustum culling -- tests whether AABBs are inside a camera frustum.
/// </summary>
public readonly struct FrustumPlane
{
    /// <summary>X component of the plane normal.</summary>
    public float NormalX { get; }

    /// <summary>Y component of the plane normal.</summary>
    public float NormalY { get; }

    /// <summary>Z component of the plane normal.</summary>
    public float NormalZ { get; }

    /// <summary>Signed distance from the origin.</summary>
    public float Distance { get; }

    /// <summary>
    /// Creates a frustum plane from a normal and distance.
    /// </summary>
    /// <param name="normalX">X component of the plane normal.</param>
    /// <param name="normalY">Y component of the plane normal.</param>
    /// <param name="normalZ">Z component of the plane normal.</param>
    /// <param name="distance">Signed distance from the origin.</param>
    public FrustumPlane(float normalX, float normalY, float normalZ, float distance)
    {
        NormalX = normalX;
        NormalY = normalY;
        NormalZ = normalZ;
        Distance = distance;
    }

    /// <summary>
    /// Tests whether an AABB is completely outside the negative half-space of this plane.
    /// Returns true if the AABB is entirely outside (should be culled).
    /// Uses the p-vertex optimization: test only the corner most aligned with the plane normal.
    /// </summary>
    /// <param name="minX">Minimum X of the AABB.</param>
    /// <param name="minY">Minimum Y of the AABB.</param>
    /// <param name="minZ">Minimum Z of the AABB.</param>
    /// <param name="maxX">Maximum X of the AABB.</param>
    /// <param name="maxY">Maximum Y of the AABB.</param>
    /// <param name="maxZ">Maximum Z of the AABB.</param>
    /// <returns>True if the AABB is entirely outside this plane.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBoxOutside(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
    {
        // Select the p-vertex (the corner furthest in the direction of the plane normal)
        float pVertexX = NormalX >= 0 ? maxX : minX;
        float pVertexY = NormalY >= 0 ? maxY : minY;
        float pVertexZ = NormalZ >= 0 ? maxZ : minZ;

        return NormalX * pVertexX + NormalY * pVertexY + NormalZ * pVertexZ + Distance < 0;
    }
}
