namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Results of a timed benchmark run.
/// </summary>
public readonly struct BenchmarkReport
{
    /// <summary>Duration of the benchmark in seconds.</summary>
    public double DurationSeconds { get; init; }

    /// <summary>Total number of frames captured.</summary>
    public int TotalFrames { get; init; }

    /// <summary>Render distance during the benchmark.</summary>
    public int RenderDistance { get; init; }

    /// <summary>Number of loaded chunks during the benchmark.</summary>
    public int LoadedChunks { get; init; }

    /// <summary>Average FPS.</summary>
    public double AverageFps { get; init; }

    /// <summary>Median FPS.</summary>
    public double MedianFps { get; init; }

    /// <summary>1% low FPS (worst 1% of frames).</summary>
    public double OnePercentLowFps { get; init; }

    /// <summary>0.1% low FPS (worst 0.1% of frames).</summary>
    public double PointOnePercentLowFps { get; init; }

    /// <summary>Minimum FPS recorded.</summary>
    public double MinFps { get; init; }

    /// <summary>Maximum FPS recorded.</summary>
    public double MaxFps { get; init; }

    /// <summary>Average frame time in milliseconds.</summary>
    public double AverageFrameTimeMs { get; init; }

    /// <summary>99th percentile frame time in milliseconds.</summary>
    public double Percentile99FrameTimeMs { get; init; }

    /// <summary>99.9th percentile frame time in milliseconds.</summary>
    public double Percentile999FrameTimeMs { get; init; }

    /// <summary>Number of frame spikes above 16.6ms.</summary>
    public int SpikeCount { get; init; }

    /// <summary>Average draw call count.</summary>
    public long AverageDrawCalls { get; init; }

    /// <summary>Average vertex count.</summary>
    public long AverageVertices { get; init; }

    /// <summary>Peak memory usage in megabytes.</summary>
    public double PeakMemoryMb { get; init; }
}
