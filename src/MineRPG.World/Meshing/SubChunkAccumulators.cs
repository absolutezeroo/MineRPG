namespace MineRPG.World.Meshing;

/// <summary>
/// Pair of mesh accumulators for one sub-chunk: opaque terrain and liquid.
/// Created per sub-chunk during the meshing build pass.
/// </summary>
internal sealed class SubChunkAccumulators
{
    /// <summary>Accumulator for opaque terrain quads in this sub-chunk.</summary>
    public ChunkMeshBuilder.MeshAccumulator Opaque { get; }

    /// <summary>Accumulator for liquid quads in this sub-chunk.</summary>
    public ChunkMeshBuilder.MeshAccumulator Liquid { get; }

    /// <summary>
    /// Creates a pair of accumulators with the given initial capacities.
    /// </summary>
    /// <param name="opaqueCapacity">Initial vertex capacity for opaque mesh.</param>
    /// <param name="liquidCapacity">Initial vertex capacity for liquid mesh.</param>
    public SubChunkAccumulators(int opaqueCapacity, int liquidCapacity)
    {
        Opaque = new ChunkMeshBuilder.MeshAccumulator(opaqueCapacity);
        Liquid = new ChunkMeshBuilder.MeshAccumulator(liquidCapacity);
    }

    /// <summary>
    /// Converts the accumulated data into a <see cref="SubChunkMesh"/>.
    /// </summary>
    /// <returns>Sub-chunk mesh data for opaque and liquid surfaces.</returns>
    public SubChunkMesh ToSubChunkMesh() => new(Opaque.ToMeshData(), Liquid.ToMeshData());
}
