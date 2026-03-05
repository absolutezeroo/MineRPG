using System;
using System.Collections.Generic;

using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Diagnostics;
using MineRPG.Core.Events;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.Entities.Player;
using MineRPG.Godot.World.Pipeline;
using MineRPG.World.Chunks;
using MineRPG.World.Events;
using MineRPG.World.Generation;
using MineRPG.World.Meshing;

namespace MineRPG.Godot.World;

/// <summary>
/// Orchestrates the async chunk generation, meshing, saving, and unloading pipeline.
/// Delegates work to specialized sub-systems:
/// <see cref="ChunkWorkerPool"/> for background processing,
/// <see cref="ChunkResultDrainer"/> for main-thread mesh application,
/// <see cref="ChunkNodeCleaner"/> for deferred node cleanup,
/// <see cref="ChunkDistanceEvaluator"/> for load/unload decisions.
/// </summary>
public sealed partial class ChunkLoadingScheduler : Node
{
    /// <summary>The default render distance in chunks.</summary>
    public const int DefaultRenderDistance = 32;

    private const int FrameBudgetMs = 4;
    private const int UnloadFrameBudgetMs = 2;

    private int _renderDistance = DefaultRenderDistance;

    private ChunkWorkerPool _workerPool = null!;
    private ChunkResultDrainer _resultDrainer = null!;
    private ChunkNodeCleaner _nodeCleaner = null!;
    private ChunkDistanceEvaluator _distanceEvaluator = null!;

    private IChunkManager _chunkManager = null!;
    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private WorldNode _worldNode = null!;
    private PerformanceMonitor? _performanceMonitor;
    private ChunkPersistenceService? _persistence;

    private readonly List<ChunkEntry> _loadBuffer = new();
    private readonly List<ChunkCoord> _unloadBuffer = new();

    /// <summary>
    /// Gets the current render distance in chunks.
    /// </summary>
    public int CurrentRenderDistance => _renderDistance;

    /// <inheritdoc />
    public override void _Ready()
    {
        _chunkManager = ServiceLocator.Instance.Get<IChunkManager>();
        IWorldGenerator generator = ServiceLocator.Instance.Get<IWorldGenerator>();
        IChunkMeshBuilder meshBuilder = ServiceLocator.Instance.Get<IChunkMeshBuilder>();
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

        PreloadProgress? preloadProgress = null;

        if (ServiceLocator.Instance.TryGet<PreloadProgress>(out PreloadProgress? progress))
        {
            preloadProgress = progress;
        }

        _workerPool = new ChunkWorkerPool(
            _chunkManager, generator, meshBuilder, _logger, _persistence, _performanceMonitor);
        _resultDrainer = new ChunkResultDrainer(
            _workerPool, _chunkManager, _eventBus, _logger, _worldNode, preloadProgress);
        _nodeCleaner = new ChunkNodeCleaner(_worldNode);
        _distanceEvaluator = new ChunkDistanceEvaluator(_chunkManager);

        ServiceLocator.Instance.Register(this);
        _eventBus.Subscribe<PlayerChunkChangedEvent>(OnPlayerChunkChanged);

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
        _workerPool.Shutdown(_chunkManager.GetAll());
        _workerPool.Dispose();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        _resultDrainer.DrainResults(FrameBudgetMs);
        _nodeCleaner.CleanNodes(UnloadFrameBudgetMs);
        UpdatePerformanceMetrics();
    }

    /// <summary>
    /// Schedule an async remesh for a chunk after a block edit.
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

        if (!_workerPool.PendingRemeshes.TryAdd(coord, 0))
        {
            return;
        }

        _workerPool.BlockEditRemeshes.TryAdd(coord, 0);
        _workerPool.EnqueueBlockEditRemesh(entry, coord);
    }

    /// <summary>
    /// Enqueues a pre-snapshotted chunk save for background processing.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <param name="blockSnapshot">Pre-copied block array (ownership transferred).</param>
    public void EnqueueSave(ChunkCoord coord, ushort[] blockSnapshot)
    {
        _workerPool.EnqueueSave(coord, blockSnapshot);
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
        _distanceEvaluator.Evaluate(center, _renderDistance, _loadBuffer, _unloadBuffer);

        foreach (ChunkCoord coord in _unloadBuffer)
        {
            UnloadChunk(coord);
        }

        foreach (ChunkEntry entry in _loadBuffer)
        {
            _workerPool.EnqueueGeneration(entry);
        }
    }

    private void UnloadChunk(ChunkCoord coord)
    {
        _workerPool.CancelGeneration(coord);

        if (_chunkManager.TryGet(coord, out ChunkEntry? entry) && entry is not null)
        {
            entry.SetState(ChunkState.Unloading);

            if (entry.IsModified && _persistence is not null)
            {
                ushort[] snapshot = new ushort[ChunkData.TotalBlocks];
                entry.Data.CopyBlocksUnderReadLock(snapshot);
                entry.IsModified = false;
                _workerPool.EnqueueSave(coord, snapshot);
            }
        }

        _chunkManager.Remove(coord);

        if (_worldNode.TryExtractChunkNode(coord, out ChunkNode? node) && node is not null)
        {
            _nodeCleaner.Enqueue(coord, node);
        }
    }

    private void UpdatePerformanceMetrics()
    {
        if (_performanceMonitor is null)
        {
            return;
        }

        _performanceMonitor.SetChunksInQueue(_workerPool.PendingWorkCount);
        _performanceMonitor.SetActiveChunks(_chunkManager.Count);
        _performanceMonitor.SetVisibleChunks(_worldNode.ChunkNodeCount);

        ChunkNodePool pool = _worldNode.NodePool;
        _performanceMonitor.SetPoolStats(pool.IdleCount, pool.ActiveCount, pool.RecycleCount);
    }
}
