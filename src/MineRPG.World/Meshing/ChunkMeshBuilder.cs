using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using MineRPG.World.Blocks;
using MineRPG.World.Chunks;

namespace MineRPG.World.Meshing;

/// <summary>
/// Greedy mesh builder with per-vertex ambient occlusion.
///
/// For each of the 6 face directions, scans the chunk slice by slice.
/// On each slice, builds a 2D mask of visible faces then merges
/// contiguous same-block faces into larger quads.
///
/// Produces separate mesh data for opaque and liquid faces so they
/// can be rendered with different materials (opaque vs translucent).
///
/// UV channels:
///   UV  = tiling coordinates in block units (0..width, 0..height).
///   UV2 = atlas tile origin (u0, v0).
///
/// Vertex color:
///   RGB = block tint from definition.
///   A   = vertex ambient occlusion (0 = fully occluded, 1 = fully lit).
///
/// Thread-safe: all state is local to each Build() call.
///
/// NOTE: This file exceeds 300 lines because the greedy meshing algorithm,
/// AO computation, and quad emission are tightly coupled. Splitting would
/// fragment a single cohesive algorithm across files with no clarity gain.
/// </summary>
public sealed class ChunkMeshBuilder : IChunkMeshBuilder
{
    private const int ChunkSizeX = ChunkData.SizeX;
    private const int ChunkSizeY = ChunkData.SizeY;
    private const int ChunkSizeZ = ChunkData.SizeZ;
    private const int FaceDirectionCount = 6;
    private const int VerticesPerQuad = 4;
    private const int ComponentsPerVertex = 3;
    private const int UvComponentsPerFace = 4;
    private const int NeighborCount = 4;
    private const int AxisX = 0;
    private const int AxisY = 1;
    private const int AxisZ = 2;
    private const int InitialOpaqueCapacity = 4096;
    private const int InitialLiquidCapacity = 512;
    private const float AoOcclusionFull = 0f;
    private const float AoDivisor = 3f;

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
        GetAxes(faceDirection, out int depthAxis, out int uAxis, out int vAxis);
        (int normalX, int normalY, int normalZ) = GetNormal(faceDirection);

        int sliceCount = GetDimension(depthAxis);
        int uCount = GetDimension(uAxis);
        int vCount = GetDimension(vAxis);

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
                        ResolveCoord(depthAxis, uAxis, vAxis, slice, ui, vi,
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

                        ushort neighborBlockId = SampleBlock(chunk, neighbors,
                            positionX + normalX, positionY + normalY, positionZ + normalZ);
                        BlockDefinition neighborDefinition = _blockRegistry.Get(neighborBlockId);

                        // Emit face if neighbor is air, or transparent and not the same block type
                        // (prevents interior liquid-to-liquid faces causing z-fighting)
                        if (neighborBlockId == 0
                            || (neighborDefinition.IsTransparent && neighborBlockId != blockId))
                        {
                            mask[ui + vi * uCount] = blockId;
                        }
                    }
                }

                GreedyMerge(mask, uCount, vCount, faceDirection, depthAxis, uAxis, vAxis, slice,
                    normalX, normalY, normalZ, chunk, neighbors, opaque, liquid);
            }
        }
        finally
        {
            ArrayPool<ushort>.Shared.Return(mask);
        }
    }

    private void GreedyMerge(
        ushort[] mask, int uCount, int vCount,
        int faceDirection, int depthAxis, int uAxis, int vAxis, int slice,
        int normalX, int normalY, int normalZ,
        ChunkData chunk, ChunkData?[] neighbors,
        MeshAccumulator opaque, MeshAccumulator liquid)
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

                    // Expand in u direction
                    int width = 1;

                    while (ui + width < uCount
                           && mask[(ui + width) + vi * uCount] == blockId
                           && !merged[(ui + width) + vi * uCount])
                    {
                        width++;
                    }

                    // Expand in v direction
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

                    // Mark as merged
                    for (int dv = 0; dv < height; dv++)
                    {
                        for (int du = 0; du < width; du++)
                        {
                            merged[(ui + du) + (vi + dv) * uCount] = true;
                        }
                    }

                    // Route to opaque or liquid accumulator
                    BlockDefinition definition = _blockRegistry.Get(blockId);
                    MeshAccumulator target = definition.IsLiquid ? liquid : opaque;
                    int offset = (normalX + normalY + normalZ) > 0 ? 1 : 0;

                    EmitQuad(depthAxis, uAxis, vAxis, slice, offset, ui, vi, width, height,
                        normalX, normalY, normalZ, definition, faceDirection,
                        chunk, neighbors, target);
                }
            }
        }
        finally
        {
            ArrayPool<bool>.Shared.Return(merged);
        }
    }

    private void EmitQuad(
        int depthAxis, int uAxis, int vAxis,
        int slice, int offset,
        int ui, int vi, int width, int height,
        int normalX, int normalY, int normalZ,
        BlockDefinition definition, int faceDirection,
        ChunkData chunk, ChunkData?[] neighbors,
        MeshAccumulator target)
    {
        // Corner offsets: (du, dv) for each of the 4 quad vertices
        Span<int> cornerU = stackalloc int[VerticesPerQuad];
        Span<int> cornerV = stackalloc int[VerticesPerQuad];

        // Godot uses CW front-face (Vulkan convention). The Z-axis permutation
        // (Z,X,Y) is even, unlike X(X,Z,Y) and Y(Y,X,Z) which are odd.
        // This inverts the Z flip compared to X/Y: flip for -X, -Y, +Z.
        if ((normalX + normalY - normalZ) < 0)
        {
            cornerU[0] = 0;
            cornerV[0] = 0;
            cornerU[1] = 0;
            cornerV[1] = height;
            cornerU[2] = width;
            cornerV[2] = height;
            cornerU[3] = width;
            cornerV[3] = 0;
        }
        else
        {
            cornerU[0] = 0;
            cornerV[0] = 0;
            cornerU[1] = width;
            cornerV[1] = 0;
            cornerU[2] = width;
            cornerV[2] = height;
            cornerU[3] = 0;
            cornerV[3] = height;
        }

        // UV = tiling coords so the shader can fract() per block.
        // UV2 = atlas tile origin.
        float tileU0 = definition.FaceUvs[faceDirection * UvComponentsPerFace + 0];
        float tileV0 = definition.FaceUvs[faceDirection * UvComponentsPerFace + 1];

        // Tiling UV corners must match vertex corners 1:1.
        Span<float> tilingU = stackalloc float[VerticesPerQuad];
        Span<float> tilingV = stackalloc float[VerticesPerQuad];

        if ((normalX + normalY - normalZ) < 0)
        {
            tilingU[0] = 0;
            tilingV[0] = 0;
            tilingU[1] = 0;
            tilingV[1] = height;
            tilingU[2] = width;
            tilingV[2] = height;
            tilingU[3] = width;
            tilingV[3] = 0;
        }
        else
        {
            tilingU[0] = 0;
            tilingV[0] = 0;
            tilingU[1] = width;
            tilingV[1] = 0;
            tilingU[2] = width;
            tilingV[2] = height;
            tilingU[3] = 0;
            tilingV[3] = height;
        }

        // Side faces (v-axis = Y): flip tiling V so textures aren't upside-down.
        // Godot UV v=0 is top of texture, but world Y=0 is bottom of block.
        if (vAxis == AxisY)
        {
            for (int i = 0; i < VerticesPerQuad; i++)
            {
                tilingV[i] = height - tilingV[i];
            }
        }

        // Compute per-vertex AO
        int airDepth = slice + ((normalX + normalY + normalZ) > 0 ? 1 : -1);
        Span<float> ambientOcclusion = stackalloc float[VerticesPerQuad];
        int baseVertex = target.Vertices.Count / ComponentsPerVertex;

        for (int i = 0; i < VerticesPerQuad; i++)
        {
            int du = cornerU[i];
            int dv = cornerV[i];

            ResolveCoord(depthAxis, uAxis, vAxis, slice + offset, ui + du, vi + dv,
                out int cornerX, out int cornerY, out int cornerZ);

            target.Vertices.Add(cornerX);
            target.Vertices.Add(cornerY);
            target.Vertices.Add(cornerZ);

            target.Normals.Add(normalX);
            target.Normals.Add(normalY);
            target.Normals.Add(normalZ);

            target.Uvs.Add(tilingU[i]);
            target.Uvs.Add(tilingV[i]);

            target.Uv2s.Add(tileU0);
            target.Uv2s.Add(tileV0);

            // Compute AO for this vertex
            ambientOcclusion[i] = ComputeVertexAO(chunk, neighbors,
                depthAxis, uAxis, vAxis, airDepth, ui + du, vi + dv, du, dv);

            target.Colors.Add(definition.TintR);
            target.Colors.Add(definition.TintG);
            target.Colors.Add(definition.TintB);
            target.Colors.Add(ambientOcclusion[i]);
        }

        // Quad flip: choose the diagonal that minimizes AO interpolation artifacts.
        // When ao[0]+ao[2] > ao[1]+ao[3], the standard diagonal produces smoother
        // interpolation. Otherwise flip to reduce the visible seam.
        if (ambientOcclusion[0] + ambientOcclusion[2] > ambientOcclusion[1] + ambientOcclusion[3])
        {
            target.Indices.Add(baseVertex);
            target.Indices.Add(baseVertex + 1);
            target.Indices.Add(baseVertex + 2);
            target.Indices.Add(baseVertex);
            target.Indices.Add(baseVertex + 2);
            target.Indices.Add(baseVertex + 3);
        }
        else
        {
            target.Indices.Add(baseVertex + 1);
            target.Indices.Add(baseVertex + 2);
            target.Indices.Add(baseVertex + 3);
            target.Indices.Add(baseVertex + 1);
            target.Indices.Add(baseVertex + 3);
            target.Indices.Add(baseVertex);
        }
    }

    /// <summary>
    /// Computes ambient occlusion for a vertex at position (uVertex, vVertex)
    /// in the face plane. Samples 3 neighboring blocks at the air level:
    /// two edge neighbors and one corner neighbor.
    ///
    /// Returns 0.0 (fully occluded) to 1.0 (fully lit).
    /// Uses the standard voxel AO formula: if both edges are solid, AO = 0.
    /// Otherwise AO = (3 - solidCount) / 3.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float ComputeVertexAO(
        ChunkData chunk, ChunkData?[] neighbors,
        int depthAxis, int uAxis, int vAxis,
        int airDepth, int uVertex, int vVertex,
        int du, int dv)
    {
        // Determine which blocks around this vertex to check.
        // The vertex sits at the corner of 4 blocks. One is known air (the face block).
        // The "other" direction points away from the quad interior.
        int uOther = (du == 0) ? -1 : 0;
        int vOther = (dv == 0) ? -1 : 0;
        int uAir = (du == 0) ? 0 : -1;
        int vAir = (dv == 0) ? 0 : -1;

        bool side1 = IsSolidAt(chunk, neighbors, depthAxis, uAxis, vAxis,
            airDepth, uVertex + uOther, vVertex + vAir);
        bool side2 = IsSolidAt(chunk, neighbors, depthAxis, uAxis, vAxis,
            airDepth, uVertex + uAir, vVertex + vOther);

        if (side1 && side2)
        {
            return AoOcclusionFull;
        }

        bool corner = IsSolidAt(chunk, neighbors, depthAxis, uAxis, vAxis,
            airDepth, uVertex + uOther, vVertex + vOther);
        int count = (side1 ? 1 : 0) + (side2 ? 1 : 0) + (corner ? 1 : 0);
        return (AoDivisor - count) / AoDivisor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSolidAt(
        ChunkData chunk, ChunkData?[] neighbors,
        int depthAxis, int uAxis, int vAxis,
        int depthValue, int uValue, int vValue)
    {
        int x = 0, y = 0, z = 0;
        SetAxis(depthAxis, depthValue, ref x, ref y, ref z);
        SetAxis(uAxis, uValue, ref x, ref y, ref z);
        SetAxis(vAxis, vValue, ref x, ref y, ref z);

        ushort blockId = SampleBlock(chunk, neighbors, x, y, z);
        return blockId != 0 && _blockRegistry.Get(blockId).IsSolid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetAxis(int axis, int value, ref int x, ref int y, ref int z)
    {
        switch (axis)
        {
            case AxisX:
                x = value;
                break;
            case AxisY:
                y = value;
                break;
            default:
                z = value;
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ResolveCoord(int depthAxis, int uAxis, int vAxis,
        int depthValue, int uValue, int vValue,
        out int x, out int y, out int z)
    {
        x = 0;
        y = 0;
        z = 0;
        SetAxis(depthAxis, depthValue, ref x, ref y, ref z);
        SetAxis(uAxis, uValue, ref x, ref y, ref z);
        SetAxis(vAxis, vValue, ref x, ref y, ref z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetDimension(int axis) => axis switch
    {
        AxisX => ChunkSizeX,
        AxisY => ChunkSizeY,
        _ => ChunkSizeZ,
    };

    private static ushort SampleBlock(ChunkData main, ChunkData?[] neighbors, int worldX, int worldY, int worldZ)
    {
        if (worldY is < 0 or >= ChunkData.SizeY)
        {
            return 0;
        }

        if (ChunkData.IsInBounds(worldX, worldY, worldZ))
        {
            return main.GetBlock(worldX, worldY, worldZ);
        }

        int neighborDirectionX = 0, neighborDirectionZ = 0;
        int localX = worldX, localZ = worldZ;

        if (worldX < 0)
        {
            neighborDirectionX = -1;
            localX = worldX + ChunkData.SizeX;
        }
        else if (worldX >= ChunkData.SizeX)
        {
            neighborDirectionX = 1;
            localX = worldX - ChunkData.SizeX;
        }

        if (worldZ < 0)
        {
            neighborDirectionZ = -1;
            localZ = worldZ + ChunkData.SizeZ;
        }
        else if (worldZ >= ChunkData.SizeZ)
        {
            neighborDirectionZ = 1;
            localZ = worldZ - ChunkData.SizeZ;
        }

        // neighbors: [0]=+X, [1]=-X, [2]=+Z, [3]=-Z
        ChunkData? neighbor = null;

        if (neighborDirectionX == 1)
        {
            neighbor = neighbors[0];
        }
        else if (neighborDirectionX == -1)
        {
            neighbor = neighbors[1];
        }
        else if (neighborDirectionZ == 1)
        {
            neighbor = neighbors[2];
        }
        else if (neighborDirectionZ == -1)
        {
            neighbor = neighbors[3];
        }

        return neighbor?.GetBlock(localX, worldY, localZ) ?? (ushort)0;
    }

    private static void GetAxes(int faceDirection, out int depthAxis, out int uAxis, out int vAxis)
    {
        (depthAxis, uAxis, vAxis) = faceDirection switch
        {
            0 => (AxisX, AxisZ, AxisY), // +X: d=X, u=Z, v=Y
            1 => (AxisX, AxisZ, AxisY), // -X
            2 => (AxisY, AxisX, AxisZ), // +Y: d=Y, u=X, v=Z
            3 => (AxisY, AxisX, AxisZ), // -Y
            4 => (AxisZ, AxisX, AxisY), // +Z: d=Z, u=X, v=Y
            5 => (AxisZ, AxisX, AxisY), // -Z
            _ => throw new ArgumentOutOfRangeException(nameof(faceDirection)),
        };
    }

    private static (int NormalX, int NormalY, int NormalZ) GetNormal(int faceDirection) => faceDirection switch
    {
        0 => (1, 0, 0),
        1 => (-1, 0, 0),
        2 => (0, 1, 0),
        3 => (0, -1, 0),
        4 => (0, 0, 1),
        5 => (0, 0, -1),
        _ => throw new ArgumentOutOfRangeException(nameof(faceDirection)),
    };

    /// <summary>
    /// Collects mesh vertex data into lists, then converts to MeshData.
    /// Avoids passing 6 lists through the entire call chain.
    /// </summary>
    private sealed class MeshAccumulator
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
