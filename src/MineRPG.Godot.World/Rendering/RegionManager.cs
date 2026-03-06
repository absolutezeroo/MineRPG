using System.Collections.Generic;

using Godot;

using MineRPG.Core.Math;
using MineRPG.World.Meshing;

namespace MineRPG.Godot.World.Rendering;

/// <summary>
/// Manages <see cref="ChunkRegion"/> instances, mapping chunk coordinates to
/// their containing 4x4 region. Handles creation and cleanup of regions
/// as chunks load and unload.
///
/// When draw call batching is enabled, chunk meshes are combined per region
/// instead of rendered as individual MeshInstance3D nodes per chunk.
/// </summary>
public sealed partial class RegionManager : Node3D
{
    private readonly Dictionary<ChunkCoord, ChunkRegion> _regions = new();

    /// <summary>Gets the number of active regions.</summary>
    public int RegionCount => _regions.Count;

    /// <summary>
    /// Gets or creates the region for the given chunk coordinate.
    /// Adds the chunk to the region's member set.
    /// </summary>
    /// <param name="chunkCoord">The chunk coordinate.</param>
    /// <returns>The region containing the chunk.</returns>
    public ChunkRegion GetOrCreateRegion(ChunkCoord chunkCoord)
    {
        ChunkCoord regionCoord = RegionMeshBatcher.GetRegionCoord(chunkCoord);

        if (!_regions.TryGetValue(regionCoord, out ChunkRegion? region))
        {
            region = new ChunkRegion();
            region.Initialize(regionCoord);
            AddChild(region);
            _regions[regionCoord] = region;
        }

        region.AddChunk(chunkCoord);
        return region;
    }

    /// <summary>
    /// Removes a chunk from its region. If the region becomes empty, it is freed.
    /// </summary>
    /// <param name="chunkCoord">The chunk coordinate to remove.</param>
    public void RemoveChunk(ChunkCoord chunkCoord)
    {
        ChunkCoord regionCoord = RegionMeshBatcher.GetRegionCoord(chunkCoord);

        if (!_regions.TryGetValue(regionCoord, out ChunkRegion? region))
        {
            return;
        }

        bool isEmpty = region.RemoveChunk(chunkCoord);

        if (isEmpty)
        {
            region.ClearAllMeshes();
            RemoveChild(region);
            region.QueueFree();
            _regions.Remove(regionCoord);
        }
    }

    /// <summary>
    /// Gets the region for a chunk coordinate, if it exists.
    /// </summary>
    /// <param name="chunkCoord">The chunk coordinate.</param>
    /// <param name="region">The region, or null if not found.</param>
    /// <returns>True if the region exists.</returns>
    public bool TryGetRegion(ChunkCoord chunkCoord, out ChunkRegion? region)
    {
        ChunkCoord regionCoord = RegionMeshBatcher.GetRegionCoord(chunkCoord);
        return _regions.TryGetValue(regionCoord, out region);
    }

    /// <summary>
    /// Returns all active regions.
    /// </summary>
    /// <returns>An enumerable of all regions.</returns>
    public IEnumerable<ChunkRegion> GetAllRegions() => _regions.Values;

    /// <summary>
    /// Clears all regions and frees their nodes.
    /// </summary>
    public void ClearAll()
    {
        foreach (ChunkRegion region in _regions.Values)
        {
            region.ClearAllMeshes();
            RemoveChild(region);
            region.QueueFree();
        }

        _regions.Clear();
    }
}
