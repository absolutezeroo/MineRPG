using Godot;

using MineRPG.World.Chunks;
using MineRPG.World.Meshing;

namespace MineRPG.Godot.World.Chunks;

/// <summary>
/// Converts pure MeshData (float arrays) into Godot ArrayMesh instances.
/// Supports per-sub-chunk mesh building and combined collision shape generation.
/// Handles both standard MeshData and PackedMeshData (unpacking as needed).
/// Called on the main thread by ChunkNode.
/// </summary>
public static class ChunkMeshApplier
{
    private const int VertexStride = 3;
    private const int UvStride = 2;
    private const int ColorStride = 4;
    private const int TriangleVertexCount = 3;

    /// <summary>
    /// Builds a Godot ArrayMesh from a sub-chunk mesh containing opaque and liquid surfaces.
    /// Unpacks packed data if present.
    /// </summary>
    /// <param name="subChunkMesh">The sub-chunk mesh with opaque and liquid mesh data.</param>
    /// <returns>The assembled ArrayMesh, or null if the sub-chunk is empty.</returns>
    public static ArrayMesh? Build(SubChunkMesh subChunkMesh)
    {
        if (subChunkMesh.IsEmpty)
        {
            return null;
        }

        ArrayMesh mesh = new();

        MeshData opaque = ResolveOpaque(subChunkMesh);

        if (!opaque.IsEmpty)
        {
            AddSurface(mesh, opaque);
        }

        MeshData liquid = ResolveLiquid(subChunkMesh);

        if (!liquid.IsEmpty)
        {
            AddSurface(mesh, liquid);
        }

        return mesh;
    }

    /// <summary>
    /// Builds a Godot ArrayMesh from a single MeshData (used by region batching).
    /// </summary>
    /// <param name="meshData">The mesh data to convert.</param>
    /// <returns>The assembled ArrayMesh, or null if the mesh data is empty.</returns>
    public static ArrayMesh? BuildSingle(MeshData meshData)
    {
        if (meshData.IsEmpty)
        {
            return null;
        }

        ArrayMesh mesh = new();
        AddSurface(mesh, meshData);
        return mesh;
    }

    /// <summary>
    /// Builds a combined concave polygon collision shape from all sub-chunk opaque meshes.
    /// Supports both standard and packed mesh data. Only opaque surfaces generate collision.
    /// </summary>
    /// <param name="result">The chunk mesh result containing per-sub-chunk data.</param>
    /// <returns>The combined collision shape, or null if no opaque geometry exists.</returns>
    public static ConcavePolygonShape3D? BuildCombinedCollision(ChunkMeshResult result)
    {
        int totalFaceVertices = 0;

        for (int i = 0; i < result.SubChunks.Length; i++)
        {
            MeshData opaque = ResolveOpaque(result.SubChunks[i]);

            if (!opaque.IsEmpty)
            {
                totalFaceVertices += opaque.IndexCount;
            }
        }

        if (totalFaceVertices == 0)
        {
            return null;
        }

        Vector3[] faceVertices = new Vector3[totalFaceVertices];
        int writeOffset = 0;

        for (int subChunkIndex = 0; subChunkIndex < result.SubChunks.Length; subChunkIndex++)
        {
            MeshData opaque = ResolveOpaque(result.SubChunks[subChunkIndex]);

            if (opaque.IsEmpty)
            {
                continue;
            }

            int faceCount = opaque.IndexCount / TriangleVertexCount;

            for (int face = 0; face < faceCount; face++)
            {
                for (int vertex = 0; vertex < TriangleVertexCount; vertex++)
                {
                    int index = opaque.Indices[face * TriangleVertexCount + vertex];
                    faceVertices[writeOffset++] = new Vector3(
                        opaque.Vertices[index * VertexStride],
                        opaque.Vertices[index * VertexStride + 1],
                        opaque.Vertices[index * VertexStride + 2]);
                }
            }
        }

        ConcavePolygonShape3D shape = new();
        shape.SetFaces(faceVertices);
        return shape;
    }

    /// <summary>
    /// Resolves opaque mesh data, unpacking from packed format if necessary.
    /// </summary>
    private static MeshData ResolveOpaque(SubChunkMesh subChunk)
    {
        if (!subChunk.Opaque.IsEmpty)
        {
            return subChunk.Opaque;
        }

        if (subChunk.PackedOpaque is not null && !subChunk.PackedOpaque.IsEmpty)
        {
            return VertexPacker.Unpack(subChunk.PackedOpaque.Vertices, subChunk.PackedOpaque.Indices);
        }

        return MeshData.Empty;
    }

    /// <summary>
    /// Resolves liquid mesh data, unpacking from packed format if necessary.
    /// </summary>
    private static MeshData ResolveLiquid(SubChunkMesh subChunk)
    {
        if (!subChunk.Liquid.IsEmpty)
        {
            return subChunk.Liquid;
        }

        if (subChunk.PackedLiquid is not null && !subChunk.PackedLiquid.IsEmpty)
        {
            return VertexPacker.Unpack(subChunk.PackedLiquid.Vertices, subChunk.PackedLiquid.Indices);
        }

        return MeshData.Empty;
    }

    private static void AddSurface(ArrayMesh mesh, MeshData meshData)
    {
        global::Godot.Collections.Array arrays = new();
        arrays.Resize((int)Mesh.ArrayType.Max);

        arrays[(int)Mesh.ArrayType.Vertex] = ConvertVertices(meshData);
        arrays[(int)Mesh.ArrayType.Normal] = ConvertNormals(meshData);
        arrays[(int)Mesh.ArrayType.TexUV] = ConvertUvs(meshData.Uvs, meshData.VertexCount);
        arrays[(int)Mesh.ArrayType.TexUV2] = ConvertUvs(meshData.Uv2s, meshData.VertexCount);
        arrays[(int)Mesh.ArrayType.Color] = ConvertColors(meshData);
        arrays[(int)Mesh.ArrayType.Index] = meshData.Indices;

        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
    }

    private static Vector3[] ConvertVertices(MeshData meshData)
    {
        Vector3[] vertices = new Vector3[meshData.VertexCount];

        for (int i = 0; i < meshData.VertexCount; i++)
        {
            vertices[i] = new Vector3(
                meshData.Vertices[i * VertexStride],
                meshData.Vertices[i * VertexStride + 1],
                meshData.Vertices[i * VertexStride + 2]);
        }

        return vertices;
    }

    private static Vector3[] ConvertNormals(MeshData meshData)
    {
        Vector3[] normals = new Vector3[meshData.VertexCount];

        for (int i = 0; i < meshData.VertexCount; i++)
        {
            normals[i] = new Vector3(
                meshData.Normals[i * VertexStride],
                meshData.Normals[i * VertexStride + 1],
                meshData.Normals[i * VertexStride + 2]);
        }

        return normals;
    }

    private static Vector2[] ConvertUvs(float[] sourceUvs, int vertexCount)
    {
        Vector2[] uvs = new Vector2[vertexCount];

        for (int i = 0; i < vertexCount; i++)
        {
            uvs[i] = new Vector2(
                sourceUvs[i * UvStride],
                sourceUvs[i * UvStride + 1]);
        }

        return uvs;
    }

    private static Color[] ConvertColors(MeshData meshData)
    {
        Color[] colors = new Color[meshData.VertexCount];

        for (int i = 0; i < meshData.VertexCount; i++)
        {
            colors[i] = new Color(
                meshData.Colors[i * ColorStride],
                meshData.Colors[i * ColorStride + 1],
                meshData.Colors[i * ColorStride + 2],
                meshData.Colors[i * ColorStride + 3]);
        }

        return colors;
    }
}
