using MineRPG.World.Chunks;

namespace MineRPG.World.Meshing;

/// <summary>
/// Output of the chunk mesher: per-sub-chunk mesh data arrays.
/// Each sub-chunk (16x16x16 vertical section) has separate opaque
/// and liquid mesh data rendered with distinct materials.
/// </summary>
public sealed class ChunkMeshResult
{
    /// <summary>Empty mesh result with no geometry in any sub-chunk.</summary>
    public static readonly ChunkMeshResult Empty = CreateEmpty();

    /// <summary>
    /// Per-sub-chunk mesh data. Array length is <see cref="SubChunkConstants.SubChunkCount"/>.
    /// Empty sub-chunks have <see cref="SubChunkMesh.IsEmpty"/> = true.
    /// </summary>
    public SubChunkMesh[] SubChunks { get; }

    /// <summary>Whether all sub-chunks are empty (no geometry at all).</summary>
    public bool IsEmpty { get; }

    /// <summary>
    /// Creates a chunk mesh result with per-sub-chunk mesh data.
    /// </summary>
    /// <param name="subChunks">Mesh data per sub-chunk.</param>
    public ChunkMeshResult(SubChunkMesh[] subChunks)
    {
        SubChunks = subChunks;

        bool allEmpty = true;

        for (int i = 0; i < subChunks.Length; i++)
        {
            if (!subChunks[i].IsEmpty)
            {
                allEmpty = false;
                break;
            }
        }

        IsEmpty = allEmpty;
    }

    private static ChunkMeshResult CreateEmpty()
    {
        SubChunkMesh[] empty = new SubChunkMesh[SubChunkConstants.SubChunkCount];

        for (int i = 0; i < empty.Length; i++)
        {
            empty[i] = SubChunkMesh.Empty;
        }

        return new ChunkMeshResult(empty);
    }
}
