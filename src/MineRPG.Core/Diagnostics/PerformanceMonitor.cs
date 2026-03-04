using System.Diagnostics;

namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Collects performance metrics for the debug overlay.
/// Thread-safe: counters use Interlocked, timing uses Stopwatch.
/// No Godot dependency — lives in Core.
/// </summary>
public sealed class PerformanceMonitor
{
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

    public long ChunksGenerated => Interlocked.Read(ref _chunksGenerated);
    public long ChunksMeshed => Interlocked.Read(ref _chunksMeshed);
    public long ChunksInQueue => Interlocked.Read(ref _chunksInQueue);
    public long ActiveChunks => Interlocked.Read(ref _activeChunks);
    public long VisibleChunks => Interlocked.Read(ref _visibleChunks);
    public long TotalVertices => Interlocked.Read(ref _totalVertices);
    public int RenderDistance => _renderDistance;

    public long PoolIdleCount => Interlocked.Read(ref _poolIdleCount);
    public long PoolActiveCount => Interlocked.Read(ref _poolActiveCount);
    public long PoolRecycleCount => Interlocked.Read(ref _poolRecycleCount);

    /// <summary>
    /// Average mesh time in milliseconds. Returns 0 if no meshes recorded.
    /// </summary>
    public double AverageMeshTimeMs
    {
        get
        {
            var count = Interlocked.Read(ref _meshTimeCount);
            if (count == 0)
                return 0;

            var ticks = Interlocked.Read(ref _meshTimeAccumulatorTicks);
            return (double)ticks / count / Stopwatch.Frequency * 1000.0;
        }
    }

    public void IncrementChunksGenerated() => Interlocked.Increment(ref _chunksGenerated);
    public void IncrementChunksMeshed() => Interlocked.Increment(ref _chunksMeshed);

    public void SetChunksInQueue(long count) => Interlocked.Exchange(ref _chunksInQueue, count);
    public void SetActiveChunks(long count) => Interlocked.Exchange(ref _activeChunks, count);
    public void SetVisibleChunks(long count) => Interlocked.Exchange(ref _visibleChunks, count);
    public void SetTotalVertices(long count) => Interlocked.Exchange(ref _totalVertices, count);
    public void SetRenderDistance(int distance) => _renderDistance = distance;

    public void SetPoolStats(long idle, long active, long recycled)
    {
        Interlocked.Exchange(ref _poolIdleCount, idle);
        Interlocked.Exchange(ref _poolActiveCount, active);
        Interlocked.Exchange(ref _poolRecycleCount, recycled);
    }

    /// <summary>
    /// Record a mesh build duration for averaging.
    /// </summary>
    public void RecordMeshTime(long elapsedTicks)
    {
        Interlocked.Add(ref _meshTimeAccumulatorTicks, elapsedTicks);
        Interlocked.Increment(ref _meshTimeCount);
    }

    /// <summary>
    /// Reset rolling averages. Call periodically (e.g., every few seconds).
    /// </summary>
    public void ResetAverages()
    {
        Interlocked.Exchange(ref _meshTimeAccumulatorTicks, 0);
        Interlocked.Exchange(ref _meshTimeCount, 0);
    }
}
