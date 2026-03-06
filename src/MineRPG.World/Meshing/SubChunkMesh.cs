namespace MineRPG.World.Meshing;

/// <summary>
/// Mesh data for a single 16x16x16 vertical sub-section of a chunk.
/// Each sub-chunk has separate opaque and liquid mesh data to support
/// different materials (opaque terrain vs translucent water).
/// When vertex packing is active, the packed fields contain the compressed
/// data and the original MeshData fields are set to <see cref="MeshData.Empty"/>.
/// </summary>
public readonly struct SubChunkMesh
{
    /// <summary>Empty sub-chunk mesh with no geometry.</summary>
    public static readonly SubChunkMesh Empty = new(MeshData.Empty, MeshData.Empty);

    /// <summary>Mesh data for opaque terrain faces in this sub-chunk.</summary>
    public MeshData Opaque { get; }

    /// <summary>Mesh data for translucent liquid faces in this sub-chunk.</summary>
    public MeshData Liquid { get; }

    /// <summary>Compressed opaque mesh for memory-efficient transport. Null if not packed.</summary>
    public PackedMeshData? PackedOpaque { get; init; }

    /// <summary>Compressed liquid mesh for memory-efficient transport. Null if not packed.</summary>
    public PackedMeshData? PackedLiquid { get; init; }

    /// <summary>Whether this sub-chunk contains no geometry (checking both standard and packed data).</summary>
    public bool IsEmpty => Opaque.IsEmpty && Liquid.IsEmpty
                        && (PackedOpaque is null || PackedOpaque.IsEmpty)
                        && (PackedLiquid is null || PackedLiquid.IsEmpty);

    /// <summary>Whether this sub-chunk has opaque geometry (standard or packed).</summary>
    public bool HasOpaque => !Opaque.IsEmpty || (PackedOpaque is not null && !PackedOpaque.IsEmpty);

    /// <summary>Whether this sub-chunk has liquid geometry (standard or packed).</summary>
    public bool HasLiquid => !Liquid.IsEmpty || (PackedLiquid is not null && !PackedLiquid.IsEmpty);

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
