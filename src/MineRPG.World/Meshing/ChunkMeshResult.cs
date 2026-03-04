namespace MineRPG.World.Meshing;

/// <summary>
/// Output of the chunk mesher: separate mesh data for opaque terrain
/// and translucent liquid faces. Each is rendered as a distinct
/// surface with its own material.
/// </summary>
public sealed class ChunkMeshResult
{
    public static readonly ChunkMeshResult Empty = new(MeshData.Empty, MeshData.Empty);

    public MeshData Opaque { get; }
    public MeshData Liquid { get; }

    public bool IsEmpty => Opaque.IsEmpty && Liquid.IsEmpty;

    public ChunkMeshResult(MeshData opaque, MeshData liquid)
    {
        Opaque = opaque;
        Liquid = liquid;
    }
}
