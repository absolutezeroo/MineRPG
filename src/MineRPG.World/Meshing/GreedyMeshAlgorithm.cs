using System;
using System.Buffers;

using MineRPG.World.Blocks;
using MineRPG.World.Chunks;

namespace MineRPG.World.Meshing;

/// <summary>
/// Implements the greedy meshing merge pass. Scans a 2D mask of visible
/// block faces and merges contiguous same-block faces into larger quads.
/// Routes each merged quad to the appropriate sub-chunk accumulator (opaque or liquid).
///
/// Height expansion is constrained at sub-chunk Y boundaries (multiples of 16)
/// when the V axis is Y, ensuring each quad belongs to exactly one sub-chunk.
///
/// All methods are static for hot-path performance.
/// </summary>
internal static class GreedyMeshAlgorithm
{
    private const int AxisY = 1;

    /// <summary>
    /// Performs greedy merging on a pre-built face mask for one slice.
    /// Routes quads to per-sub-chunk accumulators based on block Y coordinate.
    /// </summary>
    /// <param name="mask">2D mask of visible block IDs (u * vCount layout).</param>
    /// <param name="uCount">Width of the mask.</param>
    /// <param name="vCount">Height of the mask.</param>
    /// <param name="faceDirection">Face direction index (0-5).</param>
    /// <param name="depthAxis">Depth axis for this face direction.</param>
    /// <param name="uAxis">U axis for this face direction.</param>
    /// <param name="vAxis">V axis for this face direction.</param>
    /// <param name="slice">Current slice index along depth axis.</param>
    /// <param name="normalX">Face normal X component.</param>
    /// <param name="normalY">Face normal Y component.</param>
    /// <param name="normalZ">Face normal Z component.</param>
    /// <param name="chunk">The chunk being meshed.</param>
    /// <param name="neighbors">Cardinal neighbor chunks.</param>
    /// <param name="blockRegistry">Block registry for definition lookups.</param>
    /// <param name="subChunkAccumulators">Per-sub-chunk accumulator pairs.</param>
    public static void Merge(
        ushort[] mask, int uCount, int vCount,
        int faceDirection, int depthAxis, int uAxis, int vAxis, int slice,
        int normalX, int normalY, int normalZ,
        ChunkData chunk, ChunkData?[] neighbors,
        BlockRegistry blockRegistry,
        SubChunkAccumulators[] subChunkAccumulators)
    {
        bool[] merged = ArrayPool<bool>.Shared.Rent(uCount * vCount);
        Array.Clear(merged, 0, uCount * vCount);

        bool vAxisIsY = vAxis == AxisY;

        try
        {
            for (int vi = 0; vi < vCount; vi++)
            {
                // When V axis is Y, limit height expansion to the current sub-chunk boundary
                int maxV = vAxisIsY
                    ? Math.Min(((vi / SubChunkConstants.SubChunkSize) + 1) * SubChunkConstants.SubChunkSize, vCount)
                    : vCount;

                for (int ui = 0; ui < uCount; ui++)
                {
                    int index = ui + vi * uCount;

                    if (mask[index] == 0 || merged[index])
                    {
                        continue;
                    }

                    ushort blockId = mask[index];

                    int width = 1;

                    while (ui + width < uCount
                           && mask[(ui + width) + vi * uCount] == blockId
                           && !merged[(ui + width) + vi * uCount])
                    {
                        width++;
                    }

                    int height = 1;
                    bool canExpand = true;

                    while (canExpand && vi + height < maxV)
                    {
                        for (int k = 0; k < width; k++)
                        {
                            int mergeIndex = (ui + k) + (vi + height) * uCount;

                            if (mask[mergeIndex] != blockId || merged[mergeIndex])
                            {
                                canExpand = false;
                                break;
                            }
                        }

                        if (canExpand)
                        {
                            height++;
                        }
                    }

                    for (int dv = 0; dv < height; dv++)
                    {
                        for (int du = 0; du < width; du++)
                        {
                            merged[(ui + du) + (vi + dv) * uCount] = true;
                        }
                    }

                    // Determine block Y to route to the correct sub-chunk accumulator
                    int blockY = vAxisIsY ? vi : slice;
                    int subChunkIndex = Math.Min(
                        blockY / SubChunkConstants.SubChunkSize,
                        SubChunkConstants.SubChunkCount - 1);

                    BlockDefinition definition = blockRegistry.Get(blockId);
                    ChunkMeshBuilder.MeshAccumulator target = definition.IsLiquid
                        ? subChunkAccumulators[subChunkIndex].Liquid
                        : subChunkAccumulators[subChunkIndex].Opaque;

                    int offset = (normalX + normalY + normalZ) > 0 ? 1 : 0;

                    QuadEmitter.Emit(depthAxis, uAxis, vAxis, slice, offset, ui, vi, width, height,
                        normalX, normalY, normalZ, definition, faceDirection,
                        chunk, neighbors, blockRegistry, target);
                }
            }
        }
        finally
        {
            ArrayPool<bool>.Shared.Return(merged);
        }
    }
}
