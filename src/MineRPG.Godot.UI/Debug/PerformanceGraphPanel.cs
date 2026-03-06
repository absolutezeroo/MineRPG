#if DEBUG
using System;
using System.Text;

using Godot;

using MineRPG.Core.Diagnostics;

namespace MineRPG.Godot.UI.Debug;

/// <summary>
/// F6 performance graph panel. Draws a scrolling frame time graph
/// with a 60 FPS target line, spike markers, and a spike log.
/// Uses <see cref="Control._Draw"/> for efficient 2D rendering.
/// </summary>
public sealed partial class PerformanceGraphPanel : Control
{
    private const float TargetFrameTimeMs = 16.667f;
    private const float GraphScaleMax = 50f;
    private const float GraphPaddingX = 8f;
    private const float GraphPaddingY = 4f;
    private const float SpikeLogSpacing = 4f;
    private const int MaxSpikeLogEntries = 8;
    private const int StringBuilderCapacity = 256;

    private readonly PerformanceSampler _sampler;
    private readonly StringBuilder _statsBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _spikeBuilder = new(StringBuilderCapacity);

    private Label _statsLabel = null!;
    private Label _spikeLogLabel = null!;

    /// <summary>
    /// Creates a new PerformanceGraphPanel.
    /// </summary>
    /// <param name="sampler">The performance sampler with frame time data.</param>
    public PerformanceGraphPanel(PerformanceSampler sampler)
    {
        _sampler = sampler;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        float totalWidth = DebugTheme.GraphWidth + GraphPaddingX * 2;
        float totalHeight = DebugTheme.GraphHeight + 120;

        SetAnchorsPreset(LayoutPreset.BottomLeft);
        GrowVertical = GrowDirection.Begin;
        CustomMinimumSize = new Vector2(totalWidth, totalHeight);
        Position = new Vector2(GraphPaddingX, -totalHeight - GraphPaddingY);
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer layout = new();
        layout.SetAnchorsPreset(LayoutPreset.FullRect);
        layout.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(layout);

        // Title
        Label title = new();
        title.Text = "--- Performance Graph ---";
        DebugTheme.ApplyLabelStyle(title, DebugTheme.TextAccent, DebugTheme.FontSizeSmall);
        layout.AddChild(title);

        // Graph area (uses _Draw on this Control)
        // Graph is drawn directly on this node

        // Stats line below graph
        _statsLabel = new Label();
        DebugTheme.ApplyLabelStyle(_statsLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        _statsLabel.Position = new Vector2(GraphPaddingX, DebugTheme.GraphHeight + 20);
        AddChild(_statsLabel);

        // Spike log
        _spikeLogLabel = new Label();
        DebugTheme.ApplyLabelStyle(_spikeLogLabel, DebugTheme.TextWarning, DebugTheme.FontSizeSmall);
        _spikeLogLabel.Position = new Vector2(GraphPaddingX, DebugTheme.GraphHeight + 50);
        AddChild(_spikeLogLabel);
    }

    /// <summary>
    /// Updates the graph display. Called by DebugManager from _Process.
    /// </summary>
    public void UpdateDisplay()
    {
        UpdateStatsLabel();
        UpdateSpikeLog();
        QueueRedraw();
    }

    /// <inheritdoc />
    public override void _Draw()
    {
        float graphX = GraphPaddingX;
        float graphY = 18f;
        float graphWidth = DebugTheme.GraphWidth;
        float graphHeight = DebugTheme.GraphHeight;

        // Background
        DrawRect(new Rect2(graphX, graphY, graphWidth, graphHeight), DebugTheme.GraphBackground);

        FrameTimeTracker tracker = _sampler.FrameTimeTracker;
        RingBuffer<double> buffer = tracker.Buffer;
        int sampleCount = buffer.Count;

        if (sampleCount < 2)
        {
            return;
        }

        // 60 FPS target line
        float targetY = graphY + graphHeight - (TargetFrameTimeMs / GraphScaleMax * graphHeight);
        DrawLine(
            new Vector2(graphX, targetY),
            new Vector2(graphX + graphWidth, targetY),
            DebugTheme.GraphTarget, 1f);

        // Frame time line graph
        float stepX = graphWidth / (sampleCount - 1);

        for (int i = 1; i < sampleCount; i++)
        {
            double prevValue = buffer.PeekAt(i - 1);
            double currValue = buffer.PeekAt(i);

            float prevY = graphY + graphHeight - ((float)prevValue / GraphScaleMax * graphHeight);
            float currY = graphY + graphHeight - ((float)currValue / GraphScaleMax * graphHeight);

            prevY = System.Math.Clamp(prevY, graphY, graphY + graphHeight);
            currY = System.Math.Clamp(currY, graphY, graphY + graphHeight);

            float prevX = graphX + (i - 1) * stepX;
            float currX = graphX + i * stepX;

            Color lineColor = currValue > TargetFrameTimeMs ? DebugTheme.GraphSpike : DebugTheme.GraphLine;
            DrawLine(new Vector2(prevX, prevY), new Vector2(currX, currY), lineColor, 1.5f);
        }

        // Border
        DrawRect(new Rect2(graphX, graphY, graphWidth, graphHeight),
            new Color(0.3f, 0.3f, 0.35f, 0.6f), false);
    }

    private void UpdateStatsLabel()
    {
        FrameTimeTracker tracker = _sampler.FrameTimeTracker;
        double fps = Engine.GetFramesPerSecond();

        _statsBuilder.Clear();
        _statsBuilder.Append("FPS: ").Append(fps.ToString("F0"))
            .Append("  Avg: ").Append(tracker.AverageFrameTimeMs.ToString("F2")).Append("ms")
            .Append("  99th: ").Append(tracker.GetPercentile(99).ToString("F2")).Append("ms")
            .Append("  Spikes: ").Append(_sampler.SpikeDetector.SpikeCount);

        _statsLabel.Text = _statsBuilder.ToString();
    }

    private void UpdateSpikeLog()
    {
        RingBuffer<SpikeRecord> history = _sampler.SpikeDetector.History;
        int count = history.Count;

        if (count == 0)
        {
            _spikeLogLabel.Text = "No spikes recorded.";
            return;
        }

        _spikeBuilder.Clear();
        _spikeBuilder.Append("Recent spikes:").AppendLine();

        int start = System.Math.Max(0, count - MaxSpikeLogEntries);

        for (int i = count - 1; i >= start; i--)
        {
            SpikeRecord spike = history.PeekAt(i);
            _spikeBuilder.Append("  #").Append(spike.FrameNumber)
                .Append(": ").Append(spike.FrameTimeMs.ToString("F1")).Append("ms").AppendLine();
        }

        _spikeLogLabel.Text = _spikeBuilder.ToString();
    }
}
#endif
