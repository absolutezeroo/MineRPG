using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Chunks.Serialization;
using MineRPG.World.Generation;
using MineRPG.World.Meshing;
using MineRPG.World.Spatial;

namespace MineRPG.Godot.World.Pipeline;

/// <summary>
/// Processes chunk generation work items: loads from persistence or generates
/// new terrain, meshes the result, and enqueues it for the main thread.
/// Supports LOD downsampling and vertex packing when optimization flags are enabled.
/// </summary>
internal sealed class GenerationWorkProcessor
{
    private readonly IChunkManager _chunkManager;
    private readonly IWorldGenerator _generator;
    private readonly IChunkMeshBuilder _meshBuilder;
    private readonly ILogger _logger;
    private readonly ChunkPersistenceService? _persistence;
    private readonly PerformanceMonitor? _performanceMonitor;
    private readonly OptimizationFlags? _optimizationFlags;
    private readonly ConcurrentQueue<ChunkEntry> _loadResultQueue;
    private readonly ConcurrentDictionary<ChunkCoord, CancellationTokenSource> _pendingCts;

    private volatile int _playerChunkX;
    private volatile int _playerChunkZ;

    /// <summary>
    /// Creates a generation work processor.
    /// </summary>
    /// <param name="chunkManager">Chunk manager for neighbor lookups.</param>
    /// <param name="generator">World generator for terrain creation.</param>
    /// <param name="meshBuilder">Mesh builder for chunk meshing.</param>
    /// <param name="logger">Logger for error reporting.</param>
    /// <param name="persistence">Optional persistence for loading saved chunks.</param>
    /// <param name="performanceMonitor">Optional performance metrics recorder.</param>
    /// <param name="optimizationFlags">Optional optimization flags for LOD and packing.</param>
    /// <param name="loadResultQueue">Queue to deliver completed entries.</param>
    /// <param name="pendingCts">Shared pending cancellation token dictionary.</param>
    public GenerationWorkProcessor(
        IChunkManager chunkManager,
        IWorldGenerator generator,
        IChunkMeshBuilder meshBuilder,
        ILogger logger,
        ChunkPersistenceService? persistence,
        PerformanceMonitor? performanceMonitor,
        OptimizationFlags? optimizationFlags,
        ConcurrentQueue<ChunkEntry> loadResultQueue,
        ConcurrentDictionary<ChunkCoord, CancellationTokenSource> pendingCts)
    {
        _chunkManager = chunkManager;
        _generator = generator;
        _meshBuilder = meshBuilder;
        _logger = logger;
        _persistence = persistence;
        _performanceMonitor = performanceMonitor;
        _optimizationFlags = optimizationFlags;
        _loadResultQueue = loadResultQueue;
        _pendingCts = pendingCts;
    }

    /// <summary>
    /// Updates the known player chunk position used for LOD distance calculations.
    /// Called from the main thread before enqueueing generation work.
    /// </summary>
    /// <param name="chunkX">The player's current chunk X coordinate.</param>
    /// <param name="chunkZ">The player's current chunk Z coordinate.</param>
    public void UpdatePlayerChunk(int chunkX, int chunkZ)
    {
        _playerChunkX = chunkX;
        _playerChunkZ = chunkZ;
    }

    /// <summary>
    /// Processes a single generation work item. Loads from persistence or generates,
    /// then meshes and enqueues the result. Applies LOD and packing if enabled.
    /// </summary>
    /// <param name="entry">The chunk entry to generate.</param>
    public void Process(ChunkEntry entry)
    {
        _pendingCts.TryGetValue(entry.Coord, out CancellationTokenSource? cts);
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
            entry.VisibilityMatrix = VisibilityMatrixBuilder.Build(entry.Data, entry.SubChunks);
            entry.SetState(ChunkState.Meshing);

            // Determine LOD level based on distance (read flag once to avoid tearing)
            bool lodEnabled = _optimizationFlags is null || _optimizationFlags.LodEnabled;
            ChunkData meshSourceData = entry.Data;
            int lodFactor = 1;

            if (lodEnabled)
            {
                ChunkCoord playerChunk = new(_playerChunkX, _playerChunkZ);
                int distance = entry.Coord.ChebyshevDistance(playerChunk);
                entry.CurrentLod = LodPolicy.GetInitialLod(distance);

                if (entry.CurrentLod != LodLevel.Lod0)
                {
                    lodFactor = LodPolicy.GetDownsampleFactor(entry.CurrentLod);
                    ushort[] downsampledBuffer = ArrayPool<ushort>.Shared.Rent(
                        ChunkDownsampler.GetOutputSize(lodFactor));

                    try
                    {
                        ChunkDownsampler.Downsample(
                            entry.Data, lodFactor, downsampledBuffer,
                            out int outSizeX, out int outSizeY, out int outSizeZ);

                        meshSourceData = ChunkDownsampler.Expand(
                            entry.Coord, downsampledBuffer, outSizeX, outSizeY, outSizeZ, lodFactor);
                    }
                    finally
                    {
                        ArrayPool<ushort>.Shared.Return(downsampledBuffer);
                    }
                }
            }

            long meshStart = Stopwatch.GetTimestamp();
            ChunkData?[] neighbors = _chunkManager.GetNeighborData(entry.Coord);
            ChunkMeshResult mesh = _meshBuilder.Build(meshSourceData, neighbors, token);
            _performanceMonitor?.RecordMeshTime(Stopwatch.GetTimestamp() - meshStart);
            _performanceMonitor?.IncrementChunksMeshed();

            if (token.IsCancellationRequested)
            {
                return;
            }

            // Scale vertex positions for LOD meshes
            if (entry.CurrentLod != LodLevel.Lod0)
            {
                mesh = MeshScaler.ScaleResult(mesh, lodFactor);
            }

            // Pack vertices for memory-efficient transport
            if (_optimizationFlags is null || _optimizationFlags.VertexPackingEnabled)
            {
                mesh = MeshPackHelper.PackResult(mesh);
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
            if (_pendingCts.TryRemove(entry.Coord, out CancellationTokenSource? removedCts))
            {
                removedCts.Dispose();
            }
        }
    }

}
