#if DEBUG
using System;
using System.Text;

using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Godot.UI.Debug.Components;

namespace MineRPG.Godot.UI.Debug.Tabs;

/// <summary>
/// Tab 3: Performance. Displays live metrics, pipeline stats, memory,
/// spike log, benchmark runner, and A/B comparison.
/// </summary>
public sealed partial class PerformanceTab : VBoxContainer, IDebugTab
{
    private const int StringBuilderCapacity = 512;

    private readonly PerformanceSampler _sampler;
    private readonly PerformanceMonitor _monitor;
    private readonly PipelineMetrics _pipeline;
    private readonly BenchmarkRunner _benchmarkRunner = new();
    private readonly StringBuilder _builder = new(StringBuilderCapacity);

    private Label _metricsLabel = null!;
    private Label _pipelineLabel = null!;
    private Label _memoryLabel = null!;
    private Label _spikeLabel = null!;
    private Label _benchmarkLabel = null!;
    private Label _abLabel = null!;
    private DebugButton _benchmarkButton = null!;

    private ABCompareSnapshot _snapshotA;
    private ABCompareSnapshot _snapshotB;

    /// <summary>
    /// Creates the performance tab.
    /// </summary>
    public PerformanceTab(
        PerformanceSampler sampler,
        PerformanceMonitor monitor,
        PipelineMetrics pipeline)
    {
        _sampler = sampler;
        _monitor = monitor;
        _pipeline = pipeline;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 4);

        // -- Live Metrics --
        DebugSection metricsSection = new("Live Metrics");
        AddChild(metricsSection);

        _metricsLabel = new Label();
        DebugTheme.ApplyLabelStyle(_metricsLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        metricsSection.Content.AddChild(_metricsLabel);

        // -- Pipeline --
        DebugSection pipelineSection = new("Pipeline");
        AddChild(pipelineSection);

        _pipelineLabel = new Label();
        DebugTheme.ApplyLabelStyle(_pipelineLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        pipelineSection.Content.AddChild(_pipelineLabel);

        // -- Memory --
        DebugSection memorySection = new("Memory");
        AddChild(memorySection);

        _memoryLabel = new Label();
        DebugTheme.ApplyLabelStyle(_memoryLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        memorySection.Content.AddChild(_memoryLabel);

        // -- Spike Log --
        DebugSection spikeSection = new("Spike Log", false);
        AddChild(spikeSection);

        _spikeLabel = new Label();
        DebugTheme.ApplyLabelStyle(_spikeLabel, DebugTheme.TextWarning, DebugTheme.FontSizeSmall);
        spikeSection.Content.AddChild(_spikeLabel);

        // -- Benchmark --
        DebugSection benchmarkSection = new("Benchmark");
        AddChild(benchmarkSection);

        _benchmarkButton = new DebugButton("Start 5s Benchmark", StartBenchmark);
        benchmarkSection.Content.AddChild(_benchmarkButton);

        _benchmarkLabel = new Label();
        DebugTheme.ApplyLabelStyle(_benchmarkLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        benchmarkSection.Content.AddChild(_benchmarkLabel);

        // -- A/B Compare --
        DebugSection abSection = new("A/B Compare");
        AddChild(abSection);

        HBoxContainer abButtons = new();
        abButtons.AddThemeConstantOverride("separation", 8);
        abSection.Content.AddChild(abButtons);

        DebugButton snapshotAButton = new("Snapshot A", TakeSnapshotA);
        abButtons.AddChild(snapshotAButton);

        DebugButton snapshotBButton = new("Snapshot B", TakeSnapshotB);
        abButtons.AddChild(snapshotBButton);

        _abLabel = new Label();
        DebugTheme.ApplyLabelStyle(_abLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        abSection.Content.AddChild(_abLabel);

        UpdateABDisplay();
    }

    /// <inheritdoc />
    public void UpdateDisplay(double delta)
    {
        UpdateMetrics();
        UpdatePipeline();
        UpdateMemory();
        UpdateSpikes();
        UpdateBenchmark(delta);
    }

    private void UpdateMetrics()
    {
        FrameTimeTracker tracker = _sampler.FrameTimeTracker;
        double fps = Engine.GetFramesPerSecond();

        _builder.Clear();
        _builder.Append("FPS: ").Append(fps.ToString("F0")).AppendLine();
        _builder.Append("Frame: ").Append(tracker.AverageFrameTimeMs.ToString("F2")).Append("ms").AppendLine();
        _builder.Append("99th: ").Append(tracker.GetPercentile(99).ToString("F2")).Append("ms").AppendLine();
        _builder.Append("Draw Calls: ").Append(RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalDrawCallsInFrame)).AppendLine();
        _builder.Append("Vertices: ").Append(_monitor.TotalVertices.ToString("N0")).AppendLine();
        _builder.Append("Spikes: ").Append(_sampler.SpikeDetector.SpikeCount);
        _metricsLabel.Text = _builder.ToString();
    }

    private void UpdatePipeline()
    {
        _builder.Clear();
        _builder.Append("Gen Queue: ").Append(_pipeline.GenerationQueueSize).AppendLine();
        _builder.Append("Mesh Queue: ").Append(_pipeline.RemeshQueueSize).AppendLine();
        _builder.Append("Save Queue: ").Append(_pipeline.SaveQueueSize).AppendLine();
        _builder.Append("Workers: ").Append(_pipeline.ActiveWorkerCount)
            .Append('/').Append(_pipeline.TotalWorkerCount).AppendLine();
        _builder.Append("Avg Gen: ").Append(_pipeline.AverageGenerationTimeMs.ToString("F1")).Append("ms").AppendLine();
        _builder.Append("Avg Mesh: ").Append(_monitor.AverageMeshTimeMs.ToString("F1")).Append("ms").AppendLine();
        _builder.Append("Avg Drain: ").Append(_pipeline.AverageDrainTimeMs.ToString("F1")).Append("ms");
        _pipelineLabel.Text = _builder.ToString();
    }

    private void UpdateMemory()
    {
        MemoryMetrics memory = _sampler.MemoryMetrics;

        _builder.Clear();
        _builder.Append("GC Heap: ").Append(memory.GcHeapMb.ToString("F1")).Append(" MB").AppendLine();
        _builder.Append("Gen0: ").Append(memory.Gen0Collections)
            .Append("  Gen1: ").Append(memory.Gen1Collections)
            .Append("  Gen2: ").Append(memory.Gen2Collections).AppendLine();
        _builder.Append("Chunk Data: ~").Append(memory.EstimatedChunkDataMb.ToString("F1")).Append(" MB").AppendLine();
        _builder.Append("Mesh Data: ~").Append(memory.EstimatedMeshDataMb.ToString("F1")).Append(" MB");
        _memoryLabel.Text = _builder.ToString();
    }

    private void UpdateSpikes()
    {
        RingBuffer<SpikeRecord> history = _sampler.SpikeDetector.History;
        int count = history.Count;

        if (count == 0)
        {
            _spikeLabel.Text = "No spikes recorded.";
            return;
        }

        _builder.Clear();
        int start = System.Math.Max(0, count - 8);

        for (int i = count - 1; i >= start; i--)
        {
            SpikeRecord spike = history.PeekAt(i);
            _builder.Append('#').Append(spike.FrameNumber)
                .Append(": ").Append(spike.FrameTimeMs.ToString("F1")).Append("ms").AppendLine();
        }

        _spikeLabel.Text = _builder.ToString();
    }

    private void UpdateBenchmark(double delta)
    {
        if (!_benchmarkRunner.IsRunning)
        {
            return;
        }

        double heapMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
        long drawCalls = (long)RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalDrawCallsInFrame);

        bool stillRunning = _benchmarkRunner.RecordFrame(
            delta,
            drawCalls,
            _monitor.TotalVertices,
            heapMb);

        if (!stillRunning)
        {
            BenchmarkReport report = _benchmarkRunner.GenerateReport(
                _monitor.RenderDistance,
                (int)_monitor.ActiveChunks);

            _builder.Clear();
            _builder.Append("Avg FPS: ").Append(report.AverageFps.ToString("F1")).AppendLine();
            _builder.Append("Median FPS: ").Append(report.MedianFps.ToString("F1")).AppendLine();
            _builder.Append("1% Low: ").Append(report.OnePercentLowFps.ToString("F1")).AppendLine();
            _builder.Append("0.1% Low: ").Append(report.PointOnePercentLowFps.ToString("F1")).AppendLine();
            _builder.Append("Min: ").Append(report.MinFps.ToString("F1"))
                .Append("  Max: ").Append(report.MaxFps.ToString("F1")).AppendLine();
            _builder.Append("Spikes: ").Append(report.SpikeCount).AppendLine();
            _builder.Append("Peak Mem: ").Append(report.PeakMemoryMb.ToString("F1")).Append(" MB");
            _benchmarkLabel.Text = _builder.ToString();
        }
        else
        {
            _benchmarkLabel.Text = $"Running... {_benchmarkRunner.Progress * 100:F0}%";
        }
    }

    private void StartBenchmark()
    {
        _benchmarkRunner.Start();
        _benchmarkLabel.Text = "Starting benchmark...";
    }

    private void TakeSnapshotA()
    {
        _snapshotA = TakeSnapshot("A");
        UpdateABDisplay();
    }

    private void TakeSnapshotB()
    {
        _snapshotB = TakeSnapshot("B");
        UpdateABDisplay();
    }

    private ABCompareSnapshot TakeSnapshot(string label)
    {
        FrameTimeTracker tracker = _sampler.FrameTimeTracker;

        return new ABCompareSnapshot
        {
            Label = label,
            AverageFps = Engine.GetFramesPerSecond(),
            AverageFrameTimeMs = tracker.AverageFrameTimeMs,
            Vertices = _monitor.TotalVertices,
            DrawCalls = (long)RenderingServer.GetRenderingInfo(
                RenderingServer.RenderingInfo.TotalDrawCallsInFrame),
            MemoryMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0),
            IsValid = true,
        };
    }

    private void UpdateABDisplay()
    {
        _builder.Clear();

        if (!_snapshotA.IsValid)
        {
            _builder.Append("Take Snapshot A first.");
            _abLabel.Text = _builder.ToString();
            return;
        }

        _builder.Append("A: FPS=").Append(_snapshotA.AverageFps.ToString("F0"))
            .Append("  Verts=").Append(_snapshotA.Vertices.ToString("N0"))
            .Append("  Mem=").Append(_snapshotA.MemoryMb.ToString("F1")).Append("MB").AppendLine();

        if (!_snapshotB.IsValid)
        {
            _builder.Append("Take Snapshot B to compare.");
            _abLabel.Text = _builder.ToString();
            return;
        }

        _builder.Append("B: FPS=").Append(_snapshotB.AverageFps.ToString("F0"))
            .Append("  Verts=").Append(_snapshotB.Vertices.ToString("N0"))
            .Append("  Mem=").Append(_snapshotB.MemoryMb.ToString("F1")).Append("MB").AppendLine();

        double fpsDelta = _snapshotB.AverageFps - _snapshotA.AverageFps;
        double fpsPercent = _snapshotA.AverageFps > 0
            ? fpsDelta / _snapshotA.AverageFps * 100
            : 0;

        _builder.Append("Delta FPS: ").Append(fpsDelta.ToString("+0.0;-0.0"))
            .Append(" (").Append(fpsPercent.ToString("+0.0;-0.0")).Append("%)");

        _abLabel.Text = _builder.ToString();
    }
}
#endif
