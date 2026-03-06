using System;
using System.Collections.Generic;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.World.Meshing;

/// <summary>
/// Combines mesh data from multiple chunks in a region (4x4 chunk grid)
/// into a single merged mesh for reduced draw calls. Each chunk's vertex
/// positions are offset to their correct world-space location relative
/// to the region origin.
///
/// Only batches chunks at the same LOD level. Chunks with different LODs
/// are rendered separately.
///
/// Thread-safe: all state is local to each Batch() call.
/// </summary>
public static class RegionMeshBatcher
{
    /// <summary>Region size in chunks per axis (4x4 grid = 16 chunks per region).</summary>
    public const int RegionSize = 4;

    /// <summary>
    /// Computes the region coordinate for a given chunk coordinate.
    /// Uses floor division to handle negative coordinates correctly.
    /// </summary>
    /// <param name="chunkCoord">The chunk coordinate.</param>
    /// <returns>The region coordinate containing this chunk.</returns>
    public static ChunkCoord GetRegionCoord(ChunkCoord chunkCoord)
    {
        int regionX = chunkCoord.X >= 0
            ? chunkCoord.X / RegionSize
            : (chunkCoord.X - RegionSize + 1) / RegionSize;
        int regionZ = chunkCoord.Z >= 0
            ? chunkCoord.Z / RegionSize
            : (chunkCoord.Z - RegionSize + 1) / RegionSize;
        return new ChunkCoord(regionX, regionZ);
    }

    /// <summary>
    /// Combines sub-chunk mesh data from multiple chunks into a single MeshData.
    /// Offsets each chunk's vertices to their world-space position relative to
    /// the region origin.
    /// </summary>
    /// <param name="chunkMeshes">
    /// List of (ChunkCoord, SubChunkMesh[]) pairs to batch.
    /// All chunks must be in the same region and at the same LOD level.
    /// </param>
    /// <param name="regionCoord">The region coordinate (for computing the origin).</param>
    /// <param name="subChunkIndex">The sub-chunk index to batch across all chunks.</param>
    /// <returns>Combined opaque mesh data, or MeshData.Empty if all inputs are empty.</returns>
    public static MeshData BatchSubChunkOpaque(
        IReadOnlyList<(ChunkCoord Coord, SubChunkMesh[] SubChunks)> chunkMeshes,
        ChunkCoord regionCoord,
        int subChunkIndex)
    {
        float regionOriginX = regionCoord.X * RegionSize * ChunkData.SizeX;
        float regionOriginZ = regionCoord.Z * RegionSize * ChunkData.SizeZ;

        int totalVertices = 0;
        int totalIndices = 0;

        for (int i = 0; i < chunkMeshes.Count; i++)
        {
            if (subChunkIndex < chunkMeshes[i].SubChunks.Length)
            {
                MeshData opaque = chunkMeshes[i].SubChunks[subChunkIndex].Opaque;
                totalVertices += opaque.VertexCount;
                totalIndices += opaque.IndexCount;
            }
        }

        if (totalVertices == 0)
        {
            return MeshData.Empty;
        }

        float[] vertices = new float[totalVertices * 3];
        float[] normals = new float[totalVertices * 3];
        float[] uvs = new float[totalVertices * 2];
        float[] uv2s = new float[totalVertices * 2];
        float[] colors = new float[totalVertices * 4];
        int[] indices = new int[totalIndices];

        int vertexOffset = 0;
        int indexOffset = 0;

        for (int i = 0; i < chunkMeshes.Count; i++)
        {
            if (subChunkIndex >= chunkMeshes[i].SubChunks.Length)
            {
                continue;
            }

            MeshData opaque = chunkMeshes[i].SubChunks[subChunkIndex].Opaque;

            if (opaque.IsEmpty)
            {
                continue;
            }

            float offsetX = chunkMeshes[i].Coord.X * ChunkData.SizeX - regionOriginX;
            float offsetZ = chunkMeshes[i].Coord.Z * ChunkData.SizeZ - regionOriginZ;

            // Copy and offset vertices
            for (int v = 0; v < opaque.VertexCount; v++)
            {
                int srcBase = v * 3;
                int dstBase = (vertexOffset + v) * 3;
                vertices[dstBase] = opaque.Vertices[srcBase] + offsetX;
                vertices[dstBase + 1] = opaque.Vertices[srcBase + 1];
                vertices[dstBase + 2] = opaque.Vertices[srcBase + 2] + offsetZ;
            }

            // Copy normals (no offset needed)
            Array.Copy(opaque.Normals, 0, normals, vertexOffset * 3, opaque.VertexCount * 3);

            // Copy UVs
            Array.Copy(opaque.Uvs, 0, uvs, vertexOffset * 2, opaque.VertexCount * 2);
            Array.Copy(opaque.Uv2s, 0, uv2s, vertexOffset * 2, opaque.VertexCount * 2);

            // Copy colors
            Array.Copy(opaque.Colors, 0, colors, vertexOffset * 4, opaque.VertexCount * 4);

            // Copy and offset indices
            for (int idx = 0; idx < opaque.IndexCount; idx++)
            {
                indices[indexOffset + idx] = opaque.Indices[idx] + vertexOffset;
            }

            vertexOffset += opaque.VertexCount;
            indexOffset += opaque.IndexCount;
        }

        return new MeshData(vertices, normals, uvs, uv2s, colors, indices);
    }
}
