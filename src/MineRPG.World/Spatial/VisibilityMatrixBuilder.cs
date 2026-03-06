using System;

using MineRPG.World.Chunks;

namespace MineRPG.World.Spatial;

/// <summary>
/// Computes a <see cref="ChunkVisibilityMatrix"/> for a chunk column by running
/// flood-fill BFS from each of the 4 horizontal faces. For each entry face, all
/// air blocks on that face are seed points; the BFS floods through 6-connected
/// air blocks and marks which exit faces are reachable.
///
/// Uses a per-sub-chunk approach: runs BFS independently per 16x16x16 sub-chunk,
/// then combines results. If ANY sub-chunk allows passage from face A to face B,
/// the column allows it. This keeps the BFS working set small (4096 blocks max).
///
/// Thread-safe: all state is local to each Build() call.
/// </summary>
public static class VisibilityMatrixBuilder
{
    private const int SubSize = SubChunkConstants.SubChunkSize;

    /// <summary>
    /// Builds the face-to-face visibility matrix for a full chunk column.
    /// Examines each non-empty, non-fully-solid sub-chunk independently.
    /// </summary>
    /// <param name="chunk">The chunk data to analyze.</param>
    /// <param name="subChunkInfos">Pre-computed sub-chunk metadata (may be null).</param>
    /// <returns>The combined visibility matrix for the chunk column.</returns>
    public static ChunkVisibilityMatrix Build(ChunkData chunk, SubChunkInfo[]? subChunkInfos)
    {
        ushort combinedBits = 0;

        for (int subChunkIndex = 0; subChunkIndex < SubChunkConstants.SubChunkCount; subChunkIndex++)
        {
            if (subChunkInfos is not null)
            {
                SubChunkInfo info = subChunkInfos[subChunkIndex];

                if (info.IsEmpty)
                {
                    // All air: every direction is passable
                    combinedBits |= 0xFFFF;
                    continue;
                }

                if (info.IsFullySolid)
                {
                    // All solid: no passage, skip
                    continue;
                }
            }

            int minY = subChunkIndex * SubSize;
            ushort subBits = BuildSubChunk(chunk, minY);
            combinedBits |= subBits;

            if (combinedBits == 0xFFFF)
            {
                // Early exit: all paths already reachable
                return ChunkVisibilityMatrix.AllVisible;
            }
        }

        return new ChunkVisibilityMatrix(combinedBits);
    }

    /// <summary>
    /// Runs BFS for a single 16x16x16 sub-chunk section starting at minY.
    /// Does 4 BFS passes (one per entry face), marking all reachable exit faces.
    /// </summary>
    private static ushort BuildSubChunk(ChunkData chunk, int minY)
    {
        ushort bits = 0;

        // Pre-check: if the sub-chunk has no air on any horizontal face, skip BFS
        bool hasNorthAir = false;
        bool hasSouthAir = false;
        bool hasEastAir = false;
        bool hasWestAir = false;

        for (int y = minY; y < minY + SubSize; y++)
        {
            for (int u = 0; u < ChunkData.SizeX; u++)
            {
                if (chunk.GetBlock(u, y, 0) == 0)
                {
                    hasNorthAir = true;
                }

                if (chunk.GetBlock(u, y, ChunkData.SizeZ - 1) == 0)
                {
                    hasSouthAir = true;
                }
            }

            for (int u = 0; u < ChunkData.SizeZ; u++)
            {
                if (chunk.GetBlock(ChunkData.SizeX - 1, y, u) == 0)
                {
                    hasEastAir = true;
                }

                if (chunk.GetBlock(0, y, u) == 0)
                {
                    hasWestAir = true;
                }
            }
        }

        if (hasNorthAir)
        {
            bits |= FloodFromFace(chunk, minY, ChunkVisibilityMatrix.FaceNorth);
        }

        if (hasSouthAir)
        {
            bits |= FloodFromFace(chunk, minY, ChunkVisibilityMatrix.FaceSouth);
        }

        if (hasEastAir)
        {
            bits |= FloodFromFace(chunk, minY, ChunkVisibilityMatrix.FaceEast);
        }

        if (hasWestAir)
        {
            bits |= FloodFromFace(chunk, minY, ChunkVisibilityMatrix.FaceWest);
        }

        return bits;
    }

    /// <summary>
    /// Floods from all air blocks on the given entry face through 6-connected air,
    /// returning the bits for which exit faces were reached.
    /// Uses a stack-based BFS with a visited bitmap to avoid heap allocation.
    /// </summary>
    private static ushort FloodFromFace(ChunkData chunk, int minY, int entryFace)
    {
        // visited: 16*16*16 = 4096 bits = 512 bytes = 64 ulongs
        Span<ulong> visited = stackalloc ulong[64];
        visited.Clear();

        // BFS queue: worst case 4096 entries, each encoded as ushort (x|z<<4|y<<8)
        // Use stackalloc for the queue buffer
        Span<ushort> queue = stackalloc ushort[4096];
        int queueHead = 0;
        int queueTail = 0;

        // Seed from the entry face
        SeedFace(chunk, minY, entryFace, visited, queue, ref queueTail);

        // BFS loop
        while (queueHead < queueTail)
        {
            ushort encoded = queue[queueHead++];
            int localX = encoded & 0xF;
            int localZ = (encoded >> 4) & 0xF;
            int localY = (encoded >> 8) & 0xF;

            // Expand to 6 neighbors
            if (localX > 0)
            {
                TryEnqueue(chunk, minY, localX - 1, localY, localZ, visited, queue, ref queueTail);
            }

            if (localX < ChunkData.SizeX - 1)
            {
                TryEnqueue(chunk, minY, localX + 1, localY, localZ, visited, queue, ref queueTail);
            }

            if (localZ > 0)
            {
                TryEnqueue(chunk, minY, localX, localY, localZ - 1, visited, queue, ref queueTail);
            }

            if (localZ < ChunkData.SizeZ - 1)
            {
                TryEnqueue(chunk, minY, localX, localY, localZ + 1, visited, queue, ref queueTail);
            }

            if (localY > 0)
            {
                TryEnqueue(chunk, minY, localX, localY - 1, localZ, visited, queue, ref queueTail);
            }

            if (localY < SubSize - 1)
            {
                TryEnqueue(chunk, minY, localX, localY + 1, localZ, visited, queue, ref queueTail);
            }
        }

        // Check which exit faces were reached
        ushort resultBits = 0;

        resultBits |= CheckFaceReached(visited, ChunkVisibilityMatrix.FaceNorth, entryFace);
        resultBits |= CheckFaceReached(visited, ChunkVisibilityMatrix.FaceSouth, entryFace);
        resultBits |= CheckFaceReached(visited, ChunkVisibilityMatrix.FaceEast, entryFace);
        resultBits |= CheckFaceReached(visited, ChunkVisibilityMatrix.FaceWest, entryFace);

        return resultBits;
    }

    private static void SeedFace(
        ChunkData chunk, int minY, int face,
        Span<ulong> visited, Span<ushort> queue, ref int queueTail)
    {
        for (int localY = 0; localY < SubSize; localY++)
        {
            int worldY = minY + localY;

            switch (face)
            {
                case ChunkVisibilityMatrix.FaceNorth:
                    for (int x = 0; x < ChunkData.SizeX; x++)
                    {
                        TryEnqueue(chunk, minY, x, localY, 0, visited, queue, ref queueTail);
                    }

                    break;

                case ChunkVisibilityMatrix.FaceSouth:
                    for (int x = 0; x < ChunkData.SizeX; x++)
                    {
                        TryEnqueue(chunk, minY, x, localY, ChunkData.SizeZ - 1, visited, queue, ref queueTail);
                    }

                    break;

                case ChunkVisibilityMatrix.FaceEast:
                    for (int z = 0; z < ChunkData.SizeZ; z++)
                    {
                        TryEnqueue(chunk, minY, ChunkData.SizeX - 1, localY, z, visited, queue, ref queueTail);
                    }

                    break;

                case ChunkVisibilityMatrix.FaceWest:
                    for (int z = 0; z < ChunkData.SizeZ; z++)
                    {
                        TryEnqueue(chunk, minY, 0, localY, z, visited, queue, ref queueTail);
                    }

                    break;
            }
        }
    }

    private static void TryEnqueue(
        ChunkData chunk, int minY,
        int localX, int localY, int localZ,
        Span<ulong> visited, Span<ushort> queue, ref int queueTail)
    {
        int worldY = minY + localY;
        ushort blockId = chunk.GetBlock(localX, worldY, localZ);

        if (blockId != 0)
        {
            return;
        }

        int flatIndex = localX + (localZ << 4) + (localY << 8);
        int longIndex = flatIndex >> 6;
        ulong bit = 1UL << (flatIndex & 63);

        if ((visited[longIndex] & bit) != 0)
        {
            return;
        }

        visited[longIndex] |= bit;
        ushort encoded = (ushort)(localX | (localZ << 4) | (localY << 8));

        if (queueTail < queue.Length)
        {
            queue[queueTail++] = encoded;
        }
    }

    private static ushort CheckFaceReached(
        ReadOnlySpan<ulong> visited, int exitFace, int entryFace)
    {
        for (int localY = 0; localY < SubSize; localY++)
        {
            switch (exitFace)
            {
                case ChunkVisibilityMatrix.FaceNorth:
                    for (int x = 0; x < ChunkData.SizeX; x++)
                    {
                        if (IsVisited(visited, x, localY, 0))
                        {
                            return (ushort)(1 << (entryFace * ChunkVisibilityMatrix.FaceCount + exitFace));
                        }
                    }

                    break;

                case ChunkVisibilityMatrix.FaceSouth:
                    for (int x = 0; x < ChunkData.SizeX; x++)
                    {
                        if (IsVisited(visited, x, localY, ChunkData.SizeZ - 1))
                        {
                            return (ushort)(1 << (entryFace * ChunkVisibilityMatrix.FaceCount + exitFace));
                        }
                    }

                    break;

                case ChunkVisibilityMatrix.FaceEast:
                    for (int z = 0; z < ChunkData.SizeZ; z++)
                    {
                        if (IsVisited(visited, ChunkData.SizeX - 1, localY, z))
                        {
                            return (ushort)(1 << (entryFace * ChunkVisibilityMatrix.FaceCount + exitFace));
                        }
                    }

                    break;

                case ChunkVisibilityMatrix.FaceWest:
                    for (int z = 0; z < ChunkData.SizeZ; z++)
                    {
                        if (IsVisited(visited, 0, localY, z))
                        {
                            return (ushort)(1 << (entryFace * ChunkVisibilityMatrix.FaceCount + exitFace));
                        }
                    }

                    break;
            }
        }

        return 0;
    }

    private static bool IsVisited(ReadOnlySpan<ulong> visited, int localX, int localY, int localZ)
    {
        int flatIndex = localX + (localZ << 4) + (localY << 8);
        int longIndex = flatIndex >> 6;
        ulong bit = 1UL << (flatIndex & 63);
        return (visited[longIndex] & bit) != 0;
    }
}
