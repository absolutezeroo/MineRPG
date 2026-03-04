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
/// </summary>
public sealed partial class WorldNode : Node3D
{
	private readonly Dictionary<ChunkCoord, ChunkNode> _chunkNodes = new();
	private IChunkManager _chunkManager = null!;
	private IChunkMeshBuilder _meshBuilder = null!;
	private IEventBus _eventBus = null!;
	private ILogger _logger = null!;
	private ChunkLoadingScheduler? _scheduler;

	private ChunkCoord _lastKnownPlayerChunk = new(int.MinValue, int.MinValue);

	public override void _Ready()
	{
		_chunkManager = ServiceLocator.Instance.Get<IChunkManager>();
		_meshBuilder = ServiceLocator.Instance.Get<IChunkMeshBuilder>();
		_eventBus = ServiceLocator.Instance.Get<IEventBus>();
		_logger = ServiceLocator.Instance.Get<ILogger>();

		if (ServiceLocator.Instance.TryGet<ChunkLoadingScheduler>(out var scheduler))
			_scheduler = scheduler;

		_eventBus.Subscribe<PlayerPositionUpdatedEvent>(OnPlayerPositionUpdated);
	}

	public override void _ExitTree() => _eventBus.Unsubscribe<PlayerPositionUpdatedEvent>(OnPlayerPositionUpdated);

    private void OnPlayerPositionUpdated(PlayerPositionUpdatedEvent evt) => UpdatePlayerPosition(evt.X, evt.Z);

    public void UpdatePlayerPosition(float worldX, float worldZ)
	{
		var (cx, cz) = VoxelMath.WorldToChunk(
			(int)MathF.Floor(worldX), (int)MathF.Floor(worldZ),
			ChunkData.SizeX, ChunkData.SizeZ);
		var newChunk = new ChunkCoord(cx, cz);

		if (newChunk == _lastKnownPlayerChunk)
			return;

		_eventBus.Publish(new PlayerChunkChangedEvent
		{
			OldChunk = _lastKnownPlayerChunk,
			NewChunk = newChunk,
		});
		_lastKnownPlayerChunk = newChunk;
	}

	public ChunkNode GetOrCreateChunkNode(ChunkCoord coord)
	{
		if (_chunkNodes.TryGetValue(coord, out var existing))
			return existing;

		var node = new ChunkNode();
		node.Initialize(coord);
		AddChild(node);
		_chunkNodes[coord] = node;
		return node;
	}

	public bool HasChunkNode(ChunkCoord coord) => _chunkNodes.ContainsKey(coord);

	public void RemoveChunkNode(ChunkCoord coord)
	{
		if (!_chunkNodes.TryGetValue(coord, out var node))
			return;

		node.QueueFree();
		_chunkNodes.Remove(coord);
	}

	public void BreakBlock(WorldPosition pos)
	{
		var (cx, cz) = VoxelMath.WorldToChunk(pos.X, pos.Z, ChunkData.SizeX, ChunkData.SizeZ);
		var coord = new ChunkCoord(cx, cz);

		if (!_chunkManager.TryGet(coord, out var entry) || entry is null)
			return;

		var (lx, lz) = VoxelMath.WorldToLocal(pos.X, pos.Z, ChunkData.SizeX, ChunkData.SizeZ);
		var oldId = entry.Data.GetBlock(lx, pos.Y, lz);

		if (oldId == 0)
			return;

		entry.Data.SetBlock(lx, pos.Y, lz, 0);
		entry.SetState(ChunkState.Dirty);
		entry.IsModified = true;

		_eventBus.Publish(new BlockChangedEvent
		{
			Position = pos,
			OldBlockId = oldId,
			NewBlockId = 0,
		});

		ScheduleOrSyncRemesh(coord);
		_logger.Debug("Block broken at {0}", pos);
	}

	public void PlaceBlock(WorldPosition pos, ushort blockId)
	{
		if (blockId == 0)
			return;

		var (cx, cz) = VoxelMath.WorldToChunk(pos.X, pos.Z, ChunkData.SizeX, ChunkData.SizeZ);
		var coord = new ChunkCoord(cx, cz);

		if (!_chunkManager.TryGet(coord, out var entry) || entry is null)
			return;

		var (lx, lz) = VoxelMath.WorldToLocal(pos.X, pos.Z, ChunkData.SizeX, ChunkData.SizeZ);
		var oldId = entry.Data.GetBlock(lx, pos.Y, lz);

		if (oldId != 0)
			return;

		entry.Data.SetBlock(lx, pos.Y, lz, blockId);
		entry.SetState(ChunkState.Dirty);
		entry.IsModified = true;

		_eventBus.Publish(new BlockChangedEvent
		{
			Position = pos,
			OldBlockId = 0,
			NewBlockId = blockId,
		});

		ScheduleOrSyncRemesh(coord);
	}

	private void ScheduleOrSyncRemesh(ChunkCoord coord)
	{
		if (_scheduler is not null)
		{
			_scheduler.ScheduleBlockEditRemesh(coord);
			return;
		}

		// Fallback: sync remesh when scheduler is unavailable
		if (!_chunkManager.TryGet(coord, out var entry) || entry is null)
			return;

		var neighbors = _chunkManager.GetNeighborData(coord);
		var mesh = _meshBuilder.Build(entry.Data, neighbors);

		if (_chunkNodes.TryGetValue(coord, out var chunkNode))
			chunkNode.ApplyMesh(mesh);

		entry.SetState(ChunkState.Ready);
	}
}
