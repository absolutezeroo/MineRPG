using System.Collections.Generic;

using Godot;

using MineRPG.Core.Math;
using MineRPG.Godot.World.Chunks;
using MineRPG.World.Chunks;
using MineRPG.World.Meshing;

namespace MineRPG.Godot.World.Rendering;

/// <summary>
/// Rebuilds the combined mesh layers for a <see cref="ChunkRegion"/> when any of its
/// member chunks receives a new mesh result. Reads <see cref="ChunkEntry.LastMeshResult"/>
/// to reconstruct the batch without requiring all chunks to remesh simultaneously.
/// </summary>
internal sealed class RegionLayerBuilder
{
    private readonly IChunkManager _chunkManager;
    private readonly List<(ChunkCoord Coord, SubChunkMesh[] SubChunks)> _collectBuffer = new();

    /// <summary>
    /// Creates a region layer builder.
    /// </summary>
    /// <param name="chunkManager">Chunk manager for looking up chunk entries.</param>
    public RegionLayerBuilder(IChunkManager chunkManager)
    {
        _chunkManager = chunkManager;
    }

    /// <summary>
    /// Rebuilds all sub-chunk layers for the given region using the last mesh result
    /// of each member chunk. Only includes chunks at LOD 0.
    /// </summary>
    /// <param name="region">The region to rebuild.</param>
    public void RebuildRegion(ChunkRegion region)
    {
        _collectBuffer.Clear();
        CollectChunkMeshes(region);

        Material? sharedMaterial = ChunkNode.SharedMaterial;

        for (int layer = 0; layer < SubChunkConstants.SubChunkCount; layer++)
        {
            if (_collectBuffer.Count == 0)
            {
                region.ApplyLayerMesh(layer, null, null);
                continue;
            }

            MeshData combined = RegionMeshBatcher.BatchSubChunkOpaque(
                _collectBuffer, region.RegionCoord, layer);

            if (combined.IsEmpty)
            {
                region.ApplyLayerMesh(layer, null, null);
                continue;
            }

            ArrayMesh? arrayMesh = ChunkMeshApplier.BuildSingle(combined);
            region.ApplyLayerMesh(layer, arrayMesh, sharedMaterial);
        }

        _collectBuffer.Clear();
    }

    /// <summary>
    /// Collects mesh data from all LOD 0 chunks belonging to the region.
    /// </summary>
    private void CollectChunkMeshes(ChunkRegion region)
    {
        foreach (ChunkEntry entry in _chunkManager.GetAll())
        {
            if (!region.ContainsChunk(entry.Coord))
            {
                continue;
            }

            // Only batch LOD 0 chunks for v1
            if (entry.CurrentLod != LodLevel.Lod0)
            {
                continue;
            }

            if (entry.LastMeshResult is null || entry.LastMeshResult.IsEmpty)
            {
                continue;
            }

            _collectBuffer.Add((entry.Coord, entry.LastMeshResult.SubChunks));
        }
    }
}
