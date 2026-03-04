using System;
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
    /// <param name="planes">The 6 frustum planes.</param>
    /// <param name="chunkWorldX">Chunk origin X in world coordinates.</param>
    /// <param name="chunkWorldZ">Chunk origin Z in world coordinates.</param>
    /// <returns>True if the chunk is at least partially visible.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsChunkVisible(
        ReadOnlySpan<FrustumPlane> planes,
        int chunkWorldX, int chunkWorldZ)
    {
        float minX = (float)chunkWorldX;
        float minY = 0f;
        float minZ = (float)chunkWorldZ;
        float maxX = minX + ChunkData.SizeX;
        float maxY = (float)ChunkData.SizeY;
        float maxZ = minZ + ChunkData.SizeZ;

        for (int i = 0; i < planes.Length; i++)
        {
            if (planes[i].IsBoxOutside(minX, minY, minZ, maxX, maxY, maxZ))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Tests whether a sub-chunk AABB is inside the frustum.
    /// </summary>
    /// <param name="planes">The 6 frustum planes.</param>
    /// <param name="chunkWorldX">Chunk origin X in world coordinates.</param>
    /// <param name="subChunkMinY">Sub-chunk minimum Y coordinate.</param>
    /// <param name="chunkWorldZ">Chunk origin Z in world coordinates.</param>
    /// <param name="subChunkHeight">Height of the sub-chunk.</param>
    /// <returns>True if the sub-chunk is at least partially visible.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSubChunkVisible(
        ReadOnlySpan<FrustumPlane> planes,
        int chunkWorldX, int subChunkMinY, int chunkWorldZ,
        int subChunkHeight)
    {
        float minX = (float)chunkWorldX;
        float minY = (float)subChunkMinY;
        float minZ = (float)chunkWorldZ;
        float maxX = minX + ChunkData.SizeX;
        float maxY = minY + subChunkHeight;
        float maxZ = minZ + ChunkData.SizeZ;

        for (int i = 0; i < planes.Length; i++)
        {
            if (planes[i].IsBoxOutside(minX, minY, minZ, maxX, maxY, maxZ))
            {
                return false;
            }
        }

        return true;
    }
}
