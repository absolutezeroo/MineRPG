namespace MineRPG.World.Meshing;

/// <summary>
/// Mesh data for a single 16x16x16 vertical sub-section of a chunk.
/// Each sub-chunk has separate opaque and liquid mesh data to support
/// different materials (opaque terrain vs translucent water).
/// </summary>
public readonly struct SubChunkMesh
{
    /// <summary>Empty sub-chunk mesh with no geometry.</summary>
    public static readonly SubChunkMesh Empty = new(MeshData.Empty, MeshData.Empty);

    /// <summary>Mesh data for opaque terrain faces in this sub-chunk.</summary>
    public MeshData Opaque { get; }

    /// <summary>Mesh data for translucent liquid faces in this sub-chunk.</summary>
    public MeshData Liquid { get; }

    /// <summary>Whether both opaque and liquid meshes are empty.</summary>
    public bool IsEmpty => Opaque.IsEmpty && Liquid.IsEmpty;

    /// <summary>
    /// Creates a sub-chunk mesh with separate opaque and liquid mesh data.
    /// </summary>
    /// <param name="opaque">Opaque terrain mesh data.</param>
    /// <param name="liquid">Translucent liquid mesh data.</param>
    public SubChunkMesh(MeshData opaque, MeshData liquid)
    {
        Opaque = opaque;
        Liquid = liquid;
    }
}
