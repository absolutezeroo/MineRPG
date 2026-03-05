using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Diagnostics;
using MineRPG.Core.Events;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.Entities.Player;
using MineRPG.World.Chunks;
using MineRPG.World.Events;
using MineRPG.World.Generation;
using MineRPG.World.Meshing;

namespace MineRPG.Godot.World;

/// <summary>
/// Drives the async chunk generation, meshing, saving, and unloading pipeline
/// using a fixed worker pool.
///
/// Priority model:
///   1. Block edit remeshes  -> <see cref="_remeshQueue"/> (workers check this FIRST).
///   2. Initial loads        -> <see cref="_generationQueue"/> (workers check this second).
///   3. Chunk saves          -> <see cref="_saveQueue"/> (workers check this third).
///   Results are enqueued to <see cref="_blockEditResultQueue"/> or <see cref="_loadResultQueue"/>
///   and drained on the main thread in <see cref="_Process"/>.
///   Chunk node cleanups are deferred to <see cref="_pendingNodeCleanup"/> and drained
///   within a separate frame budget.
///
/// Worker pool:
///   A fixed number of long-running worker tasks (ProcessorCount - 1) consume work items
///   from the three queues. Remesh work is always checked first, so block edits are never
///   starved by a backlog of generation tasks.
///
/// Thread safety:
///   Block data is snapshotted (ushort[] copy under read lock) before each background
///   remesh or save task. Write locks protect <see cref="ChunkData.SetBlock"/> in WorldNode.
///   During initial generation, the worker is the sole accessor of entry.Data.
/// </summary>
public sealed partial class ChunkLoadingScheduler : Node
{
    /// <summary>The default render distance in chunks.</summary>
    public const int DefaultRenderDistance = 32;

    private const int FrameBudgetMs = 4;
    private const int UnloadFrameBudgetMs = 2;

    private int _renderDistance = DefaultRenderDistance;

    /// <summary>
    /// Gets the current render distance in chunks.
    /// </summary>
    public int CurrentRenderDistance => _renderDistance;

    // --- Work queues (fed by main thread, consumed by workers) ---
    private readonly ConcurrentQueue<ChunkEntry> _generationQueue = new();
    private readonly ConcurrentQueue<RemeshWork> _remeshQueue = new();
    private readonly ConcurrentQueue<SaveWork> _saveQueue = new();

    // --- Result queues (fed by workers, consumed by main thread in _Process) ---
    private readonly ConcurrentQueue<ChunkEntry> _blockEditResultQueue = new();
    private readonly ConcurrentQueue<ChunkEntry> _loadResultQueue = new();

    // --- Deferred node cleanup queue (main thread only) ---
    private readonly ConcurrentQueue<NodeCleanupWork> _pendingNodeCleanup = new();

    // --- Dedup tracking ---
    private readonly ConcurrentDictionary<ChunkCoord, CancellationTokenSource> _pendingCts = new();
    private readonly ConcurrentDictionary<ChunkCoord, byte> _pendingRemeshes = new();
    private readonly ConcurrentDictionary<ChunkCoord, byte> _blockEditRemeshes = new();

    // --- Worker pool ---
    private SemaphoreSlim _workSignal = null!;
    private CancellationTokenSource _shutdownCts = null!;
    private Task[] _workers = null!;

    private IChunkManager _chunkManager = null!;
    private IWorldGenerator _generator = null!;
    private IChunkMeshBuilder _meshBuilder = null!;
    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private WorldNode _worldNode = null!;
    private PerformanceMonitor? _performanceMonitor;
    private ChunkPersistenceService? _persistence;
    private PreloadProgress? _preloadProgress;

    /// <inheritdoc />
    public override void _Ready()
    {
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

        if (ServiceLocator.Instance.TryGet<PerformanceMonitor>(out PerformanceMonitor? monitor))
        {
            _performanceMonitor = monitor;
            _performanceMonitor.SetRenderDistance(_renderDistance);
        }

        if (ServiceLocator.Instance.TryGet<PreloadProgress>(out PreloadProgress? preloadProgress))
        {
            _preloadProgress = preloadProgress;
        }

        // Start worker pool
        int workerCount = Math.Max(1, System.Environment.ProcessorCount - 1);
        _shutdownCts = new CancellationTokenSource();
        _workSignal = new SemaphoreSlim(0);
        _workers = new Task[workerCount];

        for (int i = 0; i < workerCount; i++)
        {
            _workers[i] = Task.Run(() => WorkerLoopAsync(_shutdownCts.Token));
        }

        ServiceLocator.Instance.Register(this);
        _eventBus.Subscribe<PlayerChunkChangedEvent>(OnPlayerChunkChanged);

        // Kick the initial chunk loading using the player's spawn position.
        // PlayerNode is frozen (ProcessMode.Disabled) during preload, so no
        // PlayerChunkChangedEvent fires naturally — we must seed the pipeline here.
        if (ServiceLocator.Instance.TryGet<PlayerData>(out PlayerData? playerData)
            && playerData is not null)
        {
            ChunkCoord2D chunkCoord = VoxelMath.WorldToChunk(
                (int)MathF.Floor(playerData.PositionX),
                (int)MathF.Floor(playerData.PositionZ),
                ChunkData.SizeX, ChunkData.SizeZ);
            ChunkCoord spawnChunk = new(chunkCoord.ChunkX, chunkCoord.ChunkZ);
            ForceLoadAround(spawnChunk);
            _logger.Info(
                "ChunkLoadingScheduler: Initial preload started around chunk {0}.", spawnChunk);
        }
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        _eventBus.Unsubscribe<PlayerChunkChangedEvent>(OnPlayerChunkChanged);

        // Enqueue saves for all remaining dirty chunks before signaling shutdown
        foreach (ChunkEntry remaining in _chunkManager.GetAll())
        {
            if (remaining.IsModified && _persistence is not null)
            {
                ushort[] snapshot = new ushort[ChunkData.TotalBlocks];
                remaining.Data.CopyBlocksUnderReadLock(snapshot);
                remaining.IsModified = false;
                _saveQueue.Enqueue(new SaveWork(remaining.Coord, snapshot));
            }
        }

        // Signal all workers to stop — they will drain remaining saves before exiting
        _shutdownCts.Cancel();

        // Wake all workers so they process remaining saves then exit
        int wakeCount = _workers.Length + _saveQueue.Count;

        for (int i = 0; i < wakeCount; i++)
        {
            _workSignal.Release();
        }

        // Cancel and dispose any pending generation CTS
        foreach (CancellationTokenSource pendingCts in _pendingCts.Values)
        {
            pendingCts.Cancel();
            pendingCts.Dispose();
        }

        // Wait briefly for workers to flush saves — avoid blocking the main thread for 10s
        bool completed = Task.WaitAll(_workers, TimeSpan.FromSeconds(2));

        if (!completed)
        {
            _logger.Warning("ChunkLoadingScheduler: Workers did not finish within 2s shutdown window.");
        }

        // Drain any saves workers did not reach (safety net)
        while (_saveQueue.TryDequeue(out SaveWork leftover))
        {
            ProcessSaveWork(leftover);
        }

        _workSignal.Dispose();
        _shutdownCts.Dispose();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        // Phase 1: drain ALL block edit remeshes — no budget, player-facing priority.
        while (_blockEditResultQueue.TryDequeue(out ChunkEntry? editEntry))
        {
            ApplyChunkMesh(editEntry);
        }

        // Phase 2: drain initial chunk loads within a time budget.
        long budgetTicks = (long)(FrameBudgetMs * (Stopwatch.Frequency / 1000.0));
        long startTick = Stopwatch.GetTimestamp();

        while (_loadResultQueue.TryDequeue(out ChunkEntry? loadEntry))
        {
            ApplyChunkMesh(loadEntry);

            if (Stopwatch.GetTimestamp() - startTick >= budgetTicks)
            {
                break;
            }
        }

        // Phase 3: drain deferred node cleanups within a separate budget.
        long unloadBudgetTicks = (long)(UnloadFrameBudgetMs * (Stopwatch.Frequency / 1000.0));
        long unloadStartTick = Stopwatch.GetTimestamp();

        while (_pendingNodeCleanup.TryDequeue(out NodeCleanupWork cleanupWork))
        {
            _worldNode.ReturnChunkNodeToPool(cleanupWork.Node);

            if (Stopwatch.GetTimestamp() - unloadStartTick >= unloadBudgetTicks)
            {
                break;
            }
        }

        UpdatePerformanceMetrics();
    }

    /// <summary>
    /// Schedule an async remesh for a chunk after a block edit.
    /// The block data is snapshotted under a read lock inside the worker,
    /// guaranteeing a consistent copy. Result is enqueued to the high-priority
    /// <see cref="_blockEditResultQueue"/>.
    /// </summary>
    /// <param name="coord">The coordinate of the chunk to remesh.</param>
    public void ScheduleBlockEditRemesh(ChunkCoord coord)
    {
        if (!_chunkManager.TryGet(coord, out ChunkEntry? entry) || entry is null)
        {
            return;
        }

        if (entry.State < ChunkState.Ready || entry.State == ChunkState.Unloading)
        {
            return;
        }

        if (!_pendingRemeshes.TryAdd(coord, 0))
        {
            return;
        }

        _blockEditRemeshes.TryAdd(coord, 0);

        _remeshQueue.Enqueue(new RemeshWork(entry, coord, _blockEditResultQueue));
        _workSignal.Release();
    }

    /// <summary>
    /// Enqueues a pre-snapshotted chunk save for background processing.
    /// Called by ChunkAutosaveScheduler during periodic autosave.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <param name="blockSnapshot">Pre-copied block array (ownership transferred).</param>
    public void EnqueueSave(ChunkCoord coord, ushort[] blockSnapshot)
    {
        _saveQueue.Enqueue(new SaveWork(coord, blockSnapshot));
        _workSignal.Release();
    }

    /// <summary>
    /// Forces an immediate load of chunks around the given center coordinate.
    /// </summary>
    /// <param name="center">The center chunk coordinate.</param>
    public void ForceLoadAround(ChunkCoord center) => UpdateLoadedChunks(center);

    /// <summary>
    /// Sets the render distance in chunks. Clamped to [4, 64].
    /// </summary>
    /// <param name="distance">The new render distance.</param>
    public void SetRenderDistance(int distance)
    {
        _renderDistance = Math.Clamp(distance, 4, 64);
        _performanceMonitor?.SetRenderDistance(_renderDistance);
    }

    private void OnPlayerChunkChanged(PlayerChunkChangedEvent evt) => UpdateLoadedChunks(evt.NewChunk);

    private void UpdateLoadedChunks(ChunkCoord center)
    {
        IReadOnlyList<ChunkCoord> needed = _chunkManager.GetCoordsInRange(center, _renderDistance);
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

        // Dispose any existing CTS for this coord before replacing
        if (_pendingCts.TryRemove(entry.Coord, out CancellationTokenSource? oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }

        CancellationTokenSource cts = new();
        _pendingCts[entry.Coord] = cts;

        _generationQueue.Enqueue(entry);
        _workSignal.Release();
    }

    /// <summary>
    /// Long-running worker loop. Each worker checks queues by priority:
    /// 1. Remesh (block edits), 2. Generation, 3. Saves.
    /// On shutdown, workers drain remaining save work before exiting.
    /// </summary>
    private async Task WorkerLoopAsync(CancellationToken shutdownToken)
    {
        while (true)
        {
            try
            {
                await _workSignal.WaitAsync(shutdownToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Drain remaining saves before exiting — shutdown flush
                while (_saveQueue.TryDequeue(out SaveWork finalWork))
                {
                    ProcessSaveWork(finalWork);
                }

                return;
            }

            // Priority 1: remesh work (block edits)
            if (_remeshQueue.TryDequeue(out RemeshWork remeshWork))
            {
                ProcessRemeshWork(remeshWork);
                continue;
            }

            // Priority 2: chunk generation
            if (_generationQueue.TryDequeue(out ChunkEntry? genEntry))
            {
                ProcessGenerationWork(genEntry);
                continue;
            }

            // Priority 3: chunk save (unload saves and periodic autosave)
            if (_saveQueue.TryDequeue(out SaveWork saveWork))
            {
                ProcessSaveWork(saveWork);
                continue;
            }

            // Spurious wake — no work available, loop back to wait
        }
    }

    private void ProcessRemeshWork(RemeshWork work)
    {
        try
        {
            // Snapshot under read lock — excludes concurrent writes from main thread.
            ushort[] buffer = new ushort[ChunkData.TotalBlocks];
            work.Entry.Data.CopyBlocksUnderReadLock(buffer);

            ChunkData snapshotData = new(work.Coord);
            snapshotData.LoadFromSpan(buffer);

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
    }

    private void ProcessGenerationWork(ChunkEntry entry)
    {
        // TryRemove takes ownership — prevents UnloadChunk from disposing under us
        _pendingCts.TryRemove(entry.Coord, out CancellationTokenSource? cts);
        CancellationToken token = cts?.Token ?? CancellationToken.None;

        try
        {
            if (token.IsCancellationRequested)
            {
                return;
            }

            bool isLoaded = _persistence?.TryLoad(entry.Coord, entry.Data) ?? false;

            if (!isLoaded)
            {
                _generator.Generate(entry, token);

                if (token.IsCancellationRequested)
                {
                    return;
                }
            }

            entry.SetState(ChunkState.Generated);
            _performanceMonitor?.IncrementChunksGenerated();
            entry.RecomputeSubChunkInfo();
            entry.SetState(ChunkState.Meshing);

            // No lock needed here: this worker is the sole accessor of entry.Data
            // during Generating/Meshing states.
            long meshStart = Stopwatch.GetTimestamp();
            ChunkData?[] neighbors = _chunkManager.GetNeighborData(entry.Coord);
            ChunkMeshResult mesh = _meshBuilder.Build(entry.Data, neighbors, token);
            _performanceMonitor?.RecordMeshTime(Stopwatch.GetTimestamp() - meshStart);
            _performanceMonitor?.IncrementChunksMeshed();

            if (token.IsCancellationRequested)
            {
                return;
            }

            entry.PendingMesh = mesh;
            entry.SetState(ChunkState.Ready);
            _loadResultQueue.Enqueue(entry);
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
            cts?.Dispose();
        }
    }

    private void ProcessSaveWork(SaveWork work)
    {
        try
        {
            _persistence?.SaveSnapshot(work.Coord, work.BlockSnapshot);
        }
        catch (Exception exception)
        {
            _logger.Error("Async save failed for chunk {0}: {1}", exception, work.Coord, exception.Message);
        }
    }

    private void ApplyChunkMesh(ChunkEntry entry)
    {
        if (entry.PendingMesh is null)
        {
            return;
        }

        // The chunk may have been unloaded between when the worker enqueued this
        // result and when ApplyChunkMesh runs on the main thread. Drop stale results.
        if (!_chunkManager.TryGet(entry.Coord, out _))
        {
            entry.PendingMesh = null;
            _pendingRemeshes.TryRemove(entry.Coord, out _);
            _blockEditRemeshes.TryRemove(entry.Coord, out _);
            return;
        }

        bool isRemesh = _pendingRemeshes.TryRemove(entry.Coord, out _);
        bool isBlockEdit = _blockEditRemeshes.TryRemove(entry.Coord, out _);

        ChunkNode chunkNode = _worldNode.GetOrCreateChunkNode(entry.Coord);
        chunkNode.ApplyMesh(entry.PendingMesh!);
        entry.PendingMesh = null;

        _eventBus.Publish(new ChunkMeshedEvent { Coord = entry.Coord });

        // Track preload progress — only count initial loads, not neighbor remeshes.
        // Fires WorldReadyEvent exactly once when the preload target is reached.
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
    }

    private void ScheduleNeighborRemeshes(ChunkCoord coord, bool isFromBlockEdit)
    {
        ChunkCoord[] neighborCoords = [coord.East, coord.West, coord.South, coord.North];

        // Block-edit neighbor remeshes go to the priority queue so both sides
        // of a chunk boundary update in the same frame.
        ConcurrentQueue<ChunkEntry> targetQueue = isFromBlockEdit ? _blockEditResultQueue : _loadResultQueue;

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

            if (!_pendingRemeshes.TryAdd(neighborCoord, 0))
            {
                continue;
            }

            _remeshQueue.Enqueue(new RemeshWork(neighbor, neighborCoord, targetQueue));
            _workSignal.Release();
        }
    }

    private void UpdatePerformanceMetrics()
    {
        if (_performanceMonitor is null)
        {
            return;
        }

        int pendingWork = _blockEditResultQueue.Count + _loadResultQueue.Count
            + _generationQueue.Count + _remeshQueue.Count + _saveQueue.Count;
        _performanceMonitor.SetChunksInQueue(pendingWork);
        _performanceMonitor.SetActiveChunks(_chunkManager.Count);
        _performanceMonitor.SetVisibleChunks(_worldNode.ChunkNodeCount);

        ChunkNodePool pool = _worldNode.NodePool;
        _performanceMonitor.SetPoolStats(pool.IdleCount, pool.ActiveCount, pool.RecycleCount);
    }

    private void UnloadChunk(ChunkCoord coord)
    {
        // Step 1: cancel any in-flight generation
        if (_pendingCts.TryRemove(coord, out CancellationTokenSource? cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        _pendingRemeshes.TryRemove(coord, out _);
        _blockEditRemeshes.TryRemove(coord, out _);

        // Step 2: snapshot block data and enqueue async save before removing from manager
        if (_chunkManager.TryGet(coord, out ChunkEntry? entry) && entry is not null)
        {
            entry.SetState(ChunkState.Unloading);

            if (entry.IsModified && _persistence is not null)
            {
                ushort[] snapshot = new ushort[ChunkData.TotalBlocks];
                entry.Data.CopyBlocksUnderReadLock(snapshot);
                entry.IsModified = false;

                _saveQueue.Enqueue(new SaveWork(coord, snapshot));
                _workSignal.Release();
            }
        }

        // Step 3: remove from manager — no further access to entry from any thread
        _chunkManager.Remove(coord);

        // Step 4: defer node cleanup to _Process frame budget
        if (_worldNode.TryExtractChunkNode(coord, out ChunkNode? node) && node is not null)
        {
            _pendingNodeCleanup.Enqueue(new NodeCleanupWork(coord, node));
        }
    }

    /// <summary>
    /// Work item for a remesh operation. Carries the entry, coord, and target result queue.
    /// </summary>
    private readonly record struct RemeshWork(
        ChunkEntry Entry,
        ChunkCoord Coord,
        ConcurrentQueue<ChunkEntry> TargetQueue);

    /// <summary>
    /// Work item for a background chunk save. Carries the coordinate and a
    /// pre-copied block snapshot. The snapshot array is owned by this work item.
    /// </summary>
    private readonly record struct SaveWork(ChunkCoord Coord, ushort[] BlockSnapshot);

    /// <summary>
    /// Deferred scene-tree cleanup for a chunk node. Applied on the main thread
    /// within the <see cref="UnloadFrameBudgetMs"/> budget.
    /// </summary>
    private readonly record struct NodeCleanupWork(ChunkCoord Coord, ChunkNode Node);
}
