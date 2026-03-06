using System.Diagnostics;
using System.Threading;

namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Collects performance metrics for the debug overlay.
/// Thread-safe: counters use Interlocked, timing uses Stopwatch.
/// No Godot dependency - lives in Core.
/// </summary>
public sealed class PerformanceMonitor
{
    private const double MillisecondsPerSecond = 1000.0;

    private long _chunksGenerated;
    private long _chunksMeshed;
    private long _chunksInQueue;
    private long _activeChunks;
    private long _visibleChunks;
    private long _totalVertices;
    private long _meshTimeAccumulatorTicks;
    private long _meshTimeCount;
    private long _poolIdleCount;
    private long _poolActiveCount;
    private long _poolRecycleCount;
    private int _renderDistance;

    /// <summary>
    /// Total number of chunks generated since startup.
    /// </summary>
    public long ChunksGenerated => Interlocked.Read(ref _chunksGenerated);

    /// <summary>
    /// Total number of chunks meshed since startup.
    /// </summary>
    public long ChunksMeshed => Interlocked.Read(ref _chunksMeshed);

    /// <summary>
    /// Number of chunks currently waiting in the generation/meshing queue.
    /// </summary>
    public long ChunksInQueue => Interlocked.Read(ref _chunksInQueue);

    /// <summary>
    /// Number of chunks currently loaded in memory.
    /// </summary>
    public long ActiveChunks => Interlocked.Read(ref _activeChunks);

    /// <summary>
    /// Number of chunks currently visible after frustum culling.
    /// </summary>
    public long VisibleChunks => Interlocked.Read(ref _visibleChunks);

    /// <summary>
    /// Total vertex count across all loaded chunk meshes.
    /// </summary>
    public long TotalVertices => Interlocked.Read(ref _totalVertices);

    /// <summary>
    /// Current render distance in chunks.
    /// </summary>
    public int RenderDistance => Interlocked.CompareExchange(ref _renderDistance, 0, 0);

    /// <summary>
    /// Number of idle (recycled) objects in the pool.
    /// </summary>
    public long PoolIdleCount => Interlocked.Read(ref _poolIdleCount);

    /// <summary>
    /// Number of active (in-use) objects from the pool.
    /// </summary>
    public long PoolActiveCount => Interlocked.Read(ref _poolActiveCount);

    /// <summary>
    /// Total number of objects recycled through the pool.
    /// </summary>
    public long PoolRecycleCount => Interlocked.Read(ref _poolRecycleCount);

    /// <summary>
    /// Average mesh time in milliseconds. Returns 0 if no meshes recorded.
    /// </summary>
    public double AverageMeshTimeMs
    {
        get
        {
            long count = Interlocked.Read(ref _meshTimeCount);

            if (count == 0)
            {
                return 0;
            }

            long ticks = Interlocked.Read(ref _meshTimeAccumulatorTicks);
            return (double)ticks / count / Stopwatch.Frequency * MillisecondsPerSecond;
        }
    }

    /// <summary>
    /// Increment the chunks generated counter by one.
    /// </summary>
    public void IncrementChunksGenerated() => Interlocked.Increment(ref _chunksGenerated);

    /// <summary>
    /// Increment the chunks meshed counter by one.
    /// </summary>
    public void IncrementChunksMeshed() => Interlocked.Increment(ref _chunksMeshed);

    /// <summary>
    /// Set the current number of chunks in the generation/meshing queue.
    /// </summary>
    /// <param name="count">The current queue depth.</param>
    public void SetChunksInQueue(long count) => Interlocked.Exchange(ref _chunksInQueue, count);

    /// <summary>
    /// Set the current number of active (loaded) chunks.
    /// </summary>
    /// <param name="count">The number of active chunks.</param>
    public void SetActiveChunks(long count) => Interlocked.Exchange(ref _activeChunks, count);

    /// <summary>
    /// Set the current number of visible chunks.
    /// </summary>
    /// <param name="count">The number of visible chunks.</param>
    public void SetVisibleChunks(long count) => Interlocked.Exchange(ref _visibleChunks, count);

    /// <summary>
    /// Set the total vertex count across all loaded chunk meshes.
    /// </summary>
    /// <param name="count">The total vertex count.</param>
    public void SetTotalVertices(long count) => Interlocked.Exchange(ref _totalVertices, count);

    /// <summary>
    /// Set the current render distance in chunks.
    /// </summary>
    /// <param name="distance">The render distance.</param>
    public void SetRenderDistance(int distance) => Interlocked.Exchange(ref _renderDistance, distance);

    /// <summary>
    /// Set all pool statistics at once.
    /// </summary>
    /// <param name="idle">Number of idle objects.</param>
    /// <param name="active">Number of active objects.</param>
    /// <param name="recycled">Total number of recycled objects.</param>
    public void SetPoolStats(long idle, long active, long recycled)
    {
        Interlocked.Exchange(ref _poolIdleCount, idle);
        Interlocked.Exchange(ref _poolActiveCount, active);
        Interlocked.Exchange(ref _poolRecycleCount, recycled);
    }

    /// <summary>
    /// Record a mesh build duration for averaging.
    /// </summary>
    /// <param name="elapsedTicks">The elapsed time in stopwatch ticks.</param>
    public void RecordMeshTime(long elapsedTicks)
    {
        Interlocked.Add(ref _meshTimeAccumulatorTicks, elapsedTicks);
        Interlocked.Increment(ref _meshTimeCount);
    }

    /// <summary>
    /// Reset rolling averages. Call periodically (e.g., every few seconds).
    /// </summary>
    /// <remarks>
    /// Best-effort reset: the two exchanges are not atomic relative to each other.
    /// A concurrent <see cref="RecordMeshTime"/> between them may leave a small residual
    /// in the accumulator, which self-corrects on the next reset cycle.
    /// Count is reset first so <see cref="AverageMeshTimeMs"/> returns 0 during the window.
    /// </remarks>
    public void ResetAverages()
    {
        Interlocked.Exchange(ref _meshTimeCount, 0);
        Interlocked.Exchange(ref _meshTimeAccumulatorTicks, 0);
    }
}
