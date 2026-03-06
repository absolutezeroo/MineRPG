#if DEBUG
using System;

using MineRPG.Core.Diagnostics;

namespace MineRPG.Godot.UI.Debug;

/// <summary>
/// Aggregates per-frame performance data into diagnostic trackers.
/// Owned by DebugManager and called once per frame when any debug module is active.
/// Plain C# class (not a Node) — no scene tree overhead.
/// </summary>
public sealed class PerformanceSampler
{
    private const double MillisecondsPerSecond = 1000.0;
    private const double MemorySnapshotIntervalSeconds = 1.0;

    private readonly FrameTimeTracker _frameTimeTracker;
    private readonly SpikeDetector _spikeDetector;
    private readonly MemoryMetrics _memoryMetrics;

    private double _memorySnapshotAccumulator;
    private long _frameNumber;
    private long _lastActiveChunks;
    private long _lastTotalVertices;

    /// <summary>
    /// Creates a new performance sampler with all diagnostic sub-systems.
    /// </summary>
    public PerformanceSampler()
    {
        _frameTimeTracker = new FrameTimeTracker();
        _spikeDetector = new SpikeDetector();
        _memoryMetrics = new MemoryMetrics();
    }

    /// <summary>
    /// The frame time tracker with history and statistics.
    /// </summary>
    public FrameTimeTracker FrameTimeTracker => _frameTimeTracker;

    /// <summary>
    /// The spike detector with spike history.
    /// </summary>
    public SpikeDetector SpikeDetector => _spikeDetector;

    /// <summary>
    /// The memory metrics snapshot (updated periodically).
    /// </summary>
    public MemoryMetrics MemoryMetrics => _memoryMetrics;

    /// <summary>
    /// The current frame number (monotonically increasing).
    /// </summary>
    public long FrameNumber => _frameNumber;

    /// <summary>
    /// Samples a frame. Call once per frame from DebugManager._Process.
    /// </summary>
    /// <param name="deltaSeconds">Delta time in seconds from Godot's _Process.</param>
    /// <param name="breakdown">Per-component timing breakdown for this frame.</param>
    public void Sample(double deltaSeconds, FrameTimeBreakdown breakdown)
    {
        _frameNumber++;
        double frameTimeMs = deltaSeconds * MillisecondsPerSecond;

        _frameTimeTracker.Record(frameTimeMs);
        _spikeDetector.Evaluate(frameTimeMs, _frameNumber, breakdown);

        _memorySnapshotAccumulator += deltaSeconds;

        if (_memorySnapshotAccumulator >= MemorySnapshotIntervalSeconds)
        {
            _memoryMetrics.Snapshot(_lastActiveChunks, _lastTotalVertices);
            _memorySnapshotAccumulator = 0;
        }
    }

    /// <summary>
    /// Updates the cached resource counts for memory estimation.
    /// Call when chunk/mesh counts change.
    /// </summary>
    /// <param name="loadedChunkCount">Number of loaded chunks.</param>
    /// <param name="totalVertexCount">Total vertex count across all meshes.</param>
    public void UpdateResourceMetrics(long loadedChunkCount, long totalVertexCount)
    {
        _lastActiveChunks = loadedChunkCount;
        _lastTotalVertices = totalVertexCount;
    }

    /// <summary>
    /// Resets all sampled data (frame times, spikes, memory snapshots).
    /// </summary>
    public void Reset()
    {
        _frameTimeTracker.Buffer.Clear();
        _spikeDetector.ClearHistory();
        _memoryMetrics.Snapshot(0, 0);
        _memorySnapshotAccumulator = 0;
        _frameNumber = 0;
    }
}
#endif
