namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Record of a single frame time spike exceeding the budget threshold.
/// </summary>
public readonly struct SpikeRecord
{
    /// <summary>Frame time in milliseconds.</summary>
    public double FrameTimeMs { get; init; }

    /// <summary>Frame number when the spike occurred.</summary>
    public long FrameNumber { get; init; }

    /// <summary>Breakdown of time spent per subsystem during the spike.</summary>
    public FrameTimeBreakdown Breakdown { get; init; }
}
