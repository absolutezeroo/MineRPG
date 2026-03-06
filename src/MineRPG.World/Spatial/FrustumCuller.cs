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
    /// Computes a bitmask of sub-chunks that are vertically occluded.
    /// Scans downward from the camera's sub-chunk level. When a sub-chunk
    /// with a full horizontal barrier (or fully solid) is found, all sub-chunks
    /// below it are marked as occluded because the barrier blocks visibility.
    /// </summary>
    /// <param name="subChunks">Sub-chunk metadata array (16 entries).</param>
    /// <param name="cameraY">The camera's Y position in world coordinates.</param>
    /// <returns>
    /// A 16-bit mask where bit N is set if sub-chunk N is vertically occluded.
    /// </returns>
    public static ushort ComputeVerticalOcclusionMask(
        ReadOnlySpan<SubChunkInfo> subChunks,
        float cameraY)
    {
        int cameraSubChunk = Math.Clamp(
            (int)(cameraY / SubChunkConstants.SubChunkSize),
            0,
            SubChunkConstants.SubChunkCount - 1);

        ushort mask = 0;

        // Scan downward from camera position looking for a barrier
        for (int i = cameraSubChunk; i >= 0; i--)
        {
            if (subChunks[i].HasFullHorizontalBarrier || subChunks[i].IsFullySolid)
            {
                // Everything below this barrier is occluded from above
                for (int j = i - 1; j >= 0; j--)
                {
                    mask |= (ushort)(1 << j);
                }

                break;
            }
        }

        return mask;
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
