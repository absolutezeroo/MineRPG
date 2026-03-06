namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Snapshot of performance metrics at a point in time, used for A/B comparison.
/// </summary>
public readonly struct ABCompareSnapshot
{
    /// <summary>Label for this snapshot (e.g., "Greedy ON").</summary>
    public string Label { get; init; }

    /// <summary>Average FPS at the time of snapshot.</summary>
    public double AverageFps { get; init; }

    /// <summary>Average frame time in milliseconds.</summary>
    public double AverageFrameTimeMs { get; init; }

    /// <summary>Total vertex count.</summary>
    public long Vertices { get; init; }

    /// <summary>Total draw call count.</summary>
    public long DrawCalls { get; init; }

    /// <summary>GC heap memory in megabytes.</summary>
    public double MemoryMb { get; init; }

    /// <summary>Whether this snapshot contains valid data.</summary>
    public bool IsValid { get; init; }
}
