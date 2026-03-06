using System.Collections.Generic;

using Godot;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Meshing;

namespace MineRPG.Godot.World.Rendering;

/// <summary>
/// A group of up to 4x4 chunks sharing a single MeshInstance3D per sub-chunk layer.
/// Reduces draw calls by batching adjacent chunk meshes into one combined mesh.
///
/// Each region has <see cref="SubChunkConstants.SubChunkCount"/> MeshInstance3D children,
/// one per sub-chunk vertical layer (shared across all chunks in the region).
/// When any chunk in the region changes, the affected sub-chunk layer is rebuilt.
/// </summary>
public sealed partial class ChunkRegion : Node3D
{
    private readonly HashSet<ChunkCoord> _memberChunks = new();
    private readonly MeshInstance3D[] _layerInstances = new MeshInstance3D[SubChunkConstants.SubChunkCount];

    /// <summary>Gets the region coordinate (each component = region index, not chunk index).</summary>
    public ChunkCoord RegionCoord { get; private set; }

    /// <summary>Gets the number of chunks currently assigned to this region.</summary>
    public int ChunkCount => _memberChunks.Count;

    /// <summary>
    /// Initializes the region with its coordinate and creates layer MeshInstance3D nodes.
    /// </summary>
    /// <param name="regionCoord">The region coordinate.</param>
    public void Initialize(ChunkCoord regionCoord)
    {
        RegionCoord = regionCoord;
        Name = $"Region_{regionCoord.X}_{regionCoord.Z}";

        float originX = regionCoord.X * RegionMeshBatcher.RegionSize * ChunkData.SizeX;
        float originZ = regionCoord.Z * RegionMeshBatcher.RegionSize * ChunkData.SizeZ;
        Position = new Vector3(originX, 0, originZ);

        for (int i = 0; i < SubChunkConstants.SubChunkCount; i++)
        {
            MeshInstance3D instance = new();
            instance.Name = $"Layer_{i}";
            AddChild(instance);
            _layerInstances[i] = instance;
        }
    }

    /// <summary>
    /// Registers a chunk as belonging to this region.
    /// </summary>
    /// <param name="coord">The chunk coordinate to add.</param>
    public void AddChunk(ChunkCoord coord) => _memberChunks.Add(coord);

    /// <summary>
    /// Removes a chunk from this region.
    /// </summary>
    /// <param name="coord">The chunk coordinate to remove.</param>
    /// <returns>True if the region is now empty and can be freed.</returns>
    public bool RemoveChunk(ChunkCoord coord)
    {
        _memberChunks.Remove(coord);
        return _memberChunks.Count == 0;
    }

    /// <summary>
    /// Checks if this region contains the given chunk.
    /// </summary>
    /// <param name="coord">The chunk coordinate to check.</param>
    /// <returns>True if the chunk is in this region.</returns>
    public bool ContainsChunk(ChunkCoord coord) => _memberChunks.Contains(coord);

    /// <summary>
    /// Applies a combined mesh to a single sub-chunk layer.
    /// </summary>
    /// <param name="subChunkIndex">The sub-chunk layer index.</param>
    /// <param name="combinedMesh">The combined mesh, or null to clear.</param>
    /// <param name="material">The material to assign.</param>
    public void ApplyLayerMesh(int subChunkIndex, ArrayMesh? combinedMesh, Material? material)
    {
        if (subChunkIndex < 0 || subChunkIndex >= _layerInstances.Length)
        {
            return;
        }

        MeshInstance3D instance = _layerInstances[subChunkIndex];
        instance.Mesh = combinedMesh;

        if (combinedMesh is not null && material is not null)
        {
            instance.MaterialOverride = material;
        }
    }

    /// <summary>
    /// Clears all layer meshes, preparing the region for disposal.
    /// </summary>
    public void ClearAllMeshes()
    {
        for (int i = 0; i < _layerInstances.Length; i++)
        {
            _layerInstances[i].Mesh = null;
        }

        _memberChunks.Clear();
    }
}
