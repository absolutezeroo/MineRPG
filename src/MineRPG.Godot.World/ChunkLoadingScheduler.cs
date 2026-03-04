using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Godot;

using MineRPG.Core.DI;
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
/// Runs generate+mesh on Task threads. Applies results on the main thread.
/// Budget: processes at most <see cref="MaxChunksPerFrame"/> applications per _Process call.
/// Also handles chunk persistence: load from storage before generating,
/// save dirty chunks on unload, and autosave periodically.
/// </summary>
public sealed partial class ChunkLoadingScheduler : Node
{
    private const int MaxChunksPerFrame = 2;
    private const int RenderDistance = 8;
    private const float AutosaveIntervalSeconds = 60f;

    private readonly ConcurrentQueue<ChunkEntry> _readyQueue = new();
    private readonly ConcurrentDictionary<ChunkCoord, CancellationTokenSource> _pendingCts = new();
    private readonly ConcurrentDictionary<ChunkCoord, byte> _pendingRemeshes = new();

    private IChunkManager _chunkManager = null!;
    private IWorldGenerator _generator = null!;
    private IChunkMeshBuilder _meshBuilder = null!;
    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private WorldNode _worldNode = null!;
    private ChunkPersistenceService? _persistence;
    private float _autosaveTimer;

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

        SaveAllDirtyChunks();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        int applied = 0;

        while (applied < MaxChunksPerFrame && _readyQueue.TryDequeue(out ChunkEntry? entry))
        {
            ApplyChunkMesh(entry);
            applied++;
        }

        _autosaveTimer += (float)delta;

        if (_autosaveTimer >= AutosaveIntervalSeconds)
        {
            _autosaveTimer = 0f;
            SaveAllDirtyChunks();
        }
    }

    /// <summary>
    /// Schedule an async remesh for a chunk after a block edit.
    /// Uses the same dedup + ready-queue pattern as neighbor remeshes.
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

        // Dedup: skip if a remesh is already pending
        if (!_pendingRemeshes.TryAdd(coord, 0))
        {
            return;
        }

        ChunkEntry capturedEntry = entry;
        ChunkCoord capturedCoord = coord;

        Task.Run(() =>
        {
            try
            {
                ChunkNeighborData neighbors = _chunkManager.GetNeighborData(capturedCoord);
                ChunkMeshResult mesh = _meshBuilder.Build(capturedEntry.Data, neighbors);
                capturedEntry.PendingMesh = mesh;
                capturedEntry.SetState(ChunkState.Ready);
                _readyQueue.Enqueue(capturedEntry);
            }
            catch (Exception exception)
            {
                _pendingRemeshes.TryRemove(capturedCoord, out _);
                _logger.Error("Block edit remesh failed for {0}: {1}", exception, capturedCoord, exception.Message);
            }
        });
    }

    /// <summary>
    /// Forces an immediate load of chunks around the given center coordinate.
    /// </summary>
    /// <param name="center">The center chunk coordinate.</param>
    public void ForceLoadAround(ChunkCoord center) => UpdateLoadedChunks(center);

    private void OnPlayerChunkChanged(PlayerChunkChangedEvent evt) => UpdateLoadedChunks(evt.NewChunk);

    private void UpdateLoadedChunks(ChunkCoord center)
    {
        List<ChunkCoord> needed = _chunkManager.GetCoordsInRange(center, RenderDistance);
        HashSet<ChunkCoord> neededSet = new(needed);

        foreach (ChunkEntry entry in _chunkManager.GetAll().ToList())
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

        Task.Run(() =>
        {
            try
            {
                // Try loading from storage first
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
                entry.RecomputeSubChunkInfo();
                entry.SetState(ChunkState.Meshing);

                ChunkNeighborData neighbors = _chunkManager.GetNeighborData(entry.Coord);
                ChunkMeshResult mesh = _meshBuilder.Build(entry.Data, neighbors);

                if (cts.Token.IsCancellationRequested)
                {
                    return;
                }

                entry.PendingMesh = mesh;
                entry.SetState(ChunkState.Ready);
                _readyQueue.Enqueue(entry);
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
            }
        }, cts.Token);
    }

    private void ApplyChunkMesh(ChunkEntry entry)
    {
        if (entry.PendingMesh is null)
        {
            return;
        }

        bool isRemesh = _pendingRemeshes.TryRemove(entry.Coord, out _);

        ChunkNode chunkNode = _worldNode.GetOrCreateChunkNode(entry.Coord);
        chunkNode.ApplyMesh(entry.PendingMesh!);
        entry.PendingMesh = null;

        _eventBus.Publish(new ChunkMeshedEvent { Coord = entry.Coord });

        // Only schedule neighbor remeshes for initial meshes, not for remeshes
        if (!isRemesh)
        {
            ScheduleNeighborRemeshes(entry.Coord);
        }
    }

    private void ScheduleNeighborRemeshes(ChunkCoord coord)
    {
        ChunkCoord[] neighborCoords = [coord.East, coord.West, coord.South, coord.North];

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

            // Skip if a remesh is already pending for this neighbor
            if (!_pendingRemeshes.TryAdd(neighborCoord, 0))
            {
                continue;
            }

            ChunkEntry capturedNeighbor = neighbor;
            ChunkCoord capturedCoord = neighborCoord;

            Task.Run(() =>
            {
                try
                {
                    ChunkNeighborData neighbors = _chunkManager.GetNeighborData(capturedCoord);
                    ChunkMeshResult mesh = _meshBuilder.Build(capturedNeighbor.Data, neighbors);
                    capturedNeighbor.PendingMesh = mesh;
                    _readyQueue.Enqueue(capturedNeighbor);
                }
                catch (Exception exception)
                {
                    _pendingRemeshes.TryRemove(capturedCoord, out _);
                    _logger.Error("Neighbor remesh failed for {0}: {1}", exception, capturedCoord, exception.Message);
                }
            });
        }
    }

    private void UnloadChunk(ChunkCoord coord)
    {
        if (_pendingCts.TryRemove(coord, out CancellationTokenSource? cts))
        {
            cts.Cancel();
        }

        // Save dirty chunk before unloading
        if (_persistence is not null && _chunkManager.TryGet(coord, out ChunkEntry? entry) && entry is not null)
        {
            _persistence.SaveIfModified(entry);
        }

        _chunkManager.Remove(coord);
        _worldNode.RemoveChunkNode(coord);
    }

    private void SaveAllDirtyChunks()
    {
        if (_persistence is null)
        {
            return;
        }

        int saved = 0;

        foreach (ChunkEntry entry in _chunkManager.GetAll())
        {
            if (_persistence.SaveIfModified(entry))
            {
                saved++;
            }
        }

        if (saved > 0)
        {
            _logger.Info("Autosave: Saved {0} modified chunks.", saved);
        }
    }
}
