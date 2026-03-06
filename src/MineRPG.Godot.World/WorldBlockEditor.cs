using System.Collections.Generic;
using System.Threading;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Events;
using MineRPG.World.Meshing;
using MineRPG.World.Spatial;

using MineRPG.Godot.World.Chunks;

namespace MineRPG.Godot.World;

/// <summary>
/// Handles block modification API for the voxel world: breaking and placing blocks.
/// Manages dirty state, event publishing, and remesh scheduling.
/// </summary>
internal sealed class WorldBlockEditor
{
    private readonly IChunkManager _chunkManager;
    private readonly IChunkMeshBuilder _meshBuilder;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly Dictionary<ChunkCoord, ChunkNode> _chunkNodes;
    private readonly ChunkLoadingScheduler? _scheduler;

    /// <summary>
    /// Creates a block editor with all required dependencies.
    /// </summary>
    /// <param name="chunkManager">Chunk manager for chunk lookups.</param>
    /// <param name="meshBuilder">Mesh builder for sync remeshing fallback.</param>
    /// <param name="eventBus">Event bus for publishing block change events.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="chunkNodes">Active chunk node dictionary.</param>
    /// <param name="scheduler">Optional scheduler for async remeshing.</param>
    public WorldBlockEditor(
        IChunkManager chunkManager,
        IChunkMeshBuilder meshBuilder,
        IEventBus eventBus,
        ILogger logger,
        Dictionary<ChunkCoord, ChunkNode> chunkNodes,
        ChunkLoadingScheduler? scheduler)
    {
        _chunkManager = chunkManager;
        _meshBuilder = meshBuilder;
        _eventBus = eventBus;
        _logger = logger;
        _chunkNodes = chunkNodes;
        _scheduler = scheduler;
    }

    /// <summary>
    /// Breaks (removes) the block at the given world position.
    /// </summary>
    /// <param name="position">The world position of the block to break.</param>
    public void BreakBlock(WorldPosition position)
    {
        if (position.Y < 0 || position.Y >= ChunkData.SizeY)
        {
            return;
        }

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

        ScheduleOrSyncRemesh(coord, position.Y);
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

        if (position.Y < 0 || position.Y >= ChunkData.SizeY)
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

        ScheduleOrSyncRemesh(coord, position.Y);
        _logger.Debug("Block placed at {0}, blockId={1}", position, blockId);
    }

    private void ScheduleOrSyncRemesh(ChunkCoord coord, int worldY)
    {
        if (_scheduler is not null)
        {
            _scheduler.ScheduleBlockEditRemesh(coord);
            return;
        }

        if (!_chunkManager.TryGet(coord, out ChunkEntry? entry) || entry is null)
        {
            return;
        }

        ChunkData?[] neighbors = _chunkManager.GetNeighborData(coord);
        ChunkMeshResult mesh = _meshBuilder.Build(entry.Data, neighbors, CancellationToken.None);

        if (!_chunkNodes.TryGetValue(coord, out ChunkNode? chunkNode))
        {
            entry.SetState(ChunkState.Ready);
            return;
        }

        bool useIncremental = false;

        if (ServiceLocator.Instance.TryGet<OptimizationFlags>(out OptimizationFlags? flags)
            && flags is not null)
        {
            useIncremental = flags.IncrementalMeshingEnabled;
        }

        if (useIncremental)
        {
            int localY = worldY % ChunkData.SizeY;
            List<int> affectedSubChunks = new();
            IncrementalMeshUpdater.GetAffectedSubChunks(localY, affectedSubChunks);

            foreach (int subChunkIndex in affectedSubChunks)
            {
                chunkNode.ApplySubChunkMesh(subChunkIndex, mesh.SubChunks[subChunkIndex]);
            }
        }
        else
        {
            chunkNode.ApplyMesh(mesh);
        }

        entry.SetState(ChunkState.Ready);
    }
}
