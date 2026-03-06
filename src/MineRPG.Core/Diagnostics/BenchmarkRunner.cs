using System;
using System.Collections.Generic;

namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Manages timed benchmark runs. Collects frame times over a configurable
/// duration and produces a <see cref="BenchmarkReport"/>.
/// Pure C# — no Godot dependency.
/// </summary>
public sealed class BenchmarkRunner
{
    private const double DefaultDurationSeconds = 5.0;
    private const double StabilizationSeconds = 1.0;
    private const double SpikeThresholdMs = 16.667;
    private const double MillisecondsPerSecond = 1000.0;

    private readonly List<double> _frameTimes = new();
    private readonly List<long> _drawCalls = new();
    private readonly List<long> _vertices = new();
    private readonly List<double> _memoryMb = new();

    private double _elapsedSeconds;
    private double _stabilizationElapsed;
    private bool _isStabilizing;

    /// <summary>
    /// Whether a benchmark is currently running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// The configured duration in seconds.
    /// </summary>
    public double DurationSeconds { get; private set; }

    /// <summary>
    /// Progress of the current benchmark [0, 1].
    /// </summary>
    public double Progress => DurationSeconds > 0 ? System.Math.Clamp(_elapsedSeconds / DurationSeconds, 0, 1) : 0;

    /// <summary>
    /// Starts a benchmark run with the given duration.
    /// </summary>
    /// <param name="durationSeconds">Duration in seconds to collect data.</param>
    public void Start(double durationSeconds = DefaultDurationSeconds)
    {
        DurationSeconds = durationSeconds;
        _frameTimes.Clear();
        _drawCalls.Clear();
        _vertices.Clear();
        _memoryMb.Clear();
        _elapsedSeconds = 0;
        _stabilizationElapsed = 0;
        _isStabilizing = true;
        IsRunning = true;
    }

    /// <summary>
    /// Records a frame during the benchmark. Call once per frame while running.
    /// </summary>
    /// <param name="deltaSeconds">Frame delta time in seconds.</param>
    /// <param name="drawCallCount">Number of draw calls this frame.</param>
    /// <param name="vertexCount">Number of vertices this frame.</param>
    /// <param name="heapMb">Current GC heap size in megabytes.</param>
    /// <returns>True if the benchmark is still running, false if complete.</returns>
    public bool RecordFrame(double deltaSeconds, long drawCallCount, long vertexCount, double heapMb)
    {
        if (!IsRunning)
        {
            return false;
        }

        if (_isStabilizing)
        {
            _stabilizationElapsed += deltaSeconds;

            if (_stabilizationElapsed < StabilizationSeconds)
            {
                return true;
            }

            _isStabilizing = false;
        }

        _elapsedSeconds += deltaSeconds;

        if (_elapsedSeconds >= DurationSeconds)
        {
            IsRunning = false;
            return false;
        }

        double frameTimeMs = deltaSeconds * MillisecondsPerSecond;
        _frameTimes.Add(frameTimeMs);
        _drawCalls.Add(drawCallCount);
        _vertices.Add(vertexCount);
        _memoryMb.Add(heapMb);

        return true;
    }

    /// <summary>
    /// Generates the benchmark report from collected data.
    /// </summary>
    /// <param name="renderDistance">Current render distance for the report.</param>
    /// <param name="loadedChunks">Current loaded chunk count for the report.</param>
    /// <returns>The benchmark report.</returns>
    public BenchmarkReport GenerateReport(int renderDistance, int loadedChunks)
    {
        if (_frameTimes.Count == 0)
        {
            return default;
        }

        double[] sorted = _frameTimes.ToArray();
        Array.Sort(sorted);

        int count = sorted.Length;
        double totalTime = 0;
        int spikeCount = 0;

        for (int i = 0; i < count; i++)
        {
            totalTime += sorted[i];

            if (sorted[i] > SpikeThresholdMs)
            {
                spikeCount++;
            }
        }

        double avgFrameTime = totalTime / count;
        double avgFps = avgFrameTime > 0 ? MillisecondsPerSecond / avgFrameTime : 0;
        double medianFrameTime = sorted[count / 2];
        double medianFps = medianFrameTime > 0 ? MillisecondsPerSecond / medianFrameTime : 0;

        int onePercentIndex = System.Math.Max(0, (int)(count * 0.99) - 1);
        double onePercentFrameTime = sorted[onePercentIndex];
        double onePercentLowFps = onePercentFrameTime > 0 ? MillisecondsPerSecond / onePercentFrameTime : 0;

        int pointOnePercentIndex = System.Math.Max(0, (int)(count * 0.999) - 1);
        double pointOnePercentFrameTime = sorted[pointOnePercentIndex];
        double pointOnePercentLowFps = pointOnePercentFrameTime > 0 ? MillisecondsPerSecond / pointOnePercentFrameTime : 0;

        double minFrameTime = sorted[0];
        double maxFrameTime = sorted[count - 1];
        double maxFps = minFrameTime > 0 ? MillisecondsPerSecond / minFrameTime : 0;
        double minFps = maxFrameTime > 0 ? MillisecondsPerSecond / maxFrameTime : 0;

        long totalDrawCalls = 0;
        long totalVertices = 0;
        double peakMemory = 0;

        for (int i = 0; i < _drawCalls.Count; i++)
        {
            totalDrawCalls += _drawCalls[i];
            totalVertices += _vertices[i];

            if (_memoryMb[i] > peakMemory)
            {
                peakMemory = _memoryMb[i];
            }
        }

        return new BenchmarkReport
        {
            DurationSeconds = _elapsedSeconds,
            TotalFrames = count,
            RenderDistance = renderDistance,
            LoadedChunks = loadedChunks,
            AverageFps = avgFps,
            MedianFps = medianFps,
            OnePercentLowFps = onePercentLowFps,
            PointOnePercentLowFps = pointOnePercentLowFps,
            MinFps = minFps,
            MaxFps = maxFps,
            AverageFrameTimeMs = avgFrameTime,
            Percentile99FrameTimeMs = onePercentFrameTime,
            Percentile999FrameTimeMs = pointOnePercentFrameTime,
            SpikeCount = spikeCount,
            AverageDrawCalls = _drawCalls.Count > 0 ? totalDrawCalls / _drawCalls.Count : 0,
            AverageVertices = _vertices.Count > 0 ? totalVertices / _vertices.Count : 0,
            PeakMemoryMb = peakMemory,
        };
    }

    /// <summary>
    /// Aborts a running benchmark.
    /// </summary>
    public void Abort() => IsRunning = false;
}
