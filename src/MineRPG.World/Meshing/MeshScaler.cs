using System;

namespace MineRPG.World.Meshing;

/// <summary>
/// Scales vertex positions in a <see cref="ChunkMeshResult"/> by a given factor.
/// Used to restore correct world-space dimensions after meshing LOD-downsampled data.
///
/// Thread-safe: all state is local to each method call.
/// </summary>
public static class MeshScaler
{
    /// <summary>
    /// Scales all vertex positions in the result by the given factor.
    /// </summary>
    /// <param name="result">The mesh result to scale.</param>
    /// <param name="factor">The scale factor (2 for LOD 1, 4 for LOD 2).</param>
    /// <returns>A new result with scaled vertex positions.</returns>
    public static ChunkMeshResult ScaleResult(ChunkMeshResult result, int factor)
    {
        SubChunkMesh[] scaled = new SubChunkMesh[result.SubChunks.Length];

        for (int i = 0; i < result.SubChunks.Length; i++)
        {
            scaled[i] = new SubChunkMesh(
                ScaleMeshData(result.SubChunks[i].Opaque, factor),
                ScaleMeshData(result.SubChunks[i].Liquid, factor));
        }

        return new ChunkMeshResult(scaled);
    }

    private static MeshData ScaleMeshData(MeshData data, int factor)
    {
        if (data.IsEmpty)
        {
            return data;
        }

        float[] scaledVertices = new float[data.Vertices.Length];
        Array.Copy(data.Vertices, scaledVertices, data.Vertices.Length);

        for (int i = 0; i < scaledVertices.Length; i++)
        {
            scaledVertices[i] *= factor;
        }

        return new MeshData(scaledVertices, data.Normals, data.Uvs, data.Uv2s, data.Colors, data.Indices);
    }
}
