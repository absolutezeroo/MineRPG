using System;
using System.Buffers;

using MineRPG.World.Blocks;
using MineRPG.World.Chunks;

namespace MineRPG.World.Meshing;

/// <summary>
/// Implements the greedy meshing merge pass. Scans a 2D mask of visible
/// block faces and merges contiguous same-block faces into larger quads.
/// Routes each merged quad to the appropriate accumulator (opaque or liquid).
///
/// All methods are static for hot-path performance.
/// </summary>
internal static class GreedyMeshAlgorithm
{
    /// <summary>
    /// Performs greedy merging on a pre-built face mask for one slice.
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
    /// <param name="opaque">Accumulator for opaque quads.</param>
    /// <param name="liquid">Accumulator for liquid quads.</param>
    public static void Merge(
        ushort[] mask, int uCount, int vCount,
        int faceDirection, int depthAxis, int uAxis, int vAxis, int slice,
        int normalX, int normalY, int normalZ,
        ChunkData chunk, ChunkData?[] neighbors,
        BlockRegistry blockRegistry,
        ChunkMeshBuilder.MeshAccumulator opaque,
        ChunkMeshBuilder.MeshAccumulator liquid)
    {
        bool[] merged = ArrayPool<bool>.Shared.Rent(uCount * vCount);
        Array.Clear(merged, 0, uCount * vCount);

        try
        {
            for (int vi = 0; vi < vCount; vi++)
            {
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

                    while (canExpand && vi + height < vCount)
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

                    BlockDefinition definition = blockRegistry.Get(blockId);
                    ChunkMeshBuilder.MeshAccumulator target = definition.IsLiquid ? liquid : opaque;
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
