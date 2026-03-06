using System;

using MineRPG.World.Chunks;

namespace MineRPG.World.Spatial;

/// <summary>
/// Stack-based BFS flood fill for a single 16x16x16 sub-chunk section.
/// Floods from air blocks on an entry face through 6-connected air blocks
/// and determines which exit faces are reachable.
/// All state is stack-allocated — no heap allocations.
/// </summary>
internal static class SubChunkFloodFill
{
    private const int SubSize = SubChunkConstants.SubChunkSize;

    /// <summary>
    /// Floods from all air blocks on the given entry face through 6-connected air,
    /// returning the bits for which exit faces were reached.
    /// </summary>
    /// <param name="chunk">The chunk data to analyze.</param>
    /// <param name="minY">The world Y of the sub-chunk bottom.</param>
    /// <param name="entryFace">The face to seed from.</param>
    /// <returns>Bitmask of reachable entry-to-exit face pairs.</returns>
    public static ushort FloodFromFace(ChunkData chunk, int minY, int entryFace)
    {
        Span<ulong> visited = stackalloc ulong[64];
        visited.Clear();

        Span<ushort> queue = stackalloc ushort[4096];
        int queueHead = 0;
        int queueTail = 0;

        SeedFace(chunk, minY, entryFace, visited, queue, ref queueTail);

        while (queueHead < queueTail)
        {
            ushort encoded = queue[queueHead++];
            int localX = encoded & 0xF;
            int localZ = (encoded >> 4) & 0xF;
            int localY = (encoded >> 8) & 0xF;

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
