#if DEBUG
using System;
using System.Text;

using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Godot.UI.Debug.Components;

namespace MineRPG.Godot.UI.Debug.Tabs;

/// <summary>
/// Manages the benchmark runner and A/B comparison snapshot UI
/// within the performance tab.
/// </summary>
internal sealed class PerformanceBenchmarkSection
{
    private readonly PerformanceSampler _sampler;
    private readonly PerformanceMonitor _monitor;
    private readonly BenchmarkRunner _benchmarkRunner = new();
    private readonly StringBuilder _builder;

    private Label _benchmarkLabel = null!;
    private Label _abLabel = null!;

    private ABCompareSnapshot _snapshotA;
    private ABCompareSnapshot _snapshotB;

    /// <summary>
    /// Creates a benchmark section for the given sampler and monitor.
    /// </summary>
    /// <param name="sampler">Performance sampler for frame timing.</param>
    /// <param name="monitor">Performance monitor for metrics.</param>
    /// <param name="builder">Shared StringBuilder for text formatting.</param>
    public PerformanceBenchmarkSection(
        PerformanceSampler sampler,
        PerformanceMonitor monitor,
        StringBuilder builder)
    {
        _sampler = sampler;
        _monitor = monitor;
        _builder = builder;
    }

    /// <summary>
    /// Creates the benchmark and A/B comparison UI sections.
    /// </summary>
    /// <param name="parent">The parent container to add sections to.</param>
    public void BuildLayout(VBoxContainer parent)
    {
        DebugSection benchmarkSection = DebugSection.Create("Benchmark");
        parent.AddChild(benchmarkSection);

        DebugButton benchmarkButton = DebugButton.Create("Start 5s Benchmark", StartBenchmark);
        benchmarkSection.Content.AddChild(benchmarkButton);

        _benchmarkLabel = new Label();
        DebugTheme.ApplyLabelStyle(_benchmarkLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        benchmarkSection.Content.AddChild(_benchmarkLabel);

        DebugSection abSection = DebugSection.Create("A/B Compare");
        parent.AddChild(abSection);

        HBoxContainer abButtons = new();
        abButtons.AddThemeConstantOverride("separation", 8);
        abSection.Content.AddChild(abButtons);

        DebugButton snapshotAButton = DebugButton.Create("Snapshot A", TakeSnapshotA);
        abButtons.AddChild(snapshotAButton);

        DebugButton snapshotBButton = DebugButton.Create("Snapshot B", TakeSnapshotB);
        abButtons.AddChild(snapshotBButton);

        _abLabel = new Label();
        DebugTheme.ApplyLabelStyle(_abLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        abSection.Content.AddChild(_abLabel);

        UpdateABDisplay();
    }

    /// <summary>
    /// Updates the benchmark progress and result display.
    /// </summary>
    /// <param name="delta">Frame delta in seconds.</param>
    public void UpdateBenchmark(double delta)
    {
        if (!_benchmarkRunner.IsRunning)
        {
            return;
        }

        double heapMb = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
        long drawCalls = (long)RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalDrawCallsInFrame);

        bool stillRunning = _benchmarkRunner.RecordFrame(
            delta, drawCalls, _monitor.TotalVertices, heapMb);

        if (!stillRunning)
        {
            BenchmarkReport report = _benchmarkRunner.GenerateReport(
                _monitor.RenderDistance, (int)_monitor.ActiveChunks);

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
