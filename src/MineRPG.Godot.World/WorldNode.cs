using System;
using System.Collections.Generic;

using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Events;
using MineRPG.World.Meshing;
using MineRPG.World.Spatial;

namespace MineRPG.Godot.World;

/// <summary>
/// Root node for the voxel world. Owns ChunkNode instances.
/// Exposes block modification API for the player bridge to call.
/// Tracks player chunk position to trigger load/unload via events.
/// Uses a ChunkNodePool to recycle nodes instead of QueueFree.
/// </summary>
public sealed partial class WorldNode : Node3D
{
    private readonly Dictionary<ChunkCoord, ChunkNode> _chunkNodes = new();
    private readonly ChunkNodePool _chunkNodePool = new();

    private IChunkManager _chunkManager = null!;
    private IChunkMeshBuilder _meshBuilder = null!;
    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private ChunkLoadingScheduler? _scheduler;
    private ChunkCoord _lastKnownPlayerChunk = new(int.MinValue, int.MinValue);

    /// <summary>
    /// Gets the chunk node pool used for recycling chunk nodes.
    /// </summary>
    public ChunkNodePool NodePool => _chunkNodePool;

    /// <summary>
    /// Gets the number of active chunk nodes in the scene tree.
    /// </summary>
    public int ChunkNodeCount => _chunkNodes.Count;

    /// <inheritdoc />
    public override void _Ready()
    {
        _chunkManager = ServiceLocator.Instance.Get<IChunkManager>();
        _meshBuilder = ServiceLocator.Instance.Get<IChunkMeshBuilder>();
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        if (ServiceLocator.Instance.TryGet<ChunkLoadingScheduler>(out ChunkLoadingScheduler? scheduler))
        {
            _scheduler = scheduler;
        }

        _eventBus.Subscribe<PlayerPositionUpdatedEvent>(OnPlayerPositionUpdated);
    }

    /// <inheritdoc />
    public override void _ExitTree() => _eventBus.Unsubscribe<PlayerPositionUpdatedEvent>(OnPlayerPositionUpdated);

    /// <summary>
    /// Updates the player's chunk position based on their world coordinates.
    /// Publishes a <see cref="PlayerChunkChangedEvent"/> if the chunk changed.
    /// </summary>
    /// <param name="worldX">The player's X world coordinate.</param>
    /// <param name="worldZ">The player's Z world coordinate.</param>
    public void UpdatePlayerPosition(float worldX, float worldZ)
    {
        ChunkCoord2D chunkCoord = VoxelMath.WorldToChunk(
            (int)MathF.Floor(worldX), (int)MathF.Floor(worldZ),
            ChunkData.SizeX, ChunkData.SizeZ);
        ChunkCoord newChunk = new(chunkCoord.ChunkX, chunkCoord.ChunkZ);

        if (newChunk == _lastKnownPlayerChunk)
        {
            return;
        }

        _eventBus.Publish(new PlayerChunkChangedEvent
        {
            OldChunk = _lastKnownPlayerChunk,
            NewChunk = newChunk,
        });
        _lastKnownPlayerChunk = newChunk;
    }

    /// <summary>
    /// Gets or creates a chunk node for the given coordinate, adding it to the scene tree.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <returns>The existing or newly created chunk node.</returns>
    public ChunkNode GetOrCreateChunkNode(ChunkCoord coord)
    {
        if (_chunkNodes.TryGetValue(coord, out ChunkNode? existing))
        {
            return existing;
        }

        ChunkNode node = _chunkNodePool.Rent();
        node.Initialize(coord);
        node.Visible = true;
        AddChild(node);
        _chunkNodes[coord] = node;
        return node;
    }

    /// <summary>
    /// Checks whether a chunk node exists for the given coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate to check.</param>
    /// <returns>True if a chunk node exists for the coordinate.</returns>
    public bool HasChunkNode(ChunkCoord coord) => _chunkNodes.ContainsKey(coord);

    /// <summary>
    /// Returns all chunk nodes currently in the scene tree.
    /// </summary>
    /// <returns>An enumerable of all active chunk nodes.</returns>
    public IEnumerable<ChunkNode> GetChunkNodes() => _chunkNodes.Values;

    /// <summary>
    /// Removes and pools the chunk node for the given coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate to remove.</param>
    public void RemoveChunkNode(ChunkCoord coord)
    {
        if (!_chunkNodes.TryGetValue(coord, out ChunkNode? node))
        {
            return;
        }

        _chunkNodePool.Return(node);
        _chunkNodes.Remove(coord);
    }

    /// <summary>
    /// Breaks (removes) the block at the given world position.
    /// </summary>
    /// <param name="position">The world position of the block to break.</param>
    public void BreakBlock(WorldPosition position)
    {
        ChunkCoord2D chunkCoord2D = VoxelMath.WorldToChunk(position.X, position.Z, ChunkData.SizeX, ChunkData.SizeZ);
        ChunkCoord coord = new(chunkCoord2D.ChunkX, chunkCoord2D.ChunkZ);

        if (!_chunkManager.TryGet(coord, out ChunkEntry? entry) || entry is null)
        {
            return;
        }

        LocalCoord2D localCoord = VoxelMath.WorldToLocal(position.X, position.Z, ChunkData.SizeX, ChunkData.SizeZ);
        int localX = localCoord.LocalX;
        int localZ = localCoord.LocalZ;

        ushort oldBlockId;

        entry.Data.AcquireWriteLock();

        try
        {
            oldBlockId = entry.Data.GetBlock(localX, position.Y, localZ);

            if (oldBlockId == 0)
            {
                return;
            }

            entry.Data.SetBlock(localX, position.Y, localZ, 0);
        }
        finally
        {
            entry.Data.ReleaseWriteLock();
        }

        entry.SetState(ChunkState.Dirty);
        entry.IsModified = true;

        _eventBus.Publish(new BlockChangedEvent
        {
            Position = position,
            OldBlockId = oldBlockId,
            NewBlockId = 0,
        });

        ScheduleOrSyncRemesh(coord);
        _logger.Debug("Block broken at {0}", position);
    }

    /// <summary>
    /// Places a block at the given world position.
    /// </summary>
    /// <param name="position">The world position to place the block at.</param>
    /// <param name="blockId">The block type identifier to place.</param>
    public void PlaceBlock(WorldPosition position, ushort blockId)
    {
        if (blockId == 0)
        {
            return;
        }

        ChunkCoord2D chunkCoord2D = VoxelMath.WorldToChunk(position.X, position.Z, ChunkData.SizeX, ChunkData.SizeZ);
        ChunkCoord coord = new(chunkCoord2D.ChunkX, chunkCoord2D.ChunkZ);

        if (!_chunkManager.TryGet(coord, out ChunkEntry? entry) || entry is null)
        {
            _logger.Debug("PlaceBlock: chunk not loaded at {0}", position);
            return;
        }

        LocalCoord2D localCoord = VoxelMath.WorldToLocal(position.X, position.Z, ChunkData.SizeX, ChunkData.SizeZ);
        int localX = localCoord.LocalX;
        int localZ = localCoord.LocalZ;

        entry.Data.AcquireWriteLock();

        try
        {
            ushort existingBlock = entry.Data.GetBlock(localX, position.Y, localZ);

            if (existingBlock != 0)
            {
                _logger.Debug("PlaceBlock: position {0} already occupied (blockId={1})", position, existingBlock);
                return;
            }

            entry.Data.SetBlock(localX, position.Y, localZ, blockId);
        }
        finally
        {
            entry.Data.ReleaseWriteLock();
        }

        entry.SetState(ChunkState.Dirty);
        entry.IsModified = true;

        _eventBus.Publish(new BlockChangedEvent
        {
            Position = position,
            OldBlockId = 0,
            NewBlockId = blockId,
        });

        ScheduleOrSyncRemesh(coord);
        _logger.Debug("Block placed at {0}, blockId={1}", position, blockId);
    }

    private void OnPlayerPositionUpdated(PlayerPositionUpdatedEvent evt) => UpdatePlayerPosition(evt.X, evt.Z);

    private void ScheduleOrSyncRemesh(ChunkCoord coord)
    {
        if (_scheduler is not null)
        {
            _scheduler.ScheduleBlockEditRemesh(coord);
            return;
        }

        // Fallback: sync remesh when scheduler is unavailable
        if (!_chunkManager.TryGet(coord, out ChunkEntry? entry) || entry is null)
        {
            return;
        }

        ChunkData?[] neighbors = _chunkManager.GetNeighborData(coord);
        ChunkMeshResult mesh = _meshBuilder.Build(entry.Data, neighbors, CancellationToken.None);

        if (_chunkNodes.TryGetValue(coord, out ChunkNode? chunkNode))
        {
            chunkNode.ApplyMesh(mesh);
        }

        entry.SetState(ChunkState.Ready);
    }
}
