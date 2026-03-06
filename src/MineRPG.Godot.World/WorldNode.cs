using System.Collections.Generic;

using Godot;

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
#if DEBUG
using MineRPG.Godot.World.Debug;
#endif

namespace MineRPG.Godot.World;

/// <summary>
/// Root node for the voxel world. Owns ChunkNode instances.
/// Delegates block modification to <see cref="WorldBlockEditor"/>.
/// Tracks player chunk position to trigger load/unload via events.
/// Uses a ChunkNodePool to recycle nodes instead of QueueFree.
/// </summary>
public sealed partial class WorldNode : Node3D
{
    private readonly Dictionary<ChunkCoord, ChunkNode> _chunkNodes = new();

    private IChunkManager _chunkManager = null!;
    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private ChunkLoadingScheduler? _scheduler;
    private WorldBlockEditor _blockEditor = null!;
    private ChunkCoord _lastKnownPlayerChunk = new(int.MinValue, int.MinValue);
    private FrustumCullingSystem? _frustumCulling;
    private OcclusionCuller? _occlusionCuller;

#if DEBUG
    private ChunkBorderRenderer? _chunkBorderRenderer;
#endif

    /// <summary>Gets the chunk node pool used for recycling chunk nodes.</summary>
    public ChunkNodePool NodePool { get; } = new();

    /// <summary>Gets the number of active chunk nodes in the scene tree.</summary>
    public int ChunkNodeCount => _chunkNodes.Count;

    /// <inheritdoc />
    public override void _Ready()
    {
        _chunkManager = ServiceLocator.Instance.Get<IChunkManager>();
        IChunkMeshBuilder meshBuilder = ServiceLocator.Instance.Get<IChunkMeshBuilder>();
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        if (ServiceLocator.Instance.TryGet<ChunkLoadingScheduler>(out ChunkLoadingScheduler? scheduler))
        {
            _scheduler = scheduler;
        }

        _blockEditor = new WorldBlockEditor(
            _chunkManager, meshBuilder, _eventBus, _logger, _chunkNodes, _scheduler);

        _eventBus.Subscribe<PlayerPositionUpdatedEvent>(OnPlayerPositionUpdated);

        MiningOverlayNode miningOverlay = new();
        AddChild(miningOverlay);

#if DEBUG
        _eventBus.Subscribe<DebugToggleEvent>(OnDebugToggle);
#endif
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (_eventBus is null)
        {
            return;
        }

        _eventBus.Unsubscribe<PlayerPositionUpdatedEvent>(OnPlayerPositionUpdated);
#if DEBUG
        _eventBus.Unsubscribe<DebugToggleEvent>(OnDebugToggle);
#endif

        foreach (ChunkNode node in _chunkNodes.Values)
        {
            node.ClearMesh();
            node.QueueFree();
        }

        _chunkNodes.Clear();
        NodePool.FreeAll();
    }

    /// <summary>
    /// Updates the player's chunk position based on their world coordinates.
    /// Publishes a <see cref="PlayerChunkChangedEvent"/> if the chunk changed.
    /// </summary>
    /// <param name="worldX">The player's X world coordinate.</param>
    /// <param name="worldZ">The player's Z world coordinate.</param>
    public void UpdatePlayerPosition(float worldX, float worldZ)
    {
        ChunkCoord2D chunkCoord = VoxelMath.WorldToChunk(
            (int)System.MathF.Floor(worldX), (int)System.MathF.Floor(worldZ),
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
    /// Gets or creates a chunk node for the given coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <returns>The existing or newly created chunk node.</returns>
    public ChunkNode GetOrCreateChunkNode(ChunkCoord coord)
    {
        if (_chunkNodes.TryGetValue(coord, out ChunkNode? existing))
        {
            return existing;
        }

        ChunkNode node = NodePool.Rent();
        node.Initialize(coord);
        node.Visible = true;
        AddChild(node);
        _chunkNodes[coord] = node;
        _frustumCulling?.Invalidate();
        return node;
    }

    /// <summary>
    /// Checks whether a chunk node exists for the given coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate to check.</param>
    /// <returns>True if a chunk node exists for the coordinate.</returns>
    public bool HasChunkNode(ChunkCoord coord) => _chunkNodes.ContainsKey(coord);

    /// <summary>Returns all chunk nodes currently in the scene tree.</summary>
    /// <returns>An enumerable of all active chunk nodes.</returns>
    public IEnumerable<ChunkNode> GetChunkNodes() => _chunkNodes.Values;

    /// <summary>Removes and pools the chunk node for the given coordinate.</summary>
    /// <param name="coord">The chunk coordinate to remove.</param>
    public void RemoveChunkNode(ChunkCoord coord)
    {
        if (!_chunkNodes.TryGetValue(coord, out ChunkNode? node))
        {
            return;
        }

        NodePool.Return(node);
        _chunkNodes.Remove(coord);
        _frustumCulling?.Invalidate();
        _occlusionCuller?.RemoveMatrix(coord);
    }

    /// <summary>
    /// Extracts the chunk node without returning it to the pool.
    /// The caller is responsible for deferred pool return.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <param name="node">The extracted node, or null if not found.</param>
    /// <returns>True if a node was found and extracted.</returns>
    public bool TryExtractChunkNode(ChunkCoord coord, out ChunkNode? node)
    {
        if (!_chunkNodes.TryGetValue(coord, out node))
        {
            return false;
        }

        _chunkNodes.Remove(coord);
        return true;
    }

    /// <summary>Returns a previously extracted chunk node to the pool.</summary>
    /// <param name="node">The node to return.</param>
    public void ReturnChunkNodeToPool(ChunkNode node) => NodePool.Return(node);

    /// <summary>
    /// Sets the frustum culling system for invalidation on chunk changes.
    /// </summary>
    /// <param name="system">The frustum culling system instance.</param>
    public void SetFrustumCulling(FrustumCullingSystem system) => _frustumCulling = system;

    /// <summary>
    /// Sets the occlusion culler for visibility matrix cleanup on chunk removal.
    /// </summary>
    /// <param name="occlusionCuller">The occlusion culler instance.</param>
    public void SetOcclusionCuller(OcclusionCuller occlusionCuller) => _occlusionCuller = occlusionCuller;

    /// <summary>Breaks (removes) the block at the given world position.</summary>
    /// <param name="position">The world position of the block to break.</param>
    public void BreakBlock(WorldPosition position) => _blockEditor.BreakBlock(position);

    /// <summary>Places a block at the given world position.</summary>
    /// <param name="position">The world position to place the block at.</param>
    /// <param name="blockId">The block type identifier to place.</param>
    public void PlaceBlock(WorldPosition position, ushort blockId) => _blockEditor.PlaceBlock(position, blockId);

    private void OnPlayerPositionUpdated(PlayerPositionUpdatedEvent evt)
    {
        UpdatePlayerPosition(evt.X, evt.Z);

        if (ServiceLocator.Instance.TryGet<ClipmapRenderer>(out ClipmapRenderer? clipmap)
            && clipmap is not null)
        {
            clipmap.UpdatePlayerPosition(evt.X, evt.Z);
        }
    }

#if DEBUG
    private void OnDebugToggle(DebugToggleEvent evt)
    {
        if (evt.ModuleKey != "chunk_border")
        {
            return;
        }

        if (_chunkBorderRenderer is null)
        {
            _chunkBorderRenderer = new ChunkBorderRenderer();
            _chunkBorderRenderer.Name = "ChunkBorderRenderer";
            _chunkBorderRenderer.Visible = true;
            AddChild(_chunkBorderRenderer);
            _logger.Debug("WorldNode: ChunkBorderRenderer created.");
            return;
        }

        _chunkBorderRenderer.Visible = evt.Visible;
    }
#endif
}
