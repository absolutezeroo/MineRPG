using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Diagnostics;
using MineRPG.Core.Events;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Events;
using MineRPG.World.Generation;
using MineRPG.World.Meshing;

namespace MineRPG.Godot.World;

/// <summary>
/// Drives the async chunk generation and meshing pipeline.
///
/// Priority model:
///   Block edit remeshes -> <see cref="_blockEditQueue"/> (drained fully each frame, no budget).
///   Initial loads and neighbor remeshes -> <see cref="_loadQueue"/> (time-budgeted).
///
/// Thread safety:
///   Block data is snapshotted (ushort[] copy under read lock) before each background
///   remesh task. The snapshot is loaded into a temporary <see cref="ChunkData"/> instance
///   owned by the task. Write locks protect <see cref="ChunkData.SetBlock"/> in WorldNode.
///
/// Concurrency:
///   A <see cref="SemaphoreSlim"/> caps concurrent background tasks at ProcessorCount - 1
///   to prevent thread pool exhaustion during bulk chunk loads.
/// </summary>
public sealed partial class ChunkLoadingScheduler : Node
{
    // TODO: move to GameConfig for runtime tuning
    private const int FrameBudgetMs = 4;
    private const int RenderDistance = 32;

    private readonly ConcurrentQueue<ChunkEntry> _blockEditQueue = new();
    private readonly ConcurrentQueue<ChunkEntry> _loadQueue = new();
    private readonly ConcurrentDictionary<ChunkCoord, CancellationTokenSource> _pendingCts = new();
    private readonly ConcurrentDictionary<ChunkCoord, byte> _pendingRemeshes = new();
    private readonly ConcurrentDictionary<ChunkCoord, byte> _blockEditRemeshes = new();

    private SemaphoreSlim _taskSemaphore = null!;

    private IChunkManager _chunkManager = null!;
    private IWorldGenerator _generator = null!;
    private IChunkMeshBuilder _meshBuilder = null!;
    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private WorldNode _worldNode = null!;
    private PerformanceMonitor? _performanceMonitor;
    private ChunkPersistenceService? _persistence;
    private ChunkAutosaveScheduler? _autosaveScheduler;

    /// <inheritdoc />
    public override void _Ready()
    {
        int maxConcurrent = Math.Max(1, System.Environment.ProcessorCount - 1);
        _taskSemaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);

        _chunkManager = ServiceLocator.Instance.Get<IChunkManager>();
        _generator = ServiceLocator.Instance.Get<IWorldGenerator>();
        _meshBuilder = ServiceLocator.Instance.Get<IChunkMeshBuilder>();
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<ILogger>();
        _worldNode = GetParent<WorldNode>();

        if (ServiceLocator.Instance.TryGet<ChunkPersistenceService>(out ChunkPersistenceService? persistence))
        {
            _persistence = persistence;
        }

        if (ServiceLocator.Instance.TryGet<ChunkAutosaveScheduler>(out ChunkAutosaveScheduler? autosave))
        {
            _autosaveScheduler = autosave;
        }

        if (ServiceLocator.Instance.TryGet<PerformanceMonitor>(out PerformanceMonitor? monitor))
        {
            _performanceMonitor = monitor;
            _performanceMonitor.SetRenderDistance(RenderDistance);
        }

        ServiceLocator.Instance.Register(this);
        _eventBus.Subscribe<PlayerChunkChangedEvent>(OnPlayerChunkChanged);
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        _eventBus.Unsubscribe<PlayerChunkChangedEvent>(OnPlayerChunkChanged);

        foreach (CancellationTokenSource cts in _pendingCts.Values)
        {
            cts.Cancel();
        }

        _taskSemaphore.Dispose();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        // Phase 1: drain ALL block edit remeshes — no budget, player-facing priority.
        while (_blockEditQueue.TryDequeue(out ChunkEntry? editEntry))
        {
            ApplyChunkMesh(editEntry);
        }

        // Phase 2: drain initial chunk loads within a time budget.
        long budgetTicks = (long)(FrameBudgetMs * (Stopwatch.Frequency / 1000.0));
        long startTick = Stopwatch.GetTimestamp();

        while (_loadQueue.TryDequeue(out ChunkEntry? loadEntry))
        {
            ApplyChunkMesh(loadEntry);

            if (Stopwatch.GetTimestamp() - startTick >= budgetTicks)
            {
                break;
            }
        }

        UpdatePerformanceMetrics();
    }

    /// <summary>
    /// Schedule an async remesh for a chunk after a block edit.
    /// The block data is snapshotted under a read lock inside the background task,
    /// guaranteeing a consistent copy. Result is enqueued to the high-priority
    /// <see cref="_blockEditQueue"/>.
    /// </summary>
    /// <param name="coord">The coordinate of the chunk to remesh.</param>
    public void ScheduleBlockEditRemesh(ChunkCoord coord)
    {
        if (!_chunkManager.TryGet(coord, out ChunkEntry? entry) || entry is null)
        {
            return;
        }

        if (entry.State < ChunkState.Ready)
        {
            return;
        }

        if (!_pendingRemeshes.TryAdd(coord, 0))
        {
            return;
        }

        _blockEditRemeshes.TryAdd(coord, 0);

        ScheduleRemeshTask(entry, coord, _blockEditQueue);
    }

    /// <summary>
    /// Forces an immediate load of chunks around the given center coordinate.
    /// </summary>
    /// <param name="center">The center chunk coordinate.</param>
    public void ForceLoadAround(ChunkCoord center) => UpdateLoadedChunks(center);

    private void OnPlayerChunkChanged(PlayerChunkChangedEvent evt) => UpdateLoadedChunks(evt.NewChunk);

    private void UpdateLoadedChunks(ChunkCoord center)
    {
        IReadOnlyList<ChunkCoord> needed = _chunkManager.GetCoordsInRange(center, RenderDistance);
        HashSet<ChunkCoord> neededSet = new(needed);

        // Snapshot to safely modify the collection during iteration
        List<ChunkEntry> snapshot = new(_chunkManager.GetAll());

        foreach (ChunkEntry entry in snapshot)
        {
            if (!neededSet.Contains(entry.Coord))
            {
                UnloadChunk(entry.Coord);
            }
        }

        foreach (ChunkCoord coord in needed)
        {
            ChunkEntry entry = _chunkManager.GetOrCreate(coord);

            if (entry.State == ChunkState.Queued)
            {
                ScheduleChunk(entry);
            }
        }
    }

    private void ScheduleChunk(ChunkEntry entry)
    {
        entry.SetState(ChunkState.Generating);
        CancellationTokenSource cts = new();
        _pendingCts[entry.Coord] = cts;

        // Do NOT pass cts.Token to Task.Run — if the token is already cancelled,
        // the lambda never executes and the finally cleanup (TryRemove) is skipped.
        // The token is passed to WaitAsync and Generate for cooperative cancellation.
        Task.Run(async () =>
        {
            bool semaphoreAcquired = false;

            try
            {
                await _taskSemaphore.WaitAsync(cts.Token).ConfigureAwait(false);
                semaphoreAcquired = true;

                bool isLoaded = _persistence?.TryLoad(entry.Coord, entry.Data) ?? false;

                if (!isLoaded)
                {
                    _generator.Generate(entry, cts.Token);

                    if (cts.Token.IsCancellationRequested)
                    {
                        return;
                    }
                }

                entry.SetState(ChunkState.Generated);
                _performanceMonitor?.IncrementChunksGenerated();
                entry.RecomputeSubChunkInfo();
                entry.SetState(ChunkState.Meshing);

                // No lock needed here: this task is the sole accessor of entry.Data
                // during Generating/Meshing states.
                long meshStart = Stopwatch.GetTimestamp();
                ChunkData?[] neighbors = _chunkManager.GetNeighborData(entry.Coord);
                ChunkMeshResult mesh = _meshBuilder.Build(entry.Data, neighbors, cts.Token);
                _performanceMonitor?.RecordMeshTime(Stopwatch.GetTimestamp() - meshStart);
                _performanceMonitor?.IncrementChunksMeshed();

                if (cts.Token.IsCancellationRequested)
                {
                    return;
                }

                entry.PendingMesh = mesh;
                entry.SetState(ChunkState.Ready);
                _loadQueue.Enqueue(entry);
            }
            catch (OperationCanceledException)
            {
                // Expected when chunk is no longer needed
            }
            catch (Exception exception)
            {
                _logger.Error("Chunk generation failed for {0}: {1}", exception, entry.Coord, exception.Message);
            }
            finally
            {
                _pendingCts.TryRemove(entry.Coord, out _);

                if (semaphoreAcquired)
                {
                    _taskSemaphore.Release();
                }
            }
        });
    }

    private void ApplyChunkMesh(ChunkEntry entry)
    {
        if (entry.PendingMesh is null)
        {
            return;
        }

        bool isRemesh = _pendingRemeshes.TryRemove(entry.Coord, out _);
        bool isBlockEdit = _blockEditRemeshes.TryRemove(entry.Coord, out _);

        ChunkNode chunkNode = _worldNode.GetOrCreateChunkNode(entry.Coord);
        chunkNode.ApplyMesh(entry.PendingMesh!);
        entry.PendingMesh = null;

        _eventBus.Publish(new ChunkMeshedEvent { Coord = entry.Coord });

        if (!isRemesh || isBlockEdit)
        {
            ScheduleNeighborRemeshes(entry.Coord, isBlockEdit);
        }
    }

    private void ScheduleNeighborRemeshes(ChunkCoord coord, bool isFromBlockEdit)
    {
        ChunkCoord[] neighborCoords = [coord.East, coord.West, coord.South, coord.North];

        // Block-edit neighbor remeshes go to the priority queue so both sides
        // of a chunk boundary update in the same frame.
        ConcurrentQueue<ChunkEntry> targetQueue = isFromBlockEdit ? _blockEditQueue : _loadQueue;

        foreach (ChunkCoord neighborCoord in neighborCoords)
        {
            if (!_chunkManager.TryGet(neighborCoord, out ChunkEntry? neighbor) || neighbor is null)
            {
                continue;
            }

            if (neighbor.State < ChunkState.Ready)
            {
                continue;
            }

            if (!_worldNode.HasChunkNode(neighborCoord))
            {
                continue;
            }

            if (!_pendingRemeshes.TryAdd(neighborCoord, 0))
            {
                continue;
            }

            ScheduleRemeshTask(neighbor, neighborCoord, targetQueue);
        }
    }

    /// <summary>
    /// Common remesh task: snapshots block data under a read lock,
    /// builds the mesh on a background thread, and enqueues the result
    /// to the specified target queue.
    /// </summary>
    private void ScheduleRemeshTask(
        ChunkEntry entry,
        ChunkCoord coord,
        ConcurrentQueue<ChunkEntry> targetQueue)
    {
        ChunkEntry capturedEntry = entry;
        ChunkCoord capturedCoord = coord;

        Task.Run(async () =>
        {
            bool semaphoreAcquired = false;

            try
            {
                await _taskSemaphore.WaitAsync().ConfigureAwait(false);
                semaphoreAcquired = true;

                // Snapshot under read lock — excludes concurrent writes from main thread.
                ushort[] buffer = new ushort[ChunkData.TotalBlocks];
                capturedEntry.Data.CopyBlocksUnderReadLock(buffer);

                ChunkData snapshotData = new(capturedCoord);
                snapshotData.LoadFromSpan(buffer);

                ChunkData?[] neighbors = _chunkManager.GetNeighborData(capturedCoord);

                long meshStart = Stopwatch.GetTimestamp();
                ChunkMeshResult mesh = _meshBuilder.Build(snapshotData, neighbors, CancellationToken.None);
                _performanceMonitor?.RecordMeshTime(Stopwatch.GetTimestamp() - meshStart);
                _performanceMonitor?.IncrementChunksMeshed();

                capturedEntry.PendingMesh = mesh;
                capturedEntry.SetState(ChunkState.Ready);
                targetQueue.Enqueue(capturedEntry);
            }
            catch (Exception exception)
            {
                _pendingRemeshes.TryRemove(capturedCoord, out _);
                _blockEditRemeshes.TryRemove(capturedCoord, out _);
                _logger.Error("Remesh failed for {0}: {1}", exception, capturedCoord, exception.Message);
            }
            finally
            {
                if (semaphoreAcquired)
                {
                    _taskSemaphore.Release();
                }
            }
        });
    }

    private void UpdatePerformanceMetrics()
    {
        if (_performanceMonitor is null)
        {
            return;
        }

        _performanceMonitor.SetChunksInQueue(_blockEditQueue.Count + _loadQueue.Count + _pendingCts.Count);
        _performanceMonitor.SetActiveChunks(_chunkManager.Count);
        _performanceMonitor.SetVisibleChunks(_worldNode.ChunkNodeCount);

        ChunkNodePool pool = _worldNode.NodePool;
        _performanceMonitor.SetPoolStats(pool.IdleCount, pool.ActiveCount, pool.RecycleCount);
    }

    private void UnloadChunk(ChunkCoord coord)
    {
        if (_pendingCts.TryRemove(coord, out CancellationTokenSource? cts))
        {
            cts.Cancel();
        }

        _pendingRemeshes.TryRemove(coord, out _);
        _blockEditRemeshes.TryRemove(coord, out _);

        // Delegate persistence to autosave scheduler
        if (_chunkManager.TryGet(coord, out ChunkEntry? entry) && entry is not null)
        {
            _autosaveScheduler?.SaveIfModified(entry);
        }

        _chunkManager.Remove(coord);
        _worldNode.RemoveChunkNode(coord);
    }
}
