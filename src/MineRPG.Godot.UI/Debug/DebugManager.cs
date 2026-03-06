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
/// Delegates input to <see cref="DebugInputHandler"/> and panel creation to
/// <see cref="DebugModuleFactory"/>.
/// </summary>
public sealed partial class DebugManager : Control
{
    private ILogger _logger = null!;
    private IEventBus _eventBus = null!;
    private PerformanceMonitor _performanceMonitor = null!;
    private OptimizationFlags _optimizationFlags = null!;

    private Camera3D? _camera;
    private DebugInputHandler _inputHandler = null!;
    private DebugModuleFactory _factory = null!;

    private DebugMenuPanel? _debugMenuPanel;
    private DebugHudPanel? _hudPanel;
    private ChunkMapPanel? _chunkMapPanel;
    private BlockInspectorPanel? _blockInspectorPanel;
    private PerformanceGraphPanel? _perfGraphPanel;
    private BiomeOverlayPanel? _biomeOverlayPanel;

    private global::Godot.Environment? _cachedEnvironment;
    private bool _chunkBorderVisible;
    private bool _anyModuleVisible;

    /// <summary>The performance sampler owned by this manager.</summary>
    public PerformanceSampler Sampler { get; private set; } = null!;

    /// <summary>Sets the camera reference for modules that need look direction.</summary>
    /// <param name="camera">The active 3D camera.</param>
    public void SetCamera(Camera3D camera) => _camera = camera;

    /// <inheritdoc />
    public override void _Ready()
    {
        IServiceLocator locator = ServiceLocator.Instance;
        _logger = locator.Get<ILogger>();
        _eventBus = locator.Get<IEventBus>();
        IDebugDataProvider debugData = locator.Get<IDebugDataProvider>();
        _performanceMonitor = locator.Get<PerformanceMonitor>();
        PipelineMetrics pipelineMetrics = locator.Get<PipelineMetrics>();
        _optimizationFlags = locator.Get<OptimizationFlags>();

        locator.TryGet(out IChunkDebugProvider? chunkDebugProvider);

        Sampler = new PerformanceSampler();

        _inputHandler = new DebugInputHandler(this, _logger);
        _factory = new DebugModuleFactory(
            debugData, Sampler, _performanceMonitor, pipelineMetrics,
            _optimizationFlags, _eventBus, chunkDebugProvider, _logger);

        Size = GetViewportRect().Size;
        GetViewport().SizeChanged += OnViewportSizeChanged;
        MouseFilter = MouseFilterEnum.Ignore;

        _eventBus.Subscribe<OptimizationFlagChangedEvent>(OnOptimizationFlagChanged);
        _logger.Debug("DebugManager ready.");
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        _eventBus?.Unsubscribe<OptimizationFlagChangedEvent>(OnOptimizationFlagChanged);
    }

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        _inputHandler.HandleInput(@event, GetViewport());
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
        Sampler.Sample(delta, breakdown);
        Sampler.UpdateResourceMetrics(_performanceMonitor.ActiveChunks, _performanceMonitor.TotalVertices);

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
            _debugMenuPanel.UpdateDisplay(delta);
        }
    }

    internal void ToggleDebugMenu()
    {
        if (_debugMenuPanel is null)
        {
            _debugMenuPanel = _factory.CreateDebugMenuPanel();
            AddChild(_debugMenuPanel);
            Input.MouseMode = Input.MouseModeEnum.Visible;
            return;
        }

        _debugMenuPanel.Visible = !_debugMenuPanel.Visible;

        if (_debugMenuPanel.Visible)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else if (!AnyPanelNeedsMouse())
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    internal void ToggleHudPanel()
    {
        if (_hudPanel is null)
        {
            _hudPanel = _factory.CreateHudPanel(_camera);
            AddChild(_hudPanel);
            return;
        }

        _hudPanel.Visible = !_hudPanel.Visible;
    }

    internal void ToggleChunkMapPanel()
    {
        if (_chunkMapPanel is null)
        {
            _chunkMapPanel = _factory.CreateChunkMapPanel();
            AddChild(_chunkMapPanel);
            return;
        }

        _chunkMapPanel.Visible = !_chunkMapPanel.Visible;
    }

    internal void ToggleBiomeOverlay()
    {
        if (_biomeOverlayPanel is null)
        {
            _biomeOverlayPanel = _factory.CreateBiomeOverlayPanel();
            AddChild(_biomeOverlayPanel);
            return;
        }

        _biomeOverlayPanel.Visible = !_biomeOverlayPanel.Visible;
    }

    internal void CycleBiomeOverlayMode()
    {
        if (_biomeOverlayPanel is null)
        {
            _biomeOverlayPanel = _factory.CreateBiomeOverlayPanel();
            AddChild(_biomeOverlayPanel);
        }

        _biomeOverlayPanel.Visible = true;
        _biomeOverlayPanel.CycleMode();
    }

    internal void TogglePerfGraph()
    {
        if (_perfGraphPanel is null)
        {
            _perfGraphPanel = _factory.CreatePerfGraphPanel();
            AddChild(_perfGraphPanel);
            return;
        }

        _perfGraphPanel.Visible = !_perfGraphPanel.Visible;
    }

    internal void ToggleChunkBorder()
    {
        _chunkBorderVisible = !_chunkBorderVisible;

        _eventBus.Publish(new DebugToggleEvent
        {
            ModuleKey = "chunk_border",
            Visible = _chunkBorderVisible,
        });
    }

    internal void ToggleBlockInspector()
    {
        if (_blockInspectorPanel is null)
        {
            _blockInspectorPanel = _factory.CreateBlockInspectorPanel(_camera);
            AddChild(_blockInspectorPanel);
            return;
        }

        _blockInspectorPanel.Visible = !_blockInspectorPanel.Visible;
    }

    private bool AnyPanelNeedsMouse()
    {
        return (_chunkMapPanel is not null && _chunkMapPanel.Visible) ||
               (_blockInspectorPanel is not null && _blockInspectorPanel.Visible);
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

    private void OnViewportSizeChanged() => Size = GetViewportRect().Size;

    private void OnOptimizationFlagChanged(OptimizationFlagChangedEvent evt) => ApplyRenderingFlags();

    private void ApplyRenderingFlags()
    {
        Viewport viewport = GetViewport();
        viewport.DebugDraw = _optimizationFlags.WireframeModeEnabled
            ? Viewport.DebugDrawEnum.Wireframe
            : Viewport.DebugDrawEnum.Disabled;

        if (_cachedEnvironment is null)
        {
            Node? worldEnv = GetTree().Root.FindChild("WorldEnvironment", true, false);

            if (worldEnv is WorldEnvironment env)
            {
                _cachedEnvironment = env.Environment;
            }
        }

        if (_cachedEnvironment is not null)
        {
            _cachedEnvironment.VolumetricFogEnabled = _optimizationFlags.FogEnabled;
        }
    }
}
#endif
