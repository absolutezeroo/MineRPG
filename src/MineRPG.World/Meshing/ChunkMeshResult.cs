namespace MineRPG.World.Meshing;

/// <summary>
/// Output of the chunk mesher: separate mesh data for opaque terrain
/// and translucent liquid faces. Each is rendered as a distinct
/// surface with its own material.
/// </summary>
public sealed class ChunkMeshResult
{
    /// <summary>Empty mesh result with no geometry.</summary>
    public static readonly ChunkMeshResult Empty = new(MeshData.Empty, MeshData.Empty);

    /// <summary>Mesh data for opaque terrain faces.</summary>
    public MeshData Opaque { get; }

    /// <summary>Mesh data for translucent liquid faces.</summary>
    public MeshData Liquid { get; }

    /// <summary>Whether both opaque and liquid meshes are empty.</summary>
    public bool IsEmpty => Opaque.IsEmpty && Liquid.IsEmpty;

    /// <summary>
    /// Creates a chunk mesh result with separate opaque and liquid mesh data.
    /// </summary>
    /// <param name="opaque">Opaque terrain mesh data.</param>
    /// <param name="liquid">Translucent liquid mesh data.</param>
    public ChunkMeshResult(MeshData opaque, MeshData liquid)
    {
        Opaque = opaque;
        Liquid = liquid;
    }
}
