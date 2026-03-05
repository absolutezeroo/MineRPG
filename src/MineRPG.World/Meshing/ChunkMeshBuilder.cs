using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

using MineRPG.World.Blocks;
using MineRPG.World.Chunks;

namespace MineRPG.World.Meshing;

/// <summary>
/// Greedy mesh builder with per-vertex ambient occlusion.
///
/// For each of the 6 face directions, scans the chunk slice by slice.
/// On each slice, builds a 2D mask of visible faces then delegates
/// merging to <see cref="GreedyMeshAlgorithm"/>, AO to
/// <see cref="AmbientOcclusionCalculator"/>, and quad emission to
/// <see cref="QuadEmitter"/>.
///
/// Produces separate mesh data for opaque and liquid faces so they
/// can be rendered with different materials (opaque vs translucent).
///
/// Thread-safe: all state is local to each Build() call.
/// </summary>
public sealed class ChunkMeshBuilder : IChunkMeshBuilder
{
    private const int FaceDirectionCount = 6;
    private const int InitialOpaqueCapacity = 4096;
    private const int InitialLiquidCapacity = 512;

    private readonly BlockRegistry _blockRegistry;

    /// <summary>
    /// Creates a chunk mesh builder with the given block registry.
    /// </summary>
    /// <param name="blockRegistry">Block registry for looking up block definitions.</param>
    public ChunkMeshBuilder(BlockRegistry blockRegistry)
    {
        _blockRegistry = blockRegistry;
    }

    /// <summary>
    /// Builds mesh data for a chunk using greedy meshing with per-vertex AO.
    /// </summary>
    /// <param name="chunk">The chunk data to mesh.</param>
    /// <param name="neighbors">Data from the 4 cardinal neighbor chunks.</param>
    /// <param name="cancellationToken">Token to cancel the meshing operation.</param>
    /// <returns>Separate mesh data for opaque and liquid surfaces.</returns>
    public ChunkMeshResult Build(ChunkData chunk, ChunkData?[] neighbors, CancellationToken cancellationToken)
    {
        MeshAccumulator opaque = new(InitialOpaqueCapacity);
        MeshAccumulator liquid = new(InitialLiquidCapacity);

        for (int faceDirection = 0; faceDirection < FaceDirectionCount; faceDirection++)
        {
            BuildFaceDirection(faceDirection, chunk, neighbors, opaque, liquid);
        }

        return new ChunkMeshResult(opaque.ToMeshData(), liquid.ToMeshData());
    }

    private void BuildFaceDirection(
        int faceDirection,
        ChunkData chunk,
        ChunkData?[] neighbors,
        MeshAccumulator opaque,
        MeshAccumulator liquid)
    {
        MeshCoordHelper.GetAxes(faceDirection, out int depthAxis, out int uAxis, out int vAxis);
        (int normalX, int normalY, int normalZ) = MeshCoordHelper.GetNormal(faceDirection);

        int sliceCount = MeshCoordHelper.GetDimension(depthAxis);
        int uCount = MeshCoordHelper.GetDimension(uAxis);
        int vCount = MeshCoordHelper.GetDimension(vAxis);

        ushort[] mask = ArrayPool<ushort>.Shared.Rent(uCount * vCount);

        try
        {
            for (int slice = 0; slice < sliceCount; slice++)
            {
                Array.Clear(mask, 0, uCount * vCount);

                for (int ui = 0; ui < uCount; ui++)
                {
                    for (int vi = 0; vi < vCount; vi++)
                    {
                        MeshCoordHelper.ResolveCoord(depthAxis, uAxis, vAxis, slice, ui, vi,
                            out int positionX, out int positionY, out int positionZ);
                        ushort blockId = chunk.GetBlock(positionX, positionY, positionZ);

                        if (blockId == 0)
                        {
                            continue;
                        }

                        BlockDefinition definition = _blockRegistry.Get(blockId);

                        if (definition.IsTransparent && !definition.IsLiquid)
                        {
                            continue;
                        }

                        ushort neighborBlockId = BlockSampler.SampleBlock(chunk, neighbors,
                            positionX + normalX, positionY + normalY, positionZ + normalZ);
                        BlockDefinition neighborDefinition = _blockRegistry.Get(neighborBlockId);

                        if (neighborBlockId == 0
                            || (neighborDefinition.IsTransparent && neighborBlockId != blockId))
                        {
                            mask[ui + vi * uCount] = blockId;
                        }
                    }
                }

                GreedyMeshAlgorithm.Merge(mask, uCount, vCount, faceDirection, depthAxis, uAxis, vAxis, slice,
                    normalX, normalY, normalZ, chunk, neighbors, _blockRegistry, opaque, liquid);
            }
        }
        finally
        {
            ArrayPool<ushort>.Shared.Return(mask);
        }
    }

    /// <summary>
    /// Collects mesh vertex data into lists, then converts to MeshData.
    /// Avoids passing 6 lists through the entire call chain.
    /// </summary>
    internal sealed class MeshAccumulator
    {
        public readonly List<float> Vertices;
        public readonly List<float> Normals;
        public readonly List<float> Uvs;
        public readonly List<float> Uv2s;
        public readonly List<float> Colors;
        public readonly List<int> Indices;

        public MeshAccumulator(int initialCapacity)
        {
            Vertices = new List<float>(initialCapacity);
            Normals = new List<float>(initialCapacity);
            Uvs = new List<float>(initialCapacity / 2);
            Uv2s = new List<float>(initialCapacity / 2);
            Colors = new List<float>(initialCapacity);
            Indices = new List<int>(initialCapacity * 3 / 2);
        }

        public MeshData ToMeshData()
        {
            if (Vertices.Count == 0)
            {
                return MeshData.Empty;
            }

            return new MeshData(
                Vertices.ToArray(),
                Normals.ToArray(),
                Uvs.ToArray(),
                Uv2s.ToArray(),
                Colors.ToArray(),
                Indices.ToArray());
        }
    }
}
