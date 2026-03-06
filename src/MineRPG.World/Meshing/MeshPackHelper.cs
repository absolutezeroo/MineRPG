namespace MineRPG.World.Meshing;

/// <summary>
/// Packs all sub-chunk meshes in a <see cref="ChunkMeshResult"/> into
/// <see cref="PackedMeshData"/> for memory-efficient transport.
/// Replaces the original <see cref="MeshData"/> with <see cref="MeshData.Empty"/>
/// after packing to free the float arrays.
///
/// Thread-safe: all state is local to each method call.
/// </summary>
public static class MeshPackHelper
{
    /// <summary>
    /// Packs all sub-chunk meshes in the result, replacing the originals
    /// with <see cref="MeshData.Empty"/> to free memory.
    /// </summary>
    /// <param name="result">The mesh result to pack.</param>
    /// <returns>A new result with packed data and empty originals.</returns>
    public static ChunkMeshResult PackResult(ChunkMeshResult result)
    {
        SubChunkMesh[] packed = new SubChunkMesh[result.SubChunks.Length];

        for (int i = 0; i < result.SubChunks.Length; i++)
        {
            SubChunkMesh source = result.SubChunks[i];

            PackedMeshData? packedOpaque = source.Opaque.IsEmpty
                ? null
                : new PackedMeshData(VertexPacker.Pack(source.Opaque), source.Opaque.Indices);

            PackedMeshData? packedLiquid = source.Liquid.IsEmpty
                ? null
                : new PackedMeshData(VertexPacker.Pack(source.Liquid), source.Liquid.Indices);

            packed[i] = new SubChunkMesh(MeshData.Empty, MeshData.Empty)
            {
                PackedOpaque = packedOpaque,
                PackedLiquid = packedLiquid,
            };
        }

        return new ChunkMeshResult(packed);
    }
}
