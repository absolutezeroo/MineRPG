namespace MineRPG.World.Meshing;

/// <summary>
/// Raw mesh output from the greedy mesher.
/// Vertices: float[VertexCount * 3] (x,y,z interleaved).
/// Normals:  float[VertexCount * 3].
/// Uvs:      float[VertexCount * 2] — tiling coords (0..width, 0..height).
/// Uv2s:     float[VertexCount * 2] — atlas tile origin (u0, v0).
/// Colors:   float[VertexCount * 4] (r,g,b,a tint).
/// Indices:  int[] triangle list.
/// </summary>
public sealed class MeshData
{
    public static readonly MeshData Empty = new();

    public float[] Vertices { get; }
    public float[] Normals { get; }
    public float[] Uvs { get; }
    public float[] Uv2s { get; }
    public float[] Colors { get; }
    public int[] Indices { get; }

    public int VertexCount { get; }
    public int IndexCount { get; }
    public bool IsEmpty => VertexCount == 0;

    private MeshData()
    {
        Vertices = [];
        Normals = [];
        Uvs = [];
        Uv2s = [];
        Colors = [];
        Indices = [];
    }

    public MeshData(float[] vertices, float[] normals, float[] uvs, float[] uv2s, float[] colors, int[] indices)
    {
        Vertices = vertices;
        Normals = normals;
        Uvs = uvs;
        Uv2s = uv2s;
        Colors = colors;
        Indices = indices;
        VertexCount = vertices.Length / 3;
        IndexCount = indices.Length;
    }
}
