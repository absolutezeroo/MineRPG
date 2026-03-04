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
    public static ArrayMesh? Build(ChunkMeshResult result)
    {
        if (result.IsEmpty)
            return null;

        var mesh = new ArrayMesh();

        if (!result.Opaque.IsEmpty)
            AddSurface(mesh, result.Opaque);

        if (!result.Liquid.IsEmpty)
            AddSurface(mesh, result.Liquid);

        return mesh;
    }

    public static ConcavePolygonShape3D? BuildCollision(MeshData meshData)
    {
        if (meshData.IsEmpty)
            return null;

        var faceCount = meshData.IndexCount / 3;
        var faceVerts = new Vector3[faceCount * 3];

        for (var i = 0; i < faceCount; i++)
        {
            for (var v = 0; v < 3; v++)
            {
                var idx = meshData.Indices[i * 3 + v];
                faceVerts[i * 3 + v] = new Vector3(
                    meshData.Vertices[idx * 3],
                    meshData.Vertices[idx * 3 + 1],
                    meshData.Vertices[idx * 3 + 2]);
            }
        }

        var shape = new ConcavePolygonShape3D();
        shape.SetFaces(faceVerts);
        return shape;
    }

    private static void AddSurface(ArrayMesh mesh, MeshData meshData)
    {
        var arrays = new global::Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);

        var vertices = new Vector3[meshData.VertexCount];
        for (var i = 0; i < meshData.VertexCount; i++)
        {
            vertices[i] = new Vector3(
                meshData.Vertices[i * 3],
                meshData.Vertices[i * 3 + 1],
                meshData.Vertices[i * 3 + 2]);
        }

        arrays[(int)Mesh.ArrayType.Vertex] = vertices;

        var normals = new Vector3[meshData.VertexCount];
        for (var i = 0; i < meshData.VertexCount; i++)
        {
            normals[i] = new Vector3(
                meshData.Normals[i * 3],
                meshData.Normals[i * 3 + 1],
                meshData.Normals[i * 3 + 2]);
        }

        arrays[(int)Mesh.ArrayType.Normal] = normals;

        var uvs = new Vector2[meshData.VertexCount];
        for (var i = 0; i < meshData.VertexCount; i++)
        {
            uvs[i] = new Vector2(
                meshData.Uvs[i * 2],
                meshData.Uvs[i * 2 + 1]);
        }

        arrays[(int)Mesh.ArrayType.TexUV] = uvs;

        var uv2s = new Vector2[meshData.VertexCount];
        for (var i = 0; i < meshData.VertexCount; i++)
        {
            uv2s[i] = new Vector2(
                meshData.Uv2s[i * 2],
                meshData.Uv2s[i * 2 + 1]);
        }

        arrays[(int)Mesh.ArrayType.TexUV2] = uv2s;

        var colors = new Color[meshData.VertexCount];
        for (var i = 0; i < meshData.VertexCount; i++)
        {
            colors[i] = new Color(
                meshData.Colors[i * 4],
                meshData.Colors[i * 4 + 1],
                meshData.Colors[i * 4 + 2],
                meshData.Colors[i * 4 + 3]);
        }

        arrays[(int)Mesh.ArrayType.Color] = colors;
        arrays[(int)Mesh.ArrayType.Index] = meshData.Indices;

        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
    }
}
