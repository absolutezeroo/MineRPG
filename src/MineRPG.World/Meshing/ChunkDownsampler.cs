using System;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.World.Meshing;

/// <summary>
/// Downsamples chunk data for LOD meshing by merging NxNxN blocks into one
/// representative "mega-block". The representative is the most common non-air
/// block in the group. If all blocks are air, the result is air.
///
/// LOD 1: 2x2x2 grouping → 8x128x8 output grid (simulated as chunk data).
/// LOD 2: 4x4x4 grouping → 4x64x4 output grid.
///
/// Thread-safe: all state is local to each Downsample() call.
/// </summary>
public static class ChunkDownsampler
{
    private const int MaxBlockTypes = 64;

    /// <summary>
    /// Downsamples a chunk's block data into a smaller output array.
    /// Each NxNxN group of source blocks becomes one output block.
    /// The output block ID is the majority non-air block in the group.
    /// </summary>
    /// <param name="chunk">The source chunk data.</param>
    /// <param name="factor">The downsampling factor per axis (2 or 4).</param>
    /// <param name="output">
    /// Pre-allocated output array. Size must be at least
    /// (SizeX/factor) * (SizeY/factor) * (SizeZ/factor).
    /// </param>
    /// <param name="outputSizeX">The output X dimension.</param>
    /// <param name="outputSizeY">The output Y dimension.</param>
    /// <param name="outputSizeZ">The output Z dimension.</param>
    public static void Downsample(
        ChunkData chunk,
        int factor,
        ushort[] output,
        out int outputSizeX,
        out int outputSizeY,
        out int outputSizeZ)
    {
        outputSizeX = ChunkData.SizeX / factor;
        outputSizeY = ChunkData.SizeY / factor;
        outputSizeZ = ChunkData.SizeZ / factor;

        int outputTotal = outputSizeX * outputSizeY * outputSizeZ;
        Array.Clear(output, 0, Math.Min(output.Length, outputTotal));

        // Frequency table for majority vote — reused per mega-block
        Span<int> counts = stackalloc int[MaxBlockTypes];
        Span<ushort> blockIds = stackalloc ushort[MaxBlockTypes];

        for (int outY = 0; outY < outputSizeY; outY++)
        {
            int srcMinY = outY * factor;
            int srcMaxY = Math.Min(srcMinY + factor, ChunkData.SizeY);

            for (int outZ = 0; outZ < outputSizeZ; outZ++)
            {
                int srcMinZ = outZ * factor;
                int srcMaxZ = Math.Min(srcMinZ + factor, ChunkData.SizeZ);

                for (int outX = 0; outX < outputSizeX; outX++)
                {
                    int srcMinX = outX * factor;
                    int srcMaxX = Math.Min(srcMinX + factor, ChunkData.SizeX);

                    ushort result = FindMajorityBlock(
                        chunk, srcMinX, srcMaxX, srcMinY, srcMaxY, srcMinZ, srcMaxZ,
                        counts, blockIds);

                    int outIndex = outX + outZ * outputSizeX + outY * outputSizeX * outputSizeZ;
                    output[outIndex] = result;
                }
            }
        }
    }

    /// <summary>
    /// Finds the most common non-air block in a 3D region of the chunk.
    /// Returns 0 (air) if the region is entirely air.
    /// </summary>
    private static ushort FindMajorityBlock(
        ChunkData chunk,
        int minX, int maxX,
        int minY, int maxY,
        int minZ, int maxZ,
        Span<int> counts,
        Span<ushort> blockIds)
    {
        int uniqueCount = 0;
        counts.Clear();

        for (int y = minY; y < maxY; y++)
        {
            for (int z = minZ; z < maxZ; z++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    ushort blockId = chunk.GetBlock(x, y, z);

                    if (blockId == 0)
                    {
                        continue;
                    }

                    // Linear search for the block ID in our small list
                    int found = -1;

                    for (int i = 0; i < uniqueCount; i++)
                    {
                        if (blockIds[i] == blockId)
                        {
                            found = i;
                            break;
                        }
                    }

                    if (found >= 0)
                    {
                        counts[found]++;
                    }
                    else if (uniqueCount < MaxBlockTypes)
                    {
                        blockIds[uniqueCount] = blockId;
                        counts[uniqueCount] = 1;
                        uniqueCount++;
                    }
                }
            }
        }

        if (uniqueCount == 0)
        {
            return 0;
        }

        int bestIndex = 0;
        int bestCount = counts[0];

        for (int i = 1; i < uniqueCount; i++)
        {
            if (counts[i] > bestCount)
            {
                bestCount = counts[i];
                bestIndex = i;
            }
        }

        return blockIds[bestIndex];
    }

    /// <summary>
    /// Returns the required output buffer size for a given downsampling factor.
    /// </summary>
    /// <param name="factor">The downsampling factor (2 or 4).</param>
    /// <returns>The number of elements needed in the output array.</returns>
    public static int GetOutputSize(int factor)
    {
        int outX = ChunkData.SizeX / factor;
        int outY = ChunkData.SizeY / factor;
        int outZ = ChunkData.SizeZ / factor;
        return outX * outY * outZ;
    }

    /// <summary>
    /// Creates a standard-size <see cref="ChunkData"/> filled with expanded downsampled blocks.
    /// Each downsampled block is written to all positions in its factor-sized region
    /// to avoid spurious internal faces when meshing.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <param name="downsampled">The downsampled block array from <see cref="Downsample"/>.</param>
    /// <param name="outSizeX">The output X dimension from <see cref="Downsample"/>.</param>
    /// <param name="outSizeY">The output Y dimension from <see cref="Downsample"/>.</param>
    /// <param name="outSizeZ">The output Z dimension from <see cref="Downsample"/>.</param>
    /// <param name="factor">The downsampling factor used.</param>
    /// <returns>A full-size ChunkData with expanded blocks.</returns>
    public static ChunkData Expand(
        ChunkCoord coord,
        ushort[] downsampled,
        int outSizeX,
        int outSizeY,
        int outSizeZ,
        int factor)
    {
        ChunkData expandedChunk = new(coord);

        for (int outY = 0; outY < outSizeY; outY++)
        {
            for (int outZ = 0; outZ < outSizeZ; outZ++)
            {
                for (int outX = 0; outX < outSizeX; outX++)
                {
                    int srcIndex = outX + outZ * outSizeX + outY * outSizeX * outSizeZ;
                    ushort blockId = downsampled[srcIndex];

                    if (blockId == 0)
                    {
                        continue;
                    }

                    for (int dy = 0; dy < factor; dy++)
                    {
                        for (int dz = 0; dz < factor; dz++)
                        {
                            for (int dx = 0; dx < factor; dx++)
                            {
                                int worldX = outX * factor + dx;
                                int worldY = outY * factor + dy;
                                int worldZ = outZ * factor + dz;

                                if (ChunkData.IsInBounds(worldX, worldY, worldZ))
                                {
                                    expandedChunk.SetBlock(worldX, worldY, worldZ, blockId);
                                }
                            }
                        }
                    }
                }
            }
        }

        return expandedChunk;
    }
}
