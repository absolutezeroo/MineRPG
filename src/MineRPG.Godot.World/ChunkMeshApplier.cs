using Godot;

using MineRPG.World.Meshing;

namespace MineRPG.Godot.World;

/// <summary>
/// Converts pure MeshData (float arrays) into a Godot ArrayMesh.
/// Supports multi-surface meshes: surface 0 = opaque terrain,
/// surface 1 = translucent liquid (if present).
/// Called on the main thread by ChunkNode.
/// </summary>
public static class ChunkMeshApplier
{
    private const int VertexStride = 3;
    private const int UvStride = 2;
    private const int ColorStride = 4;
    private const int TriangleVertexCount = 3;

    /// <summary>
    /// Builds a Godot ArrayMesh from a chunk mesh result containing opaque and liquid surfaces.
    /// </summary>
    /// <param name="result">The chunk mesh result with opaque and liquid mesh data.</param>
    /// <returns>The assembled ArrayMesh, or null if the result is empty.</returns>
    public static ArrayMesh? Build(ChunkMeshResult result)
    {
        if (result.IsEmpty)
        {
            return null;
        }

        ArrayMesh mesh = new();

        if (!result.Opaque.IsEmpty)
        {
            AddSurface(mesh, result.Opaque);
        }

        if (!result.Liquid.IsEmpty)
        {
            AddSurface(mesh, result.Liquid);
        }

        return mesh;
    }

    /// <summary>
    /// Builds a concave polygon collision shape from opaque mesh data.
    /// </summary>
    /// <param name="meshData">The opaque mesh data to build collision from.</param>
    /// <returns>The collision shape, or null if the mesh data is empty.</returns>
    public static ConcavePolygonShape3D? BuildCollision(MeshData meshData)
    {
        if (meshData.IsEmpty)
        {
            return null;
        }

        int faceCount = meshData.IndexCount / TriangleVertexCount;
        Vector3[] faceVertices = new Vector3[faceCount * TriangleVertexCount];

        for (int i = 0; i < faceCount; i++)
        {
            for (int v = 0; v < TriangleVertexCount; v++)
            {
                int index = meshData.Indices[i * TriangleVertexCount + v];
                faceVertices[i * TriangleVertexCount + v] = new Vector3(
                    meshData.Vertices[index * VertexStride],
                    meshData.Vertices[index * VertexStride + 1],
                    meshData.Vertices[index * VertexStride + 2]);
            }
        }

        ConcavePolygonShape3D shape = new();
        shape.SetFaces(faceVertices);
        return shape;
    }

    private static void AddSurface(ArrayMesh mesh, MeshData meshData)
    {
        global::Godot.Collections.Array arrays = new();
        arrays.Resize((int)Mesh.ArrayType.Max);

        Vector3[] vertices = new Vector3[meshData.VertexCount];

        for (int i = 0; i < meshData.VertexCount; i++)
        {
            vertices[i] = new Vector3(
                meshData.Vertices[i * VertexStride],
                meshData.Vertices[i * VertexStride + 1],
                meshData.Vertices[i * VertexStride + 2]);
        }

        arrays[(int)Mesh.ArrayType.Vertex] = vertices;

        Vector3[] normals = new Vector3[meshData.VertexCount];

        for (int i = 0; i < meshData.VertexCount; i++)
        {
            normals[i] = new Vector3(
                meshData.Normals[i * VertexStride],
                meshData.Normals[i * VertexStride + 1],
                meshData.Normals[i * VertexStride + 2]);
        }

        arrays[(int)Mesh.ArrayType.Normal] = normals;

        Vector2[] uvs = new Vector2[meshData.VertexCount];

        for (int i = 0; i < meshData.VertexCount; i++)
        {
            uvs[i] = new Vector2(
                meshData.Uvs[i * UvStride],
                meshData.Uvs[i * UvStride + 1]);
        }

        arrays[(int)Mesh.ArrayType.TexUV] = uvs;

        Vector2[] secondaryUvs = new Vector2[meshData.VertexCount];

        for (int i = 0; i < meshData.VertexCount; i++)
        {
            secondaryUvs[i] = new Vector2(
                meshData.Uv2s[i * UvStride],
                meshData.Uv2s[i * UvStride + 1]);
        }

        arrays[(int)Mesh.ArrayType.TexUV2] = secondaryUvs;

        Color[] colors = new Color[meshData.VertexCount];

        for (int i = 0; i < meshData.VertexCount; i++)
        {
            colors[i] = new Color(
                meshData.Colors[i * ColorStride],
                meshData.Colors[i * ColorStride + 1],
                meshData.Colors[i * ColorStride + 2],
                meshData.Colors[i * ColorStride + 3]);
        }

        arrays[(int)Mesh.ArrayType.Color] = colors;
        arrays[(int)Mesh.ArrayType.Index] = meshData.Indices;

        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
    }
}
