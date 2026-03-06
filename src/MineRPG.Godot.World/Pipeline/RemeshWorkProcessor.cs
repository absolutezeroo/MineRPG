using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Meshing;
using MineRPG.World.Spatial;

namespace MineRPG.Godot.World.Pipeline;

/// <summary>
/// Processes chunk remesh work items: snapshots block data, rebuilds the mesh,
/// and enqueues the result for the main thread.
/// </summary>
internal sealed class RemeshWorkProcessor
{
    private readonly IChunkManager _chunkManager;
    private readonly IChunkMeshBuilder _meshBuilder;
    private readonly ILogger _logger;
    private readonly PerformanceMonitor? _performanceMonitor;
    private readonly ConcurrentDictionary<ChunkCoord, byte> _pendingRemeshes;
    private readonly ConcurrentDictionary<ChunkCoord, byte> _blockEditRemeshes;

    /// <summary>
    /// Creates a remesh work processor.
    /// </summary>
    /// <param name="chunkManager">Chunk manager for neighbor lookups.</param>
    /// <param name="meshBuilder">Mesh builder for chunk meshing.</param>
    /// <param name="logger">Logger for error reporting.</param>
    /// <param name="performanceMonitor">Optional performance metrics recorder.</param>
    /// <param name="pendingRemeshes">Shared dedup dictionary for pending remeshes.</param>
    /// <param name="blockEditRemeshes">Shared dedup dictionary for block-edit remeshes.</param>
    public RemeshWorkProcessor(
        IChunkManager chunkManager,
        IChunkMeshBuilder meshBuilder,
        ILogger logger,
        PerformanceMonitor? performanceMonitor,
        ConcurrentDictionary<ChunkCoord, byte> pendingRemeshes,
        ConcurrentDictionary<ChunkCoord, byte> blockEditRemeshes)
    {
        _chunkManager = chunkManager;
        _meshBuilder = meshBuilder;
        _logger = logger;
        _performanceMonitor = performanceMonitor;
        _pendingRemeshes = pendingRemeshes;
        _blockEditRemeshes = blockEditRemeshes;
    }

    /// <summary>
    /// Processes a single remesh work item. Snapshots data, computes sub-chunks,
    /// builds mesh, and enqueues the result.
    /// </summary>
    /// <param name="work">The remesh work item to process.</param>
    public void Process(RemeshWork work)
    {
        ushort[] buffer = ArrayPool<ushort>.Shared.Rent(ChunkData.TotalBlocks);

        try
        {
            work.Entry.Data.CopyBlocksUnderReadLock(buffer);

            ChunkData snapshotData = new(work.Coord);
            snapshotData.LoadFromSpan(buffer);

            work.Entry.SubChunks = snapshotData.ComputeSubChunkInfo();
            work.Entry.VisibilityMatrix = VisibilityMatrixBuilder.Build(snapshotData, work.Entry.SubChunks);

            ChunkData?[] neighbors = _chunkManager.GetNeighborData(work.Coord);

            long meshStart = Stopwatch.GetTimestamp();
            ChunkMeshResult mesh = _meshBuilder.Build(snapshotData, neighbors, CancellationToken.None);
            _performanceMonitor?.RecordMeshTime(Stopwatch.GetTimestamp() - meshStart);
            _performanceMonitor?.IncrementChunksMeshed();

            work.Entry.PendingMesh = mesh;
            work.Entry.SetState(ChunkState.Ready);
            work.TargetQueue.Enqueue(work.Entry);
        }
        catch (Exception exception)
        {
            _pendingRemeshes.TryRemove(work.Coord, out _);
            _blockEditRemeshes.TryRemove(work.Coord, out _);
            _logger.Error("Remesh failed for {0}: {1}", exception, work.Coord, exception.Message);
        }
        finally
        {
            ArrayPool<ushort>.Shared.Return(buffer);
        }
    }
}
