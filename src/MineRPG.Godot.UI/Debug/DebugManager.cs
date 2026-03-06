#if DEBUG
using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Diagnostics;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.Debug;

/// <summary>
/// Central coordinator for all debug modules. Owns the <see cref="PerformanceSampler"/>
/// and lazily instantiates debug panels on first toggle.
/// Added as a child of HUDNode (CanvasLayer). All toggles happen via _Input.
/// </summary>
public sealed partial class DebugManager : Control
{
    private ILogger _logger = null!;
    private IDebugDataProvider _debugData = null!;
    private IEventBus _eventBus = null!;
    private PerformanceMonitor _performanceMonitor = null!;
    private PipelineMetrics _pipelineMetrics = null!;
    private OptimizationFlags _optimizationFlags = null!;

    private PerformanceSampler _sampler = null!;
    private Camera3D? _camera;

    // Lazily created modules — null until first toggle
    private DebugMenuPanel? _debugMenuPanel;
    private DebugHudPanel? _hudPanel;
    private ChunkMapPanel? _chunkMapPanel;
    private BlockInspectorPanel? _blockInspectorPanel;
    private PerformanceGraphPanel? _perfGraphPanel;
    private BiomeOverlayPanel? _biomeOverlayPanel;

    private IChunkDebugProvider? _chunkDebugProvider;
    private bool _chunkBorderVisible;
    private bool _anyModuleVisible;

    /// <summary>
    /// The performance sampler owned by this manager.
    /// </summary>
    public PerformanceSampler Sampler => _sampler;

    /// <summary>
    /// Sets the camera reference for modules that need look direction.
    /// </summary>
    /// <param name="camera">The active 3D camera.</param>
    public void SetCamera(Camera3D camera) => _camera = camera;

    /// <inheritdoc />
    public override void _Ready()
    {
        IServiceLocator locator = ServiceLocator.Instance;
        _logger = locator.Get<ILogger>();
        _eventBus = locator.Get<IEventBus>();
        _debugData = locator.Get<IDebugDataProvider>();
        _performanceMonitor = locator.Get<PerformanceMonitor>();
        _pipelineMetrics = locator.Get<PipelineMetrics>();
        _optimizationFlags = locator.Get<OptimizationFlags>();

        locator.TryGet(out _chunkDebugProvider);

        _sampler = new PerformanceSampler();

        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        _logger.Debug("DebugManager ready.");
    }

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed(InputActionNames.DebugMenu))
        {
            ToggleDebugMenu();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputActionNames.DebugHud))
        {
            ToggleHudPanel();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputActionNames.DebugChunkMap))
        {
            ToggleChunkMapPanel();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputActionNames.DebugChunkBorder))
        {
            ToggleChunkBorder();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputActionNames.DebugPerfGraph))
        {
            TogglePerfGraph();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputActionNames.DebugBiomeOverlay))
        {
            ToggleBiomeOverlay();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputActionNames.DebugBiomeOverlayMode))
        {
            CycleBiomeOverlayMode();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (@event.IsActionPressed(InputActionNames.DebugBlockInspector))
        {
            ToggleBlockInspector();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        UpdateAnyModuleVisible();

        if (!_anyModuleVisible)
        {
            return;
        }

        FrameTimeBreakdown breakdown = new();
        _sampler.Sample(delta, breakdown);

        _sampler.UpdateResourceMetrics(
            _performanceMonitor.ActiveChunks,
            _performanceMonitor.TotalVertices);

        if (_hudPanel is not null && _hudPanel.Visible)
        {
            _hudPanel.UpdateDisplay(delta);
        }

        if (_chunkMapPanel is not null && _chunkMapPanel.Visible)
        {
            _chunkMapPanel.UpdateDisplay();
        }

        if (_blockInspectorPanel is not null && _blockInspectorPanel.Visible)
        {
            _blockInspectorPanel.UpdateDisplay();
        }

        if (_perfGraphPanel is not null && _perfGraphPanel.Visible)
        {
            _perfGraphPanel.UpdateDisplay();
        }

        if (_biomeOverlayPanel is not null && _biomeOverlayPanel.Visible)
        {
            _biomeOverlayPanel.UpdateDisplay();
        }

        if (_debugMenuPanel is not null && _debugMenuPanel.Visible)
        {
            _debugMenuPanel.UpdateDisplay();
        }
    }

    private void ToggleDebugMenu()
    {
        if (_debugMenuPanel is null)
        {
            _debugMenuPanel = new DebugMenuPanel(
                _debugData, _sampler, _performanceMonitor,
                _pipelineMetrics, _optimizationFlags, _eventBus, _chunkDebugProvider);
            _debugMenuPanel.Name = "DebugMenuPanel";
            AddChild(_debugMenuPanel);
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _logger.Debug("DebugManager: DebugMenuPanel created.");
            return;
        }

        _debugMenuPanel.Visible = !_debugMenuPanel.Visible;

        if (_debugMenuPanel.Visible)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        _logger.Debug("DebugManager: DebugMenuPanel toggled, Visible={0}", _debugMenuPanel.Visible);
    }

    private void ToggleHudPanel()
    {
        if (_hudPanel is null)
        {
            _hudPanel = new DebugHudPanel(_debugData, _sampler, _performanceMonitor, _pipelineMetrics);
            _hudPanel.Name = "DebugHudPanel";
            _hudPanel.SetCamera(_camera);
            AddChild(_hudPanel);
            _logger.Debug("DebugManager: DebugHudPanel created.");
            return;
        }

        _hudPanel.Visible = !_hudPanel.Visible;
        _logger.Debug("DebugManager: DebugHudPanel toggled, Visible={0}", _hudPanel.Visible);
    }

    private void ToggleChunkMapPanel()
    {
        if (_chunkMapPanel is null)
        {
            _chunkMapPanel = new ChunkMapPanel(_debugData, _chunkDebugProvider);
            _chunkMapPanel.Name = "ChunkMapPanel";
            AddChild(_chunkMapPanel);
            _logger.Debug("DebugManager: ChunkMapPanel created.");
            return;
        }

        _chunkMapPanel.Visible = !_chunkMapPanel.Visible;
        _logger.Debug("DebugManager: ChunkMapPanel toggled, Visible={0}", _chunkMapPanel.Visible);
    }

    private void ToggleBiomeOverlay()
    {
        if (_biomeOverlayPanel is null)
        {
            _biomeOverlayPanel = new BiomeOverlayPanel(_debugData, _chunkDebugProvider);
            _biomeOverlayPanel.Name = "BiomeOverlayPanel";
            AddChild(_biomeOverlayPanel);
            _logger.Debug("DebugManager: BiomeOverlayPanel created.");
            return;
        }

        _biomeOverlayPanel.Visible = !_biomeOverlayPanel.Visible;
        _logger.Debug("DebugManager: BiomeOverlayPanel toggled, Visible={0}", _biomeOverlayPanel.Visible);
    }

    private void CycleBiomeOverlayMode()
    {
        if (_biomeOverlayPanel is null)
        {
            // Auto-create and show when cycling mode
            _biomeOverlayPanel = new BiomeOverlayPanel(_debugData, _chunkDebugProvider);
            _biomeOverlayPanel.Name = "BiomeOverlayPanel";
            AddChild(_biomeOverlayPanel);
        }

        _biomeOverlayPanel.Visible = true;
        _biomeOverlayPanel.CycleMode();
        _logger.Debug("DebugManager: BiomeOverlayPanel mode cycled.");
    }

    private void TogglePerfGraph()
    {
        if (_perfGraphPanel is null)
        {
            _perfGraphPanel = new PerformanceGraphPanel(_sampler);
            _perfGraphPanel.Name = "PerformanceGraphPanel";
            AddChild(_perfGraphPanel);
            _logger.Debug("DebugManager: PerformanceGraphPanel created.");
            return;
        }

        _perfGraphPanel.Visible = !_perfGraphPanel.Visible;
        _logger.Debug("DebugManager: PerformanceGraphPanel toggled, Visible={0}", _perfGraphPanel.Visible);
    }

    private void ToggleChunkBorder()
    {
        _chunkBorderVisible = !_chunkBorderVisible;

        _eventBus.Publish(new DebugToggleEvent
        {
            ModuleKey = "chunk_border",
            Visible = _chunkBorderVisible,
        });

        _logger.Debug("DebugManager: ChunkBorder toggled, Visible={0}", _chunkBorderVisible);
    }

    private void ToggleBlockInspector()
    {
        if (_blockInspectorPanel is null)
        {
            _blockInspectorPanel = new BlockInspectorPanel();
            _blockInspectorPanel.Name = "BlockInspectorPanel";
            _blockInspectorPanel.SetCamera(_camera);
            AddChild(_blockInspectorPanel);
            _logger.Debug("DebugManager: BlockInspectorPanel created.");
            return;
        }

        _blockInspectorPanel.Visible = !_blockInspectorPanel.Visible;
        _logger.Debug("DebugManager: BlockInspectorPanel toggled, Visible={0}", _blockInspectorPanel.Visible);
    }

    private void UpdateAnyModuleVisible()
    {
        _anyModuleVisible =
            (_debugMenuPanel is not null && _debugMenuPanel.Visible) ||
            (_hudPanel is not null && _hudPanel.Visible) ||
            (_chunkMapPanel is not null && _chunkMapPanel.Visible) ||
            (_blockInspectorPanel is not null && _blockInspectorPanel.Visible) ||
            (_perfGraphPanel is not null && _perfGraphPanel.Visible) ||
            (_biomeOverlayPanel is not null && _biomeOverlayPanel.Visible);
    }
}
#endif
