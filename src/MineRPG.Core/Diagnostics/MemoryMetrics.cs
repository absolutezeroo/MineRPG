using System;

namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Captures .NET GC statistics and estimated memory usage.
/// All values are snapshot-based (computed on demand, not sampled continuously).
/// </summary>
public sealed class MemoryMetrics
{
    private const double BytesPerMegabyte = 1024.0 * 1024.0;
    private const int BytesPerBlock = 2;

    private long _activeChunkCount;
    private long _activeMeshVertexCount;

    /// <summary>Number of Gen0 GC collections since last snapshot.</summary>
    public int Gen0Collections { get; private set; }

    /// <summary>Number of Gen1 GC collections since last snapshot.</summary>
    public int Gen1Collections { get; private set; }

    /// <summary>Number of Gen2 GC collections since last snapshot.</summary>
    public int Gen2Collections { get; private set; }

    /// <summary>GC heap size in bytes.</summary>
    public long GcHeapBytes { get; private set; }

    /// <summary>GC heap size in megabytes.</summary>
    public double GcHeapMb => GcHeapBytes / BytesPerMegabyte;

    /// <summary>
    /// Estimated chunk data RAM in megabytes.
    /// Based on active chunk count x block array size.
    /// </summary>
    public double EstimatedChunkDataMb => _activeChunkCount * ChunkDataSizeBytes / BytesPerMegabyte;

    /// <summary>
    /// Estimated mesh data RAM in megabytes.
    /// Rough estimate based on vertex count x bytes per vertex.
    /// </summary>
    public double EstimatedMeshDataMb => _activeMeshVertexCount * EstimatedBytesPerVertex / BytesPerMegabyte;

    /// <summary>Chunk block array size in bytes (16 * 256 * 16 * 2).</summary>
    private static long ChunkDataSizeBytes => 16L * 256 * 16 * BytesPerBlock;

    /// <summary>
    /// Estimated bytes per vertex (position=12, normal=12, uv=8, uv2=8, color=16, index=4).
    /// </summary>
    private const int EstimatedBytesPerVertex = 60;

    /// <summary>
    /// Snapshots the current GC and memory state.
    /// Should be called periodically (e.g., every 250ms) from the main thread.
    /// </summary>
    /// <param name="activeChunks">Number of currently loaded chunks.</param>
    /// <param name="totalVertices">Total vertex count across all meshes.</param>
    public void Snapshot(long activeChunks, long totalVertices)
    {
        Gen0Collections = GC.CollectionCount(0);
        Gen1Collections = GC.CollectionCount(1);
        Gen2Collections = GC.CollectionCount(2);
        GcHeapBytes = GC.GetTotalMemory(false);
        _activeChunkCount = activeChunks;
        _activeMeshVertexCount = totalVertices;
    }
}
