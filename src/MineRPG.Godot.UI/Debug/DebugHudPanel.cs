#if DEBUG
using System.Text;

using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.Interfaces.Gameplay;

namespace MineRPG.Godot.UI.Debug;

/// <summary>
/// Enhanced F3 debug HUD. Six sections: Position, World, Performance,
/// Pipeline, Renderer, and Memory. Text formatting is delegated to
/// <see cref="DebugHudFormatter"/>.
/// </summary>
public sealed partial class DebugHudPanel : Control
{
    private const int StringBuilderCapacity = 512;

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

    /// <summary>Sets the camera reference for look direction display.</summary>
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

        _positionLabel = CreateSection(leftColumn, "Position");
        _worldLabel = CreateSection(leftColumn, "World");
        _performanceLabel = CreateSection(leftColumn, "Performance");

        _pipelineLabel = CreateSection(rightColumn, "Pipeline");
        _rendererLabel = CreateSection(rightColumn, "Renderer");
        _memoryLabel = CreateSection(rightColumn, "Memory");
    }

    /// <summary>
    /// Updates all six sections. Called by DebugManager from _Process.
    /// </summary>
    /// <param name="delta">Frame delta in seconds.</param>
    public void UpdateDisplay(double delta)
    {
        DebugHudFormatter.FormatPositionSection(_positionBuilder, _debugData, _camera);
        _positionLabel.Text = _positionBuilder.ToString();

        DebugHudFormatter.FormatWorldSection(_worldBuilder, _debugData);
        _worldLabel.Text = _worldBuilder.ToString();

        DebugHudFormatter.FormatPerformanceSection(_performanceBuilder, _sampler);
        _performanceLabel.Text = _performanceBuilder.ToString();

        DebugHudFormatter.FormatPipelineSection(_pipelineBuilder, _pipelineMetrics, _performanceMonitor);
        _pipelineLabel.Text = _pipelineBuilder.ToString();

        DebugHudFormatter.FormatRendererSection(_rendererBuilder, _performanceMonitor);
        _rendererLabel.Text = _rendererBuilder.ToString();

        DebugHudFormatter.FormatMemorySection(_memoryBuilder, _sampler);
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

        Control spacer = new();
        spacer.CustomMinimumSize = new Vector2(0, DebugTheme.SectionSpacing);
        spacer.MouseFilter = MouseFilterEnum.Ignore;
        column.AddChild(spacer);

        return dataLabel;
    }
}
#endif
