using System.Threading;

namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Extended pipeline metrics beyond <see cref="PerformanceMonitor"/>.
/// Tracks per-queue depths, worker counts, and per-operation average times.
/// Thread-safe via Interlocked.
/// </summary>
public sealed class PipelineMetrics
{
    private long _generationQueueSize;
    private long _remeshQueueSize;
    private long _saveQueueSize;
    private long _blockEditQueueSize;
    private long _activeWorkerCount;
    private long _totalWorkerCount;
    private long _averageGenerationTimeTicks;
    private long _generationTimeCount;
    private long _averageSaveTimeTicks;
    private long _saveTimeCount;
    private long _drainTimeAccumulatorTicks;
    private long _drainTimeCount;

    /// <summary>Number of chunks waiting for generation.</summary>
    public long GenerationQueueSize => Interlocked.Read(ref _generationQueueSize);

    /// <summary>Number of chunks waiting for remeshing.</summary>
    public long RemeshQueueSize => Interlocked.Read(ref _remeshQueueSize);

    /// <summary>Number of chunks waiting to be saved.</summary>
    public long SaveQueueSize => Interlocked.Read(ref _saveQueueSize);

    /// <summary>Number of block edits waiting to be processed.</summary>
    public long BlockEditQueueSize => Interlocked.Read(ref _blockEditQueueSize);

    /// <summary>Number of currently active worker threads.</summary>
    public long ActiveWorkerCount => Interlocked.Read(ref _activeWorkerCount);

    /// <summary>Total number of worker threads.</summary>
    public long TotalWorkerCount => Interlocked.Read(ref _totalWorkerCount);

    /// <summary>
    /// Average generation time in milliseconds.
    /// </summary>
    public double AverageGenerationTimeMs
    {
        get
        {
            long count = Interlocked.Read(ref _generationTimeCount);

            if (count == 0)
            {
                return 0;
            }

            long ticks = Interlocked.Read(ref _averageGenerationTimeTicks);
            return (double)ticks / count / System.Diagnostics.Stopwatch.Frequency * 1000.0;
        }
    }

    /// <summary>
    /// Average save time in milliseconds.
    /// </summary>
    public double AverageSaveTimeMs
    {
        get
        {
            long count = Interlocked.Read(ref _saveTimeCount);

            if (count == 0)
            {
                return 0;
            }

            long ticks = Interlocked.Read(ref _averageSaveTimeTicks);
            return (double)ticks / count / System.Diagnostics.Stopwatch.Frequency * 1000.0;
        }
    }

    /// <summary>
    /// Average drain time in milliseconds (time spent applying meshes per frame).
    /// </summary>
    public double AverageDrainTimeMs
    {
        get
        {
            long count = Interlocked.Read(ref _drainTimeCount);

            if (count == 0)
            {
                return 0;
            }

            long ticks = Interlocked.Read(ref _drainTimeAccumulatorTicks);
            return (double)ticks / count / System.Diagnostics.Stopwatch.Frequency * 1000.0;
        }
    }

    /// <summary>Sets the generation queue size.</summary>
    public void SetGenerationQueueSize(long size) => Interlocked.Exchange(ref _generationQueueSize, size);

    /// <summary>Sets the remesh queue size.</summary>
    public void SetRemeshQueueSize(long size) => Interlocked.Exchange(ref _remeshQueueSize, size);

    /// <summary>Sets the save queue size.</summary>
    public void SetSaveQueueSize(long size) => Interlocked.Exchange(ref _saveQueueSize, size);

    /// <summary>Sets the block edit queue size.</summary>
    public void SetBlockEditQueueSize(long size) => Interlocked.Exchange(ref _blockEditQueueSize, size);

    /// <summary>Sets the active and total worker counts.</summary>
    public void SetWorkerCounts(long active, long total)
    {
        Interlocked.Exchange(ref _activeWorkerCount, active);
        Interlocked.Exchange(ref _totalWorkerCount, total);
    }

    /// <summary>Records a generation duration for averaging.</summary>
    public void RecordGenerationTime(long elapsedTicks)
    {
        Interlocked.Add(ref _averageGenerationTimeTicks, elapsedTicks);
        Interlocked.Increment(ref _generationTimeCount);
    }

    /// <summary>Records a save duration for averaging.</summary>
    public void RecordSaveTime(long elapsedTicks)
    {
        Interlocked.Add(ref _averageSaveTimeTicks, elapsedTicks);
        Interlocked.Increment(ref _saveTimeCount);
    }

    /// <summary>Records a drain duration for averaging.</summary>
    public void RecordDrainTime(long elapsedTicks)
    {
        Interlocked.Add(ref _drainTimeAccumulatorTicks, elapsedTicks);
        Interlocked.Increment(ref _drainTimeCount);
    }

    /// <summary>
    /// Resets rolling averages. Call periodically.
    /// </summary>
    public void ResetAverages()
    {
        Interlocked.Exchange(ref _generationTimeCount, 0);
        Interlocked.Exchange(ref _averageGenerationTimeTicks, 0);
        Interlocked.Exchange(ref _saveTimeCount, 0);
        Interlocked.Exchange(ref _averageSaveTimeTicks, 0);
        Interlocked.Exchange(ref _drainTimeCount, 0);
        Interlocked.Exchange(ref _drainTimeAccumulatorTicks, 0);
    }
}
