// =============================================================================
// STYLE GUIDE REFERENCE FILE
// This file is a non-compiled example that demonstrates every rule from
// STYLE_GUIDE.md. Use it as a template when creating new files.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using MineRPG.Core.Events;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Generation;
using MineRPG.World.Meshing;

namespace MineRPG.World.Chunks;

/// <summary>
/// Orchestrates asynchronous chunk loading, generation, and meshing around the player.
/// Uses a priority queue sorted by distance to the player so that nearby chunks load first.
/// Respects a per-frame mesh application budget to avoid stutters.
/// </summary>
public sealed class ChunkLoadingService
{
    // -------------------------------------------------------------------------
    // 1. Constants
    // -------------------------------------------------------------------------
    private const int DefaultRenderDistance = 12;
    private const int MaxMeshesPerFrame = 3;
    private const int MaxConcurrentJobs = 4;

    // -------------------------------------------------------------------------
    // 2. Static readonly fields
    // -------------------------------------------------------------------------
    private static readonly TimeSpan StaleJobTimeout = TimeSpan.FromSeconds(10);

    // -------------------------------------------------------------------------
    // 3. Readonly instance fields
    // -------------------------------------------------------------------------
    private readonly IChunkManager _chunkManager;
    private readonly IWorldGenerator _worldGenerator;
    private readonly IChunkMeshBuilder _meshBuilder;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly PriorityQueue<ChunkCoord, float> _loadQueue = new();
    private readonly HashSet<ChunkCoord> _pendingCoords = new();
    private readonly List<ChunkCoord> _coordBuffer = new();

    // -------------------------------------------------------------------------
    // 4. Mutable instance fields
    // -------------------------------------------------------------------------
    private int _renderDistance = DefaultRenderDistance;
    private int _activeMeshJobs;
    private CancellationTokenSource _cancellationTokenSource = new();

    // -------------------------------------------------------------------------
    // 5. Constructor
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a new <see cref="ChunkLoadingService"/> with all required dependencies.
    /// </summary>
    /// <param name="chunkManager">Central chunk storage.</param>
    /// <param name="worldGenerator">Generates raw chunk block data.</param>
    /// <param name="meshBuilder">Builds optimized mesh data from chunk blocks.</param>
    /// <param name="eventBus">Event bus for publishing chunk lifecycle events.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ChunkLoadingService(
        IChunkManager chunkManager,
        IWorldGenerator worldGenerator,
        IChunkMeshBuilder meshBuilder,
        IEventBus eventBus,
        ILogger logger)
    {
        _chunkManager = chunkManager ?? throw new ArgumentNullException(nameof(chunkManager));
        _worldGenerator = worldGenerator ?? throw new ArgumentNullException(nameof(worldGenerator));
        _meshBuilder = meshBuilder ?? throw new ArgumentNullException(nameof(meshBuilder));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // -------------------------------------------------------------------------
    // 6. Public properties
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets or sets the chunk render distance in chunk units (Chebyshev distance).
    /// </summary>
    public int RenderDistance
    {
        get => _renderDistance;
        set
        {
            if (value < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value), value, "Render distance must be at least 1.");
            }

            _renderDistance = value;
        }
    }

    /// <summary>
    /// Gets the number of chunks currently queued for loading.
    /// </summary>
    public int PendingCount => _pendingCoords.Count;

    /// <summary>
    /// Gets whether the service is actively processing chunk jobs.
    /// </summary>
    public bool IsProcessing => _activeMeshJobs > 0 || _loadQueue.Count > 0;

    // -------------------------------------------------------------------------
    // 7. Public methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Updates the loading queue based on the player's current chunk position.
    /// Call once per frame from the main loop.
    /// </summary>
    /// <param name="playerChunk">The chunk coordinate the player currently occupies.</param>
    public void UpdatePlayerPosition(ChunkCoord playerChunk)
    {
        _coordBuffer.Clear();
        CollectCoordsInRange(playerChunk, _renderDistance, _coordBuffer);

        for (int i = 0; i < _coordBuffer.Count; i++)
        {
            ChunkCoord coord = _coordBuffer[i];

            if (_pendingCoords.Contains(coord))
            {
                continue;
            }

            if (_chunkManager.TryGet(coord, out ChunkEntry? existing)
                && existing != null
                && existing.State >= ChunkState.Generated)
            {
                continue;
            }

            float distance = coord.ChebyshevDistance(playerChunk);
            _loadQueue.Enqueue(coord, distance);
            _pendingCoords.Add(coord);
        }

        UnloadDistantChunks(playerChunk);
    }

    /// <summary>
    /// Processes up to <see cref="MaxMeshesPerFrame"/> chunk jobs from the queue.
    /// Call once per frame after <see cref="UpdatePlayerPosition"/>.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel pending operations.</param>
    /// <returns>The number of chunks submitted for meshing this frame.</returns>
    public async Task<int> ProcessQueueAsync(CancellationToken cancellationToken)
    {
        int meshesSubmitted = 0;

        while (meshesSubmitted < MaxMeshesPerFrame
               && _activeMeshJobs < MaxConcurrentJobs
               && _loadQueue.TryDequeue(out ChunkCoord coord, out float _))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                Interlocked.Increment(ref _activeMeshJobs);
                // Generation and meshing run on background threads
                await GenerateAndMeshAsync(coord, cancellationToken);
                meshesSubmitted++;
            }
            catch (OperationCanceledException)
            {
                _logger.Debug("Chunk job cancelled for {0}", coord);
                break;
            }
            catch (Exception exception)
            {
                _logger.Error(
                    "Failed to generate/mesh chunk at {0}: {1}",
                    exception,
                    coord,
                    exception.Message);
            }
            finally
            {
                Interlocked.Decrement(ref _activeMeshJobs);
                _pendingCoords.Remove(coord);
            }
        }

        return meshesSubmitted;
    }

    /// <summary>
    /// Cancels all pending chunk jobs and clears the queue.
    /// </summary>
    public void CancelAll()
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        _loadQueue.Clear();
        _pendingCoords.Clear();

        _logger.Info("All chunk loading jobs cancelled");
    }

    // -------------------------------------------------------------------------
    // 8. Private methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Generates block data and builds the mesh for a single chunk on background threads.
    /// </summary>
    private async Task GenerateAndMeshAsync(ChunkCoord coord, CancellationToken cancellationToken)
    {
        ChunkEntry entry = _chunkManager.GetOrCreate(coord);

        // Generation on a background thread
        ChunkData chunkData = await Task.Run(
            () => _worldGenerator.Generate(entry, cancellationToken),
            cancellationToken);

        entry.SetData(chunkData);

        // Meshing on a background thread
        ChunkData?[] neighbors = _chunkManager.GetNeighborData(coord);
        MeshData meshData = await Task.Run(
            () => _meshBuilder.Build(chunkData, neighbors, cancellationToken),
            cancellationToken);

        entry.SetData(meshData);

        _eventBus.Publish(new ChunkReadyEvent { Coord = coord });
        _logger.Debug("Chunk ready at {0}", coord);
    }

    /// <summary>
    /// Collects all chunk coordinates within Chebyshev distance of center
    /// into the provided buffer. Avoids allocating a new list each frame.
    /// </summary>
    private static void CollectCoordsInRange(
        ChunkCoord center,
        int renderDistance,
        List<ChunkCoord> buffer)
    {
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                buffer.Add(new ChunkCoord(center.X + x, center.Z + z));
            }
        }
    }

    /// <summary>
    /// Removes chunks that are beyond the render distance from the player.
    /// </summary>
    private void UnloadDistantChunks(ChunkCoord playerChunk)
    {
        // Unload distance is slightly larger than render distance to prevent thrashing
        int unloadDistance = _renderDistance + 2;

        foreach (ChunkEntry entry in _chunkManager.GetAll())
        {
            float distance = entry.Coord.ChebyshevDistance(playerChunk);

            if (distance > unloadDistance)
            {
                _chunkManager.Remove(entry.Coord);
            }
        }
    }
}

// =============================================================================
// Supporting event struct — in production this lives in its own file.
// Shown here for completeness of the example.
// =============================================================================

/// <summary>
/// Published when a chunk has completed generation and meshing and is ready
/// for the Godot bridge layer to apply the mesh to the scene tree.
/// </summary>
public readonly struct ChunkReadyEvent
{
    /// <summary>
    /// Gets the coordinate of the chunk that is ready.
    /// </summary>
    public ChunkCoord Coord { get; init; }
}
