#if DEBUG
using System;
using System.Text;

using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.Interfaces.Gameplay;

namespace MineRPG.Godot.UI.Debug;

/// <summary>
/// Enhanced F3 debug HUD replacing the old DebugOverlayNode.
/// Six sections: Position, World, Performance, Pipeline, Renderer, and Memory.
/// Pre-allocated StringBuilders, zero GC in _Process hot path.
/// </summary>
public sealed partial class DebugHudPanel : Control
{
    private const int StringBuilderCapacity = 512;
    private const double MillisecondsPerSecond = 1000.0;
    private const double BytesPerMegabyte = 1024.0 * 1024.0;

    private readonly IDebugDataProvider _debugData;
    private readonly PerformanceSampler _sampler;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly PipelineMetrics _pipelineMetrics;

    private readonly StringBuilder _positionBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _worldBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _performanceBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _pipelineBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _rendererBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _memoryBuilder = new(StringBuilderCapacity);

    private Label _positionLabel = null!;
    private Label _worldLabel = null!;
    private Label _performanceLabel = null!;
    private Label _pipelineLabel = null!;
    private Label _rendererLabel = null!;
    private Label _memoryLabel = null!;

    private Camera3D? _camera;

    /// <summary>
    /// Creates a new DebugHudPanel with all required data sources.
    /// </summary>
    /// <param name="debugData">Debug data provider for world/player info.</param>
    /// <param name="sampler">Performance sampler for frame timing.</param>
    /// <param name="performanceMonitor">Core performance metrics.</param>
    /// <param name="pipelineMetrics">Extended pipeline metrics.</param>
    public DebugHudPanel(
        IDebugDataProvider debugData,
        PerformanceSampler sampler,
        PerformanceMonitor performanceMonitor,
        PipelineMetrics pipelineMetrics)
    {
        _debugData = debugData;
        _sampler = sampler;
        _performanceMonitor = performanceMonitor;
        _pipelineMetrics = pipelineMetrics;
    }

    /// <summary>
    /// Sets the camera reference for look direction display.
    /// </summary>
    /// <param name="camera">The active 3D camera.</param>
    public void SetCamera(Camera3D? camera) => _camera = camera;

    /// <inheritdoc />
    public override void _Ready()
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer leftColumn = new();
        leftColumn.SetAnchorsPreset(LayoutPreset.TopLeft);
        leftColumn.Position = new Vector2(DebugTheme.PanelPaddingX, DebugTheme.PanelPaddingY);
        leftColumn.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(leftColumn);

        VBoxContainer rightColumn = new();
        rightColumn.SetAnchorsPreset(LayoutPreset.TopRight);
        rightColumn.GrowHorizontal = GrowDirection.Begin;
        rightColumn.Position = new Vector2(-DebugTheme.PanelPaddingX, DebugTheme.PanelPaddingY);
        rightColumn.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(rightColumn);

        // Left column: Position, World, Performance
        _positionLabel = CreateSection(leftColumn, "Position");
        _worldLabel = CreateSection(leftColumn, "World");
        _performanceLabel = CreateSection(leftColumn, "Performance");

        // Right column: Pipeline, Renderer, Memory
        _pipelineLabel = CreateSection(rightColumn, "Pipeline");
        _rendererLabel = CreateSection(rightColumn, "Renderer");
        _memoryLabel = CreateSection(rightColumn, "Memory");
    }

    /// <summary>
    /// Updates all six sections. Called by DebugManager from _Process.
    /// </summary>
    /// <param name="delta">Frame delta in seconds (unused here but available).</param>
    public void UpdateDisplay(double delta)
    {
        UpdatePositionSection();
        UpdateWorldSection();
        UpdatePerformanceSection();
        UpdatePipelineSection();
        UpdateRendererSection();
        UpdateMemorySection();
    }

    private void UpdatePositionSection()
    {
        float playerX = _debugData.PlayerX;
        float playerY = _debugData.PlayerY;
        float playerZ = _debugData.PlayerZ;

        Vector3 lookDirection = _camera is not null && _camera.IsInsideTree()
            ? -_camera.GlobalTransform.Basis.Z
            : Vector3.Zero;

        float yaw = MathF.Atan2(-lookDirection.X, -lookDirection.Z) * 180f / MathF.PI;
        float pitch = MathF.Asin(lookDirection.Y) * 180f / MathF.PI;

        string facing = GetCardinalDirection(yaw);

        _positionBuilder.Clear();
        _positionBuilder.Append("XYZ: ")
            .Append(playerX.ToString("F3")).Append(" / ")
            .Append(playerY.ToString("F3")).Append(" / ")
            .Append(playerZ.ToString("F3")).AppendLine();
        _positionBuilder.Append("Block: ")
            .Append((int)MathF.Floor(playerX)).Append(' ')
            .Append((int)MathF.Floor(playerY)).Append(' ')
            .Append((int)MathF.Floor(playerZ)).AppendLine();
        _positionBuilder.Append("Chunk: ")
            .Append(_debugData.ChunkX).Append(' ')
            .Append(_debugData.ChunkZ).AppendLine();
        _positionBuilder.Append("Facing: ").Append(facing)
            .Append(" (").Append(yaw.ToString("F1")).Append(" / ")
            .Append(pitch.ToString("F1")).Append(')');

        _positionLabel.Text = _positionBuilder.ToString();
    }

    private void UpdateWorldSection()
    {
        _worldBuilder.Clear();
        _worldBuilder.Append("Biome: ").Append(_debugData.CurrentBiome).AppendLine();
        _worldBuilder.Append("Chunks loaded: ").Append(_debugData.LoadedChunkCount).AppendLine();
        _worldBuilder.Append("Chunks visible: ").Append(_debugData.VisibleChunkCount).AppendLine();
        _worldBuilder.Append("Chunks queued: ").Append(_debugData.ChunksInQueue).AppendLine();
        _worldBuilder.Append("Render distance: ").Append(_debugData.RenderDistance);

        _worldLabel.Text = _worldBuilder.ToString();
    }

    private void UpdatePerformanceSection()
    {
        double framesPerSecond = Engine.GetFramesPerSecond();
        double frameTimeMs = framesPerSecond > 0
            ? MillisecondsPerSecond / framesPerSecond
            : 0;

        FrameTimeTracker tracker = _sampler.FrameTimeTracker;

        _performanceBuilder.Clear();
        _performanceBuilder.Append("FPS: ").Append(framesPerSecond)
            .Append(" (").Append(frameTimeMs.ToString("F1")).Append(" ms)").AppendLine();
        _performanceBuilder.Append("Avg: ").Append(tracker.AverageFrameTimeMs.ToString("F2"))
            .Append(" ms").AppendLine();
        _performanceBuilder.Append("Min: ").Append(tracker.MinFrameTimeMs.ToString("F2"))
            .Append("  Max: ").Append(tracker.MaxFrameTimeMs.ToString("F2")).AppendLine();
        _performanceBuilder.Append("1% low: ")
            .Append(tracker.GetPercentile(99).ToString("F2")).Append(" ms").AppendLine();
        _performanceBuilder.Append("Spikes: ").Append(_sampler.SpikeDetector.SpikeCount);

        _performanceLabel.Text = _performanceBuilder.ToString();
    }

    private void UpdatePipelineSection()
    {
        _pipelineBuilder.Clear();
        _pipelineBuilder.Append("Gen queue: ").Append(_pipelineMetrics.GenerationQueueSize).AppendLine();
        _pipelineBuilder.Append("Remesh queue: ").Append(_pipelineMetrics.RemeshQueueSize).AppendLine();
        _pipelineBuilder.Append("Save queue: ").Append(_pipelineMetrics.SaveQueueSize).AppendLine();
        _pipelineBuilder.Append("Workers: ").Append(_pipelineMetrics.ActiveWorkerCount)
            .Append('/').Append(_pipelineMetrics.TotalWorkerCount).AppendLine();
        _pipelineBuilder.Append("Gen avg: ")
            .Append(_pipelineMetrics.AverageGenerationTimeMs.ToString("F2")).Append(" ms").AppendLine();
        _pipelineBuilder.Append("Mesh avg: ")
            .Append(_performanceMonitor.AverageMeshTimeMs.ToString("F2")).Append(" ms").AppendLine();
        _pipelineBuilder.Append("Drain avg: ")
            .Append(_pipelineMetrics.AverageDrainTimeMs.ToString("F2")).Append(" ms");

        _pipelineLabel.Text = _pipelineBuilder.ToString();
    }

    private void UpdateRendererSection()
    {
        ulong drawCalls = RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalObjectsInFrame);
        ulong primitives = RenderingServer.GetRenderingInfo(
            RenderingServer.RenderingInfo.TotalPrimitivesInFrame);

        _rendererBuilder.Clear();
        _rendererBuilder.Append("Draw calls: ").Append(drawCalls).AppendLine();
        _rendererBuilder.Append("Primitives: ").Append(primitives).AppendLine();
        _rendererBuilder.Append("Vertices: ").Append(_performanceMonitor.TotalVertices).AppendLine();
        _rendererBuilder.Append("Pool: ").Append(_performanceMonitor.PoolActiveCount)
            .Append(" active / ").Append(_performanceMonitor.PoolIdleCount).Append(" idle");

        _rendererLabel.Text = _rendererBuilder.ToString();
    }

    private void UpdateMemorySection()
    {
        MemoryMetrics memory = _sampler.MemoryMetrics;
        ulong staticMem = OS.GetStaticMemoryUsage();
        double staticMb = staticMem / BytesPerMegabyte;

        _memoryBuilder.Clear();
        _memoryBuilder.Append("GC Heap: ").Append(memory.GcHeapMb.ToString("F1")).Append(" MB").AppendLine();
        _memoryBuilder.Append("Static: ").Append(staticMb.ToString("F1")).Append(" MB").AppendLine();
        _memoryBuilder.Append("Chunk data: ~").Append(memory.EstimatedChunkDataMb.ToString("F1")).Append(" MB").AppendLine();
        _memoryBuilder.Append("Mesh data: ~").Append(memory.EstimatedMeshDataMb.ToString("F1")).Append(" MB").AppendLine();
        _memoryBuilder.Append("GC: ")
            .Append(memory.Gen0Collections).Append('/').Append(memory.Gen1Collections)
            .Append('/').Append(memory.Gen2Collections).Append(" (G0/G1/G2)");

        _memoryLabel.Text = _memoryBuilder.ToString();
    }

    private static Label CreateSection(VBoxContainer column, string headerText)
    {
        PanelContainer panel = new();
        panel.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddThemeStyleboxOverride("panel", DebugTheme.CreatePanelStyle());
        column.AddChild(panel);

        VBoxContainer content = new();
        content.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddChild(content);

        Label header = new();
        header.Text = $"--- {headerText} ---";
        DebugTheme.ApplyLabelStyle(header, DebugTheme.TextAccent, DebugTheme.FontSizeNormal);
        content.AddChild(header);

        Label dataLabel = new();
        DebugTheme.ApplyLabelStyle(dataLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        content.AddChild(dataLabel);

        // Spacer between sections
        Control spacer = new();
        spacer.CustomMinimumSize = new Vector2(0, DebugTheme.SectionSpacing);
        spacer.MouseFilter = MouseFilterEnum.Ignore;
        column.AddChild(spacer);

        return dataLabel;
    }

    private static string GetCardinalDirection(float yaw)
    {
        float normalized = ((yaw % 360f) + 360f) % 360f;

        if (normalized >= 337.5f || normalized < 22.5f)
        {
            return "South (+Z)";
        }

        if (normalized < 67.5f)
        {
            return "Southwest";
        }

        if (normalized < 112.5f)
        {
            return "West (-X)";
        }

        if (normalized < 157.5f)
        {
            return "Northwest";
        }

        if (normalized < 202.5f)
        {
            return "North (-Z)";
        }

        if (normalized < 247.5f)
        {
            return "Northeast";
        }

        if (normalized < 292.5f)
        {
            return "East (+X)";
        }

        return "Southeast";
    }
}
#endif
