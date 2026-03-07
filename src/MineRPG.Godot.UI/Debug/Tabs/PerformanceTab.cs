#if DEBUG
using System.Text;

using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Godot.UI.Debug.Components;

namespace MineRPG.Godot.UI.Debug.Tabs;

/// <summary>
/// Tab 3: Performance. Displays live metrics, pipeline stats, memory,
/// spike log, and delegates benchmark/A/B comparison to
/// <see cref="PerformanceBenchmarkSection"/>.
/// </summary>
public sealed partial class PerformanceTab : VBoxContainer, IDebugTab
{
    private const int StringBuilderCapacity = 512;

    private readonly PerformanceSampler _sampler;
    private readonly PerformanceMonitor _monitor;
    private readonly PipelineMetrics _pipeline;
    private readonly StringBuilder _builder = new(StringBuilderCapacity);

    private Label _metricsLabel = null!;
    private Label _pipelineLabel = null!;
    private Label _memoryLabel = null!;
    private Label _spikeLabel = null!;

    private PerformanceBenchmarkSection _benchmarkSection = null!;

    /// <summary>
    /// Creates the performance tab.
    /// </summary>
    /// <param name="sampler">Performance sampler for frame timing.</param>
    /// <param name="monitor">Performance monitor for metrics.</param>
    /// <param name="pipeline">Pipeline metrics for queue sizes.</param>
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

        DebugSection metricsSection = DebugSection.Create("Live Metrics");
        AddChild(metricsSection);

        _metricsLabel = new Label();
        DebugTheme.ApplyLabelStyle(_metricsLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        metricsSection.Content.AddChild(_metricsLabel);

        DebugSection pipelineSection = DebugSection.Create("Pipeline");
        AddChild(pipelineSection);

        _pipelineLabel = new Label();
        DebugTheme.ApplyLabelStyle(_pipelineLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        pipelineSection.Content.AddChild(_pipelineLabel);

        DebugSection memorySection = DebugSection.Create("Memory");
        AddChild(memorySection);

        _memoryLabel = new Label();
        DebugTheme.ApplyLabelStyle(_memoryLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        memorySection.Content.AddChild(_memoryLabel);

        DebugSection spikeSection = DebugSection.Create("Spike Log", false);
        AddChild(spikeSection);

        _spikeLabel = new Label();
        DebugTheme.ApplyLabelStyle(_spikeLabel, DebugTheme.TextWarning, DebugTheme.FontSizeSmall);
        spikeSection.Content.AddChild(_spikeLabel);

        _benchmarkSection = new PerformanceBenchmarkSection(_sampler, _monitor, _builder);
        _benchmarkSection.BuildLayout(this);
    }

    /// <inheritdoc />
    public void UpdateDisplay(double delta)
    {
        UpdateMetrics();
        UpdatePipeline();
        UpdateMemory();
        UpdateSpikes();
        _benchmarkSection.UpdateBenchmark(delta);
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
}
#endif
