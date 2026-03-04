namespace MineRPG.World.Meshing;

/// <summary>
/// Raw mesh output from the greedy mesher.
/// Vertices: float[VertexCount * 3] (x,y,z interleaved).
/// Normals:  float[VertexCount * 3].
/// Uvs:      float[VertexCount * 2] -- tiling coords (0..width, 0..height).
/// Uv2s:     float[VertexCount * 2] -- atlas tile origin (u0, v0).
/// Colors:   float[VertexCount * 4] (r,g,b,a tint).
/// Indices:  int[] triangle list.
/// </summary>
public sealed class MeshData
{
    private const int ComponentsPerVertex = 3;

    /// <summary>Empty mesh data with no geometry.</summary>
    public static readonly MeshData Empty = new();

    /// <summary>Interleaved vertex positions (x,y,z).</summary>
    public float[] Vertices { get; }

    /// <summary>Interleaved vertex normals (x,y,z).</summary>
    public float[] Normals { get; }

    /// <summary>Tiling UV coordinates (u,v per vertex).</summary>
    public float[] Uvs { get; }

    /// <summary>Atlas tile origin UV coordinates (u0,v0 per vertex).</summary>
    public float[] Uv2s { get; }

    /// <summary>Vertex colors with AO in alpha (r,g,b,a per vertex).</summary>
    public float[] Colors { get; }

    /// <summary>Triangle index list.</summary>
    public int[] Indices { get; }

    /// <summary>Number of vertices in this mesh.</summary>
    public int VertexCount { get; }

    /// <summary>Number of indices in this mesh.</summary>
    public int IndexCount { get; }

    /// <summary>Whether this mesh contains no geometry.</summary>
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

    /// <summary>
    /// Creates mesh data from raw arrays.
    /// </summary>
    /// <param name="vertices">Interleaved vertex positions.</param>
    /// <param name="normals">Interleaved vertex normals.</param>
    /// <param name="uvs">Tiling UV coordinates.</param>
    /// <param name="uv2s">Atlas tile origin UV coordinates.</param>
    /// <param name="colors">Vertex colors with AO.</param>
    /// <param name="indices">Triangle index list.</param>
    public MeshData(float[] vertices, float[] normals, float[] uvs, float[] uv2s, float[] colors, int[] indices)
    {
        Vertices = vertices;
        Normals = normals;
        Uvs = uvs;
        Uv2s = uv2s;
        Colors = colors;
        Indices = indices;
        VertexCount = vertices.Length / ComponentsPerVertex;
        IndexCount = indices.Length;
    }
}
