using System.Collections.Concurrent;
using System.Diagnostics;

using MineRPG.Core.DataLoading;
using MineRPG.Core.Diagnostics;
using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Events;
using MineRPG.World.Meshing;
using MineRPG.World.Spatial;

using MineRPG.Godot.World.Chunks;
using MineRPG.Godot.World.Rendering;

namespace MineRPG.Godot.World.Pipeline;

/// <summary>
/// Drains completed chunk mesh results from worker queues onto the main thread.
/// Phase 1: all block-edit results (no budget - player-facing priority).
/// Phase 2: initial load results within a frame time budget.
/// After applying a mesh, schedules neighbor remeshes as needed.
/// </summary>
internal sealed class ChunkResultDrainer
{
    private readonly ChunkWorkerPool _workerPool;
    private readonly IChunkManager _chunkManager;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly WorldNode _worldNode;
    private readonly PreloadProgress? _preloadProgress;
    private readonly OptimizationFlags? _optimizationFlags;
    private RegionManager? _regionManager;
    private RegionLayerBuilder? _regionLayerBuilder;

    /// <summary>
    /// Creates a result drainer.
    /// </summary>
    /// <param name="workerPool">The worker pool owning the result queues.</param>
    /// <param name="chunkManager">Chunk manager for validation.</param>
    /// <param name="eventBus">Event bus for publishing mesh events.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="worldNode">World node for chunk node creation.</param>
    /// <param name="preloadProgress">Optional preload progress tracker.</param>
    /// <param name="optimizationFlags">Optional optimization flags for batching.</param>
    public ChunkResultDrainer(
        ChunkWorkerPool workerPool,
        IChunkManager chunkManager,
        IEventBus eventBus,
        ILogger logger,
        WorldNode worldNode,
        PreloadProgress? preloadProgress,
        OptimizationFlags? optimizationFlags = null)
    {
        _workerPool = workerPool;
        _chunkManager = chunkManager;
        _eventBus = eventBus;
        _logger = logger;
        _worldNode = worldNode;
        _preloadProgress = preloadProgress;
        _optimizationFlags = optimizationFlags;
    }

    /// <summary>
    /// Drains block-edit results without budget, then initial-load results
    /// within the given frame budget.
    /// </summary>
    /// <param name="frameBudgetMs">Maximum milliseconds to spend on initial loads.</param>
    public void DrainResults(int frameBudgetMs)
    {
        while (_workerPool.BlockEditResultQueue.TryDequeue(out ChunkEntry? editEntry))
        {
            ApplyChunkMesh(editEntry);
        }

        long budgetTicks = (long)(frameBudgetMs * (Stopwatch.Frequency / 1000.0));
        long startTick = Stopwatch.GetTimestamp();

        while (_workerPool.LoadResultQueue.TryDequeue(out ChunkEntry? loadEntry))
        {
            ApplyChunkMesh(loadEntry);

            if (Stopwatch.GetTimestamp() - startTick >= budgetTicks)
            {
                break;
            }
        }
    }

    private void ApplyChunkMesh(ChunkEntry entry)
    {
        if (entry.PendingMesh is null)
        {
            return;
        }

        if (!_chunkManager.TryGet(entry.Coord, out _))
        {
            entry.PendingMesh = null;
            _workerPool.PendingRemeshes.TryRemove(entry.Coord, out _);
            _workerPool.BlockEditRemeshes.TryRemove(entry.Coord, out _);
            return;
        }

        bool isRemesh = _workerPool.PendingRemeshes.TryRemove(entry.Coord, out _);
        bool isBlockEdit = _workerPool.BlockEditRemeshes.TryRemove(entry.Coord, out _);

        ChunkNode chunkNode = _worldNode.GetOrCreateChunkNode(entry.Coord);
        chunkNode.ApplyMesh(entry.PendingMesh!);
        chunkNode.SubChunkMetadata = entry.SubChunks;
        entry.LastMeshResult = entry.PendingMesh;
        entry.PendingMesh = null;

        // Region batching: hide individual chunk and rebuild the region's combined mesh
        if (_optimizationFlags is not null && _optimizationFlags.DrawCallBatchingEnabled
            && entry.CurrentLod == LodLevel.Lod0)
        {
            EnsureRegionManager();

            if (_regionManager is not null && _regionLayerBuilder is not null)
            {
                ChunkRegion region = _regionManager.GetOrCreateRegion(entry.Coord);
                chunkNode.Visible = false;
                _regionLayerBuilder.RebuildRegion(region);
            }
        }

        if (entry.VisibilityMatrix.HasValue
            && ServiceLocator.Instance.TryGet<OcclusionCuller>(out OcclusionCuller? culler)
            && culler is not null)
        {
            culler.SetMatrix(entry.Coord, entry.VisibilityMatrix.Value);
        }

        _eventBus.Publish(new ChunkMeshedEvent { Coord = entry.Coord });

        if (!isRemesh && !isBlockEdit)
        {
            _eventBus.Publish(new ChunkGeneratedEvent { Coord = entry.Coord });
        }

        if (!isRemesh && _preloadProgress is not null && !_preloadProgress.IsComplete)
        {
            int newCount = _preloadProgress.Increment();

            if (_preloadProgress.IsComplete)
            {
                _eventBus.Publish(new WorldReadyEvent());
                _logger.Info(
                    "ChunkLoadingScheduler: Preload complete ({0} chunks meshed).", newCount);
            }
        }

        if (!isRemesh || isBlockEdit)
        {
            ScheduleNeighborRemeshes(entry.Coord, isBlockEdit);
        }

        // Another block edit happened while this remesh was in flight.
        // PendingRemeshes was just cleared, so re-enqueue to capture the latest data.
        if (_workerPool.StaleEdits.TryRemove(entry.Coord, out _))
        {
            if (_chunkManager.TryGet(entry.Coord, out ChunkEntry? staleEntry)
                && staleEntry is not null
                && staleEntry.State >= ChunkState.Ready
                && staleEntry.State != ChunkState.Unloading
                && _workerPool.PendingRemeshes.TryAdd(entry.Coord, 0))
            {
                _workerPool.BlockEditRemeshes.TryAdd(entry.Coord, 0);
                _workerPool.EnqueueBlockEditRemesh(staleEntry, entry.Coord);
            }
        }
    }

    /// <summary>
    /// Lazily resolves the RegionManager from ServiceLocator.
    /// RegionManager is created by GameplayBootstrap after the scheduler is ready.
    /// </summary>
    private void EnsureRegionManager()
    {
        if (_regionManager is not null)
        {
            return;
        }

        if (ServiceLocator.Instance.TryGet<RegionManager>(out RegionManager? manager)
            && manager is not null)
        {
            _regionManager = manager;
            _regionLayerBuilder = new RegionLayerBuilder(_chunkManager);
        }
    }

    private void ScheduleNeighborRemeshes(ChunkCoord coord, bool isFromBlockEdit)
    {
        ChunkCoord[] neighborCoords = [coord.East, coord.West, coord.South, coord.North];

        ConcurrentQueue<ChunkEntry> targetQueue = isFromBlockEdit
            ? _workerPool.BlockEditResultQueue
            : _workerPool.LoadResultQueue;

        foreach (ChunkCoord neighborCoord in neighborCoords)
        {
            if (!_chunkManager.TryGet(neighborCoord, out ChunkEntry? neighbor) || neighbor is null)
            {
                continue;
            }

            if (neighbor.State < ChunkState.Ready || neighbor.State == ChunkState.Unloading)
            {
                continue;
            }

            if (!_worldNode.HasChunkNode(neighborCoord))
            {
                continue;
            }

            if (!_workerPool.PendingRemeshes.TryAdd(neighborCoord, 0))
            {
                continue;
            }

            _workerPool.EnqueueRemesh(neighbor, neighborCoord, targetQueue);
        }
    }
}
