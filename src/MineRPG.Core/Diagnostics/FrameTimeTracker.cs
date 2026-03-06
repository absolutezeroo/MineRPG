using System;

namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Tracks frame times in a ring buffer and computes statistics
/// (min, max, average, percentiles) over the stored history.
/// Called once per frame by <see cref="PerformanceSampler"/>.
/// </summary>
public sealed class FrameTimeTracker
{
    private const int DefaultCapacity = 300;

    private readonly double[] _sortBuffer;

    /// <summary>
    /// Creates a frame time tracker with the specified history capacity.
    /// </summary>
    /// <param name="capacity">Number of frame times to retain.</param>
    public FrameTimeTracker(int capacity = DefaultCapacity)
    {
        Buffer = new RingBuffer<double>(capacity);
        _sortBuffer = new double[capacity];
    }

    /// <summary>
    /// Minimum frame time in the current history window.
    /// </summary>
    public double MinFrameTimeMs { get; private set; }

    /// <summary>
    /// Maximum frame time in the current history window.
    /// </summary>
    public double MaxFrameTimeMs { get; private set; }

    /// <summary>
    /// Average frame time in the current history window.
    /// </summary>
    public double AverageFrameTimeMs { get; private set; }

    /// <summary>
    /// The number of frame time samples currently stored.
    /// </summary>
    public int SampleCount => Buffer.Count;

    /// <summary>
    /// The underlying ring buffer for direct access by graph renderers.
    /// </summary>
    public RingBuffer<double> Buffer { get; }

    /// <summary>
    /// Records a frame time and recomputes statistics.
    /// </summary>
    /// <param name="frameTimeMs">Frame time in milliseconds.</param>
    public void Record(double frameTimeMs)
    {
        Buffer.Push(frameTimeMs);
        RecomputeStatistics();
    }

    /// <summary>
    /// Computes the Nth percentile of frame times in the current history.
    /// Uses linear interpolation between nearest ranks.
    /// </summary>
    /// <param name="percentile">Percentile value in [0, 100].</param>
    /// <returns>The frame time at the given percentile, or 0 if no samples.</returns>
    public double GetPercentile(double percentile)
    {
        int count = Buffer.Count;

        if (count == 0)
        {
            return 0;
        }

        int copied = Buffer.CopyTo(_sortBuffer.AsSpan(0, count));
        Array.Sort(_sortBuffer, 0, copied);

        double rank = percentile / 100.0 * (copied - 1);
        int lowerIndex = (int)System.Math.Floor(rank);
        int upperIndex = System.Math.Min(lowerIndex + 1, copied - 1);
        double fraction = rank - lowerIndex;

        return _sortBuffer[lowerIndex] + fraction * (_sortBuffer[upperIndex] - _sortBuffer[lowerIndex]);
    }

    private void RecomputeStatistics()
    {
        int count = Buffer.Count;

        if (count == 0)
        {
            MinFrameTimeMs = 0;
            MaxFrameTimeMs = 0;
            AverageFrameTimeMs = 0;
            return;
        }

        double min = double.MaxValue;
        double max = double.MinValue;
        double sum = 0;

        for (int i = 0; i < count; i++)
        {
            double value = Buffer.PeekAt(i);
            sum += value;

            if (value < min)
            {
                min = value;
            }

            if (value > max)
            {
                max = value;
            }
        }

        MinFrameTimeMs = min;
        MaxFrameTimeMs = max;
        AverageFrameTimeMs = sum / count;
    }
}
