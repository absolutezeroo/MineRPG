namespace MineRPG.World.Meshing;

/// <summary>
/// Compressed mesh data using packed vertices for memory-efficient
/// transport between background workers and the main thread.
/// ~64% smaller than MeshData for the same geometry.
/// </summary>
public sealed class PackedMeshData
{
    /// <summary>Empty packed mesh data.</summary>
    public static readonly PackedMeshData Empty = new([], []);

    /// <summary>Compressed vertex array.</summary>
    public PackedVertex[] Vertices { get; }

    /// <summary>Triangle index list (unchanged from MeshData).</summary>
    public int[] Indices { get; }

    /// <summary>Number of vertices.</summary>
    public int VertexCount => Vertices.Length;

    /// <summary>Whether this mesh contains no geometry.</summary>
    public bool IsEmpty => Vertices.Length == 0;

    /// <summary>
    /// Creates packed mesh data from compressed vertices and indices.
    /// </summary>
    /// <param name="vertices">Compressed vertex array.</param>
    /// <param name="indices">Triangle index list.</param>
    public PackedMeshData(PackedVertex[] vertices, int[] indices)
    {
        Vertices = vertices;
        Indices = indices;
    }
}
