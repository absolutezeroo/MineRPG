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
/// Layout is defined in Scenes/UI/Debug/DebugHudPanel.tscn.
/// </summary>
public sealed partial class DebugHudPanel : Control
{
    private const int StringBuilderCapacity = 512;

    [Export] private Label _positionLabel = null!;
    [Export] private Label _worldLabel = null!;
    [Export] private Label _performanceLabel = null!;
    [Export] private Label _pipelineLabel = null!;
    [Export] private Label _rendererLabel = null!;
    [Export] private Label _memoryLabel = null!;

    private readonly StringBuilder _positionBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _worldBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _performanceBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _pipelineBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _rendererBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _memoryBuilder = new(StringBuilderCapacity);

    private IDebugDataProvider _debugData = null!;
    private PerformanceSampler _sampler = null!;
    private PerformanceMonitor _performanceMonitor = null!;
    private PipelineMetrics _pipelineMetrics = null!;
    private Camera3D? _camera;

    /// <summary>
    /// Injects dependencies after scene instantiation. Must be called before AddChild.
    /// </summary>
    /// <param name="debugData">Debug data provider for world/player info.</param>
    /// <param name="sampler">Performance sampler for frame timing.</param>
    /// <param name="performanceMonitor">Core performance metrics.</param>
    /// <param name="pipelineMetrics">Extended pipeline metrics.</param>
    public void SetDependencies(
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
}
#endif
