using System.Runtime.CompilerServices;
using MineRPG.World.Chunks;

namespace MineRPG.World.Spatial;

/// <summary>
/// Pure-logic frustum culler. Tests chunk AABBs against 6 frustum planes.
/// Thread-safe: all state is passed in, no mutable fields.
/// </summary>
public static class FrustumCuller
{
    /// <summary>
    /// Tests whether a chunk AABB is inside the frustum defined by 6 planes.
    /// Returns true if the chunk is visible (at least partially inside the frustum).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsChunkVisible(
        ReadOnlySpan<FrustumPlane> planes,
        int chunkWorldX, int chunkWorldZ)
    {
        var minX = (float)chunkWorldX;
        var minY = 0f;
        var minZ = (float)chunkWorldZ;
        var maxX = minX + ChunkData.SizeX;
        var maxY = (float)ChunkData.SizeY;
        var maxZ = minZ + ChunkData.SizeZ;

        for (var i = 0; i < planes.Length; i++)
        {
            if (planes[i].IsBoxOutside(minX, minY, minZ, maxX, maxY, maxZ))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Tests whether a sub-chunk AABB is inside the frustum.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSubChunkVisible(
        ReadOnlySpan<FrustumPlane> planes,
        int chunkWorldX, int subChunkMinY, int chunkWorldZ,
        int subChunkHeight)
    {
        var minX = (float)chunkWorldX;
        var minY = (float)subChunkMinY;
        var minZ = (float)chunkWorldZ;
        var maxX = minX + ChunkData.SizeX;
        var maxY = minY + subChunkHeight;
        var maxZ = minZ + ChunkData.SizeZ;

        for (var i = 0; i < planes.Length; i++)
        {
            if (planes[i].IsBoxOutside(minX, minY, minZ, maxX, maxY, maxZ))
                return false;
        }

        return true;
    }
}
