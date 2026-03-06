using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.Events;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Chunks.Serialization;
using MineRPG.World.Events;
using MineRPG.World.Generation;
using MineRPG.World.Meshing;

namespace MineRPG.Godot.World.Pipeline;

/// <summary>
/// Manages a fixed set of long-running worker tasks that consume work items
/// from three priority queues: remesh (highest), generation, and save (lowest).
/// Delegates processing to <see cref="GenerationWorkProcessor"/> and
/// <see cref="RemeshWorkProcessor"/>.
/// </summary>
internal sealed class ChunkWorkerPool : IDisposable
{
    private readonly ConcurrentQueue<RemeshWork> _remeshQueue = new();
    private readonly ConcurrentQueue<ChunkEntry> _generationQueue = new();
    private readonly ConcurrentDictionary<ChunkCoord, CancellationTokenSource> _pendingCts = new();

    private readonly SemaphoreSlim _workSignal;
    private readonly CancellationTokenSource _shutdownCts;
    private readonly Task[] _workers;

    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly ChunkPersistenceService? _persistence;

    private readonly GenerationWorkProcessor _generationProcessor;
    private readonly RemeshWorkProcessor _remeshProcessor;

    /// <summary>Gets the block-edit result queue (high priority, drained without budget).</summary>
    public ConcurrentQueue<ChunkEntry> BlockEditResultQueue { get; } = new();

    /// <summary>Gets the initial-load result queue (drained within frame budget).</summary>
    public ConcurrentQueue<ChunkEntry> LoadResultQueue { get; } = new();

    /// <summary>Gets the pending remesh dedup dictionary.</summary>
    public ConcurrentDictionary<ChunkCoord, byte> PendingRemeshes { get; } = new();

    /// <summary>Gets the block-edit remesh dedup dictionary.</summary>
    public ConcurrentDictionary<ChunkCoord, byte> BlockEditRemeshes { get; } = new();

    /// <summary>
    /// Tracks chunks that had additional block edits while a remesh was already in flight.
    /// The drainer checks this after applying a mesh and re-enqueues if stale.
    /// </summary>
    public ConcurrentDictionary<ChunkCoord, byte> StaleEdits { get; } = new();

    /// <summary>Gets the save queue for enqueueing chunk saves.</summary>
    public ConcurrentQueue<SaveWork> SaveQueue { get; } = new();

    /// <summary>Gets the total pending work item count across all queues.</summary>
    public int PendingWorkCount =>
        BlockEditResultQueue.Count + LoadResultQueue.Count
        + _generationQueue.Count + _remeshQueue.Count + SaveQueue.Count;

    /// <summary>
    /// Creates and starts the worker pool.
    /// </summary>
    /// <param name="chunkManager">Chunk manager for neighbor lookups.</param>
    /// <param name="generator">World generator for terrain creation.</param>
    /// <param name="meshBuilder">Mesh builder for chunk meshing.</param>
    /// <param name="eventBus">Event bus for publishing save events.</param>
    /// <param name="logger">Logger for error reporting.</param>
    /// <param name="persistence">Optional persistence service for saves.</param>
    /// <param name="performanceMonitor">Optional performance metrics recorder.</param>
    /// <param name="optimizationFlags">Optional optimization flags for LOD and packing.</param>
    public ChunkWorkerPool(
        IChunkManager chunkManager,
        IWorldGenerator generator,
        IChunkMeshBuilder meshBuilder,
        IEventBus eventBus,
        ILogger logger,
        ChunkPersistenceService? persistence,
        PerformanceMonitor? performanceMonitor,
        OptimizationFlags? optimizationFlags = null)
    {
        _eventBus = eventBus;
        _logger = logger;
        _persistence = persistence;

        _generationProcessor = new GenerationWorkProcessor(
            chunkManager, generator, meshBuilder, logger,
            persistence, performanceMonitor, optimizationFlags, LoadResultQueue, _pendingCts);

        _remeshProcessor = new RemeshWorkProcessor(
            chunkManager, meshBuilder, logger,
            performanceMonitor, optimizationFlags, PendingRemeshes, BlockEditRemeshes);

        int workerCount = Math.Max(1, System.Environment.ProcessorCount - 1);
        _shutdownCts = new CancellationTokenSource();
        _workSignal = new SemaphoreSlim(0);
        _workers = new Task[workerCount];

        for (int i = 0; i < workerCount; i++)
        {
            _workers[i] = Task.Run(() => WorkerLoopAsync(_shutdownCts.Token));
        }
    }

    /// <summary>
    /// Updates the known player chunk position used for LOD distance calculations.
    /// Called from the main thread before enqueueing generation work.
    /// </summary>
    /// <param name="chunkX">The player's current chunk X coordinate.</param>
    /// <param name="chunkZ">The player's current chunk Z coordinate.</param>
    public void UpdatePlayerChunk(int chunkX, int chunkZ)
    {
        _generationProcessor.UpdatePlayerChunk(chunkX, chunkZ);
    }

    /// <summary>
    /// Enqueues a chunk for generation and signals a worker.
    /// </summary>
    /// <param name="entry">The chunk entry to generate.</param>
    public void EnqueueGeneration(ChunkEntry entry)
    {
        entry.SetState(ChunkState.Generating);

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
    /// Enqueues a remesh for a block edit and signals a worker.
    /// </summary>
    /// <param name="entry">The chunk entry to remesh.</param>
    /// <param name="coord">The chunk coordinate.</param>
    public void EnqueueBlockEditRemesh(ChunkEntry entry, ChunkCoord coord)
    {
        _remeshQueue.Enqueue(new RemeshWork(entry, coord, BlockEditResultQueue));
        _workSignal.Release();
    }

    /// <summary>
    /// Enqueues a neighbor remesh and signals a worker.
    /// </summary>
    /// <param name="entry">The chunk entry to remesh.</param>
    /// <param name="coord">The chunk coordinate.</param>
    /// <param name="targetQueue">The result queue to deliver to.</param>
    public void EnqueueRemesh(ChunkEntry entry, ChunkCoord coord, ConcurrentQueue<ChunkEntry> targetQueue)
    {
        _remeshQueue.Enqueue(new RemeshWork(entry, coord, targetQueue));
        _workSignal.Release();
    }

    /// <summary>
    /// Enqueues a chunk save and signals a worker.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <param name="blockSnapshot">Pre-copied block data (ownership transferred).</param>
    public void EnqueueSave(ChunkCoord coord, ushort[] blockSnapshot)
    {
        SaveQueue.Enqueue(new SaveWork(coord, blockSnapshot));
        _workSignal.Release();
    }

    /// <summary>
    /// Cancels any in-flight generation for the given coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    public void CancelGeneration(ChunkCoord coord)
    {
        if (_pendingCts.TryRemove(coord, out CancellationTokenSource? cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        PendingRemeshes.TryRemove(coord, out _);
        BlockEditRemeshes.TryRemove(coord, out _);
        StaleEdits.TryRemove(coord, out _);
    }

    /// <summary>
    /// Flushes all dirty chunks to the save queue and shuts down workers.
    /// </summary>
    /// <param name="dirtyChunks">Dirty chunk entries to save before shutdown.</param>
    public void Shutdown(IEnumerable<ChunkEntry> dirtyChunks)
    {
        foreach (ChunkEntry remaining in dirtyChunks)
        {
            if (remaining.IsModified && _persistence is not null)
            {
                ushort[] snapshot = new ushort[ChunkData.TotalBlocks];
                remaining.Data.CopyBlocksUnderReadLock(snapshot);
                remaining.IsModified = false;
                SaveQueue.Enqueue(new SaveWork(remaining.Coord, snapshot));
            }
        }

        _shutdownCts.Cancel();

        int wakeCount = _workers.Length + SaveQueue.Count;

        for (int i = 0; i < wakeCount; i++)
        {
            _workSignal.Release();
        }

        foreach (CancellationTokenSource pendingCts in _pendingCts.Values)
        {
            pendingCts.Cancel();
            pendingCts.Dispose();
        }

        Task.WaitAll(_workers, TimeSpan.FromSeconds(10));

        while (SaveQueue.TryDequeue(out SaveWork leftover))
        {
            ProcessSaveWork(leftover);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _workSignal.Dispose();
        _shutdownCts.Dispose();
    }

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
                while (SaveQueue.TryDequeue(out SaveWork finalWork))
                {
                    ProcessSaveWork(finalWork);
                }

                return;
            }

            if (_remeshQueue.TryDequeue(out RemeshWork remeshWork))
            {
                _remeshProcessor.Process(remeshWork);
                continue;
            }

            if (_generationQueue.TryDequeue(out ChunkEntry? genEntry))
            {
                _generationProcessor.Process(genEntry);
                continue;
            }

            if (SaveQueue.TryDequeue(out SaveWork saveWork))
            {
                ProcessSaveWork(saveWork);
            }
        }
    }

    private void ProcessSaveWork(SaveWork work)
    {
        try
        {
            int byteSize = _persistence?.SaveSnapshot(work.Coord, work.BlockSnapshot) ?? 0;

            _eventBus.PublishQueued(new ChunkSavedEvent
            {
                Coord = work.Coord,
                ByteSize = byteSize,
            });
        }
        catch (Exception exception)
        {
            _logger.Error("Async save failed for chunk {0}: {1}", exception, work.Coord, exception.Message);
        }
    }
}
