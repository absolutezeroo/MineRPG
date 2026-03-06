namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Detects frame time spikes that exceed a configurable threshold
/// and maintains a history of the last N spikes.
/// </summary>
public sealed class SpikeDetector
{
    private const int DefaultHistorySize = 50;
    private const double DefaultThresholdMs = 16.667;

    /// <summary>
    /// Creates a spike detector with the given threshold and history size.
    /// </summary>
    /// <param name="thresholdMs">Frame time threshold in milliseconds. Frames exceeding this are spikes.</param>
    /// <param name="historySize">Number of spike records to retain.</param>
    public SpikeDetector(double thresholdMs = DefaultThresholdMs, int historySize = DefaultHistorySize)
    {
        ThresholdMs = thresholdMs;
        History = new RingBuffer<SpikeRecord>(historySize);
    }

    /// <summary>
    /// Threshold in milliseconds. Frames above this are considered spikes.
    /// </summary>
    public double ThresholdMs { get; }

    /// <summary>
    /// Number of spikes currently in the history buffer.
    /// </summary>
    public int SpikeCount => History.Count;

    /// <summary>
    /// The spike history ring buffer for direct read access.
    /// </summary>
    public RingBuffer<SpikeRecord> History { get; }

    /// <summary>
    /// Evaluates a frame. If the frame time exceeds the threshold, records a spike.
    /// </summary>
    /// <param name="frameTimeMs">The frame time in milliseconds.</param>
    /// <param name="frameNumber">The current frame number.</param>
    /// <param name="breakdown">The frame time breakdown for this frame.</param>
    /// <returns>True if a spike was detected, false otherwise.</returns>
    public bool Evaluate(double frameTimeMs, long frameNumber, FrameTimeBreakdown breakdown)
    {
        if (frameTimeMs <= ThresholdMs)
        {
            return false;
        }

        SpikeRecord spike = new()
        {
            FrameTimeMs = frameTimeMs,
            FrameNumber = frameNumber,
            Breakdown = breakdown,
        };

        History.Push(spike);
        return true;
    }

    /// <summary>
    /// Clears the spike history.
    /// </summary>
    public void ClearHistory() => History.Clear();
}
