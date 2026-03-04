using System.Runtime.CompilerServices;

namespace MineRPG.World.Spatial;

/// <summary>
/// A plane in 3D space represented by normal (A,B,C) and distance D.
/// Equation: Ax + By + Cz + D = 0.
/// Used for frustum culling — tests whether AABBs are inside a camera frustum.
/// </summary>
public readonly struct FrustumPlane(float a, float b, float c, float d)
{
    public readonly float A = a;
    public readonly float B = b;
    public readonly float C = c;
    public readonly float D = d;

    /// <summary>
    /// Tests whether an AABB is completely outside the negative half-space of this plane.
    /// Returns true if the AABB is entirely outside (should be culled).
    /// Uses the p-vertex optimization: test only the corner most aligned with the plane normal.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBoxOutside(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
    {
        // Select the p-vertex (the corner furthest in the direction of the plane normal)
        var px = A >= 0 ? maxX : minX;
        var py = B >= 0 ? maxY : minY;
        var pz = C >= 0 ? maxZ : minZ;

        return A * px + B * py + C * pz + D < 0;
    }
}
