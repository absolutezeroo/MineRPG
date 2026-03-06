using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using MineRPG.World.Chunks;

namespace MineRPG.World.Meshing;

/// <summary>
/// Identifies which sub-chunks need remeshing when a block changes.
/// Instead of remeshing the entire chunk (all 16 sub-chunks), only the
/// affected sub-chunk(s) are rebuilt. A block edit at a sub-chunk boundary
/// may affect the adjacent sub-chunk as well.
///
/// This reduces the remesh cost from ~3-5ms (full chunk) to ~0.2-0.5ms
/// (1-2 sub-chunks) for single block edits.
///
/// Thread-safe: all methods are static with no shared state.
/// </summary>
public static class IncrementalMeshUpdater
{
    /// <summary>
    /// Computes which sub-chunk indices need remeshing after a block change.
    /// A block at a sub-chunk boundary (Y % 16 == 0 or Y % 16 == 15) may
    /// affect the adjacent sub-chunk because the greedy mesher needs to know
    /// about blocks across the boundary for face culling and AO.
    /// </summary>
    /// <param name="localY">The local Y coordinate of the modified block.</param>
    /// <param name="affectedIndices">
    /// Pre-allocated list to receive the sub-chunk indices that need remeshing.
    /// Cleared before use.
    /// </param>
    [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists",
        Justification = "Hot-path method uses pre-allocated List for zero-alloc pattern.")]
    public static void GetAffectedSubChunks(int localY, List<int> affectedIndices)
    {
        affectedIndices.Clear();

        int subChunkIndex = localY / SubChunkConstants.SubChunkSize;
        subChunkIndex = Math.Clamp(subChunkIndex, 0, SubChunkConstants.SubChunkCount - 1);
        affectedIndices.Add(subChunkIndex);

        int localInSubChunk = localY % SubChunkConstants.SubChunkSize;

        // If at the bottom edge of a sub-chunk, the sub-chunk below is also affected
        if (localInSubChunk == 0 && subChunkIndex > 0)
        {
            affectedIndices.Add(subChunkIndex - 1);
        }

        // If at the top edge of a sub-chunk, the sub-chunk above is also affected
        if (localInSubChunk == SubChunkConstants.SubChunkSize - 1
            && subChunkIndex < SubChunkConstants.SubChunkCount - 1)
        {
            affectedIndices.Add(subChunkIndex + 1);
        }
    }

    /// <summary>
    /// Determines whether a block edit at the given local coordinates is near
    /// a chunk border (X=0, X=15, Z=0, Z=15), meaning neighbor chunks may
    /// also need remeshing.
    /// </summary>
    /// <param name="localX">Local X coordinate of the modified block.</param>
    /// <param name="localZ">Local Z coordinate of the modified block.</param>
    /// <returns>True if the edit is on a chunk border.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOnChunkBorder(int localX, int localZ)
    {
        return localX == 0 || localX == ChunkData.SizeX - 1
               || localZ == 0 || localZ == ChunkData.SizeZ - 1;
    }

    /// <summary>
    /// Creates a sub-chunk-level bitmask indicating which sub-chunks
    /// are affected by a block edit at the given Y coordinate.
    /// Bit N set = sub-chunk N needs remeshing.
    /// </summary>
    /// <param name="localY">The local Y coordinate of the modified block.</param>
    /// <returns>A bitmask of affected sub-chunk indices.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort GetAffectedSubChunkMask(int localY)
    {
        int subChunkIndex = Math.Clamp(
            localY / SubChunkConstants.SubChunkSize,
            0, SubChunkConstants.SubChunkCount - 1);

        ushort mask = (ushort)(1 << subChunkIndex);
        int localInSubChunk = localY % SubChunkConstants.SubChunkSize;

        if (localInSubChunk == 0 && subChunkIndex > 0)
        {
            mask |= (ushort)(1 << (subChunkIndex - 1));
        }

        if (localInSubChunk == SubChunkConstants.SubChunkSize - 1
            && subChunkIndex < SubChunkConstants.SubChunkCount - 1)
        {
            mask |= (ushort)(1 << (subChunkIndex + 1));
        }

        return mask;
    }
}
