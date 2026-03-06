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

    private double _memorySnapshotAccumulator;
    private long _lastActiveChunks;
    private long _lastTotalVertices;

    /// <summary>
    /// Creates a new performance sampler with all diagnostic sub-systems.
    /// </summary>
    public PerformanceSampler()
    {
        FrameTimeTracker = new FrameTimeTracker();
        SpikeDetector = new SpikeDetector();
        MemoryMetrics = new MemoryMetrics();
    }

    /// <summary>
    /// The frame time tracker with history and statistics.
    /// </summary>
    public FrameTimeTracker FrameTimeTracker { get; }

    /// <summary>
    /// The spike detector with spike history.
    /// </summary>
    public SpikeDetector SpikeDetector { get; }

    /// <summary>
    /// The memory metrics snapshot (updated periodically).
    /// </summary>
    public MemoryMetrics MemoryMetrics { get; }

    /// <summary>
    /// The current frame number (monotonically increasing).
    /// </summary>
    public long FrameNumber { get; private set; }

    /// <summary>
    /// Samples a frame. Call once per frame from DebugManager._Process.
    /// </summary>
    /// <param name="deltaSeconds">Delta time in seconds from Godot's _Process.</param>
    /// <param name="breakdown">Per-component timing breakdown for this frame.</param>
    public void Sample(double deltaSeconds, FrameTimeBreakdown breakdown)
    {
        FrameNumber++;
        double frameTimeMs = deltaSeconds * MillisecondsPerSecond;

        FrameTimeTracker.Record(frameTimeMs);
        SpikeDetector.Evaluate(frameTimeMs, FrameNumber, breakdown);

        _memorySnapshotAccumulator += deltaSeconds;

        if (_memorySnapshotAccumulator >= MemorySnapshotIntervalSeconds)
        {
            MemoryMetrics.Snapshot(_lastActiveChunks, _lastTotalVertices);
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
        FrameTimeTracker.Buffer.Clear();
        SpikeDetector.ClearHistory();
        MemoryMetrics.Snapshot(0, 0);
        _memorySnapshotAccumulator = 0;
        FrameNumber = 0;
    }
}
#endif
