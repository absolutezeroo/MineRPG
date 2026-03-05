using System;

using MineRPG.World.Blocks;
using MineRPG.World.Chunks;

namespace MineRPG.World.Meshing;

/// <summary>
/// Emits vertices, normals, UVs, indices, and colors for a single merged quad.
/// Handles winding order (CW front-face for Vulkan/Godot), tiling UVs,
/// atlas UV2s, per-vertex AO, and AO-based quad-flip to reduce artifacts.
///
/// All methods are static for hot-path performance.
/// </summary>
internal static class QuadEmitter
{
    private const int VerticesPerQuad = 4;
    private const int UvComponentsPerFace = 4;
    private const int ComponentsPerVertex = 3;
    private const int AxisY = 1;

    /// <summary>
    /// Emits a single quad into the target accumulator.
    /// </summary>
    /// <param name="depthAxis">The face depth axis.</param>
    /// <param name="uAxis">The face U axis.</param>
    /// <param name="vAxis">The face V axis.</param>
    /// <param name="slice">The slice index along the depth axis.</param>
    /// <param name="offset">0 or 1 offset for positive/negative face direction.</param>
    /// <param name="ui">Starting U coordinate of the quad.</param>
    /// <param name="vi">Starting V coordinate of the quad.</param>
    /// <param name="width">Width of the merged quad in U direction.</param>
    /// <param name="height">Height of the merged quad in V direction.</param>
    /// <param name="normalX">Face normal X component.</param>
    /// <param name="normalY">Face normal Y component.</param>
    /// <param name="normalZ">Face normal Z component.</param>
    /// <param name="definition">Block definition for UV and tint data.</param>
    /// <param name="faceDirection">Face direction index (0-5).</param>
    /// <param name="chunk">The chunk being meshed.</param>
    /// <param name="neighbors">Cardinal neighbor chunks.</param>
    /// <param name="blockRegistry">Block registry for AO solidity lookups.</param>
    /// <param name="target">The mesh accumulator to emit into.</param>
    public static void Emit(
        int depthAxis, int uAxis, int vAxis,
        int slice, int offset,
        int ui, int vi, int width, int height,
        int normalX, int normalY, int normalZ,
        BlockDefinition definition, int faceDirection,
        ChunkData chunk, ChunkData?[] neighbors,
        BlockRegistry blockRegistry,
        ChunkMeshBuilder.MeshAccumulator target)
    {
        Span<int> cornerU = stackalloc int[VerticesPerQuad];
        Span<int> cornerV = stackalloc int[VerticesPerQuad];

        bool isFlipped = (normalX + normalY - normalZ) < 0;

        if (isFlipped)
        {
            cornerU[0] = 0; cornerV[0] = 0;
            cornerU[1] = 0; cornerV[1] = height;
            cornerU[2] = width; cornerV[2] = height;
            cornerU[3] = width; cornerV[3] = 0;
        }
        else
        {
            cornerU[0] = 0; cornerV[0] = 0;
            cornerU[1] = width; cornerV[1] = 0;
            cornerU[2] = width; cornerV[2] = height;
            cornerU[3] = 0; cornerV[3] = height;
        }

        float tileU0 = definition.FaceUvs[faceDirection * UvComponentsPerFace + 0];
        float tileV0 = definition.FaceUvs[faceDirection * UvComponentsPerFace + 1];

        Span<float> tilingU = stackalloc float[VerticesPerQuad];
        Span<float> tilingV = stackalloc float[VerticesPerQuad];

        if (isFlipped)
        {
            tilingU[0] = 0; tilingV[0] = 0;
            tilingU[1] = 0; tilingV[1] = height;
            tilingU[2] = width; tilingV[2] = height;
            tilingU[3] = width; tilingV[3] = 0;
        }
        else
        {
            tilingU[0] = 0; tilingV[0] = 0;
            tilingU[1] = width; tilingV[1] = 0;
            tilingU[2] = width; tilingV[2] = height;
            tilingU[3] = 0; tilingV[3] = height;
        }

        if (vAxis == AxisY)
        {
            for (int i = 0; i < VerticesPerQuad; i++)
            {
                tilingV[i] = height - tilingV[i];
            }
        }

        int airDepth = slice + ((normalX + normalY + normalZ) > 0 ? 1 : -1);
        Span<float> ambientOcclusion = stackalloc float[VerticesPerQuad];
        int baseVertex = target.Vertices.Count / ComponentsPerVertex;

        for (int i = 0; i < VerticesPerQuad; i++)
        {
            int du = cornerU[i];
            int dv = cornerV[i];

            MeshCoordHelper.ResolveCoord(depthAxis, uAxis, vAxis, slice + offset, ui + du, vi + dv,
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

            ambientOcclusion[i] = AmbientOcclusionCalculator.Compute(chunk, neighbors, blockRegistry,
                depthAxis, uAxis, vAxis, airDepth, ui + du, vi + dv, du, dv);

            target.Colors.Add(definition.TintR);
            target.Colors.Add(definition.TintG);
            target.Colors.Add(definition.TintB);
            target.Colors.Add(ambientOcclusion[i]);
        }

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
}
