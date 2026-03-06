using System;

using MineRPG.Core.Math;

namespace MineRPG.World.Chunks;

/// <summary>
/// Tests whether a sub-chunk (16x16x16 section) is completely hidden by
/// surrounding geometry. A sub-chunk is considered occluded if every one
/// of its 6 faces is covered by a fully-solid face from an adjacent sub-chunk
/// (either within the same chunk or in a neighbor chunk).
///
/// Unlike the existing empty-skip (which only skips 100% air sub-chunks),
/// this detects sub-chunks that contain blocks but are entirely buried
/// underground — for example, stone deep inside a mountain.
///
/// Thread-safe: all state is passed in, no mutable fields.
/// </summary>
public static class SubChunkOcclusionTest
{
    /// <summary>
    /// Computes a bitmask of sub-chunks that are fully occluded
    /// (hidden by solid neighbors on all 6 faces).
    /// Bit N is set if sub-chunk N is fully buried and can be skipped.
    /// </summary>
    /// <param name="subChunks">Sub-chunk metadata for the target chunk.</param>
    /// <param name="neighbors">
    /// Cardinal neighbor sub-chunk metadata arrays. Index layout:
    /// [0]=East(+X), [1]=West(-X), [2]=South(+Z), [3]=North(-Z).
    /// Entries may be null if the neighbor chunk is not loaded.
    /// </param>
    /// <returns>A 16-bit mask where bit N set = sub-chunk N is fully occluded.</returns>
    public static ushort ComputeOcclusionMask(
        SubChunkInfo[] subChunks,
        SubChunkInfo[]?[] neighbors)
    {
        ushort mask = 0;

        for (int i = 0; i < subChunks.Length; i++)
        {
            if (subChunks[i].IsEmpty)
            {
                // Empty sub-chunks are already skipped by the mesher;
                // no need to mark as occluded
                continue;
            }

            if (IsSubChunkOccluded(subChunks, neighbors, i))
            {
                mask |= (ushort)(1 << i);
            }
        }

        return mask;
    }

    /// <summary>
    /// Tests whether a single sub-chunk is fully occluded on all 6 faces.
    /// </summary>
    private static bool IsSubChunkOccluded(
        SubChunkInfo[] subChunks,
        SubChunkInfo[]?[] neighbors,
        int subChunkIndex)
    {
        // Top face: check sub-chunk above
        if (!IsTopFaceCovered(subChunks, subChunkIndex))
        {
            return false;
        }

        // Bottom face: check sub-chunk below
        if (!IsBottomFaceCovered(subChunks, subChunkIndex))
        {
            return false;
        }

        // Four horizontal faces: check corresponding neighbor chunk sub-chunks
        // East (+X)
        if (!IsHorizontalFaceCovered(neighbors, 0, subChunkIndex))
        {
            return false;
        }

        // West (-X)
        if (!IsHorizontalFaceCovered(neighbors, 1, subChunkIndex))
        {
            return false;
        }

        // South (+Z)
        if (!IsHorizontalFaceCovered(neighbors, 2, subChunkIndex))
        {
            return false;
        }

        // North (-Z)
        if (!IsHorizontalFaceCovered(neighbors, 3, subChunkIndex))
        {
            return false;
        }

        return true;
    }

    private static bool IsTopFaceCovered(SubChunkInfo[] subChunks, int index)
    {
        if (index >= subChunks.Length - 1)
        {
            // Top of the world: considered covered (nothing above to see)
            return true;
        }

        return subChunks[index + 1].IsFullySolid;
    }

    private static bool IsBottomFaceCovered(SubChunkInfo[] subChunks, int index)
    {
        if (index <= 0)
        {
            // Bottom of the world: considered covered (nothing below to see)
            return true;
        }

        return subChunks[index - 1].IsFullySolid;
    }

    private static bool IsHorizontalFaceCovered(
        SubChunkInfo[]?[] neighbors,
        int neighborIndex,
        int subChunkIndex)
    {
        SubChunkInfo[]? neighborSubChunks = neighbors[neighborIndex];

        if (neighborSubChunks is null)
        {
            // Neighbor not loaded: conservatively assume the face is NOT covered
            return false;
        }

        if (subChunkIndex >= neighborSubChunks.Length)
        {
            return false;
        }

        return neighborSubChunks[subChunkIndex].IsFullySolid;
    }
}
