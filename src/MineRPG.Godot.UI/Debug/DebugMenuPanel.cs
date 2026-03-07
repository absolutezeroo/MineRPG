#if DEBUG
using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Diagnostics;
using MineRPG.Core.Events;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;
using MineRPG.Godot.UI.Debug.Tabs;

namespace MineRPG.Godot.UI.Debug;

/// <summary>
/// F1 debug menu panel. Displays a side panel with tabbed content for
/// rendering toggles, world controls, performance metrics, biome info,
/// entity controls, and system information.
/// Layout is defined in Scenes/UI/Debug/DebugMenuPanel.tscn.
/// </summary>
public sealed partial class DebugMenuPanel : Control
{
    private const int TabCount = 6;

    [Export] private PanelContainer _panel = null!;
    [Export] private Label _titleLabel = null!;
    [Export] private HBoxContainer _tabBar = null!;
    [Export] private ScrollContainer _scrollContainer = null!;
    [Export] private VBoxContainer _contentArea = null!;

    private IDebugDataProvider _debugData = null!;
    private PerformanceSampler _sampler = null!;
    private PerformanceMonitor _performanceMonitor = null!;
    private PipelineMetrics _pipelineMetrics = null!;
    private OptimizationFlags _optimizationFlags = null!;
    private IEventBus _eventBus = null!;
    private IChunkDebugProvider? _chunkDebugProvider;

    private Button[] _tabButtons = null!;
    private Control[] _tabContents = null!;
    private IDebugTab[] _tabs = null!;
    private StyleBoxFlat _tabActiveStyle = null!;
    private StyleBoxFlat _tabInactiveStyle = null!;
    private int _activeTab;

    private ILogger _logger = null!;

    /// <summary>
    /// Injects dependencies after scene instantiation. Must be called before AddChild.
    /// </summary>
    public void SetDependencies(
        IDebugDataProvider debugData,
        PerformanceSampler sampler,
        PerformanceMonitor performanceMonitor,
        PipelineMetrics pipelineMetrics,
        OptimizationFlags optimizationFlags,
        IEventBus eventBus,
        IChunkDebugProvider? chunkDebugProvider)
    {
        _debugData = debugData;
        _sampler = sampler;
        _performanceMonitor = performanceMonitor;
        _pipelineMetrics = pipelineMetrics;
        _optimizationFlags = optimizationFlags;
        _eventBus = eventBus;
        _chunkDebugProvider = chunkDebugProvider;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        _logger = ServiceLocator.Instance.Get<ILogger>();

        // [Export] node references may not auto-resolve from NodePath;
        // fallback to GetNode for reliable resolution.
        _panel ??= GetNode<PanelContainer>("Panel");
        _titleLabel ??= GetNode<Label>("Panel/Root/Title");
        _tabBar ??= GetNode<HBoxContainer>("Panel/Root/TabBar");
        _scrollContainer ??= GetNode<ScrollContainer>("Panel/Root/ScrollContainer");
        _contentArea ??= GetNode<VBoxContainer>("Panel/Root/ScrollContainer/ContentArea");

        // Size directly from viewport — parent Control under CanvasLayer has size 0
        Rect2 viewportRect = GetViewportRect();
        Position = Vector2.Zero;
        Size = new Vector2(DebugTheme.MenuPanelWidth, viewportRect.Size.Y);
        MouseFilter = MouseFilterEnum.Stop;
        GetViewport().SizeChanged += () =>
        {
            Size = new Vector2(DebugTheme.MenuPanelWidth, GetViewportRect().Size.Y);
        };

        // Collect tab buttons from scene
        _tabActiveStyle = DebugTheme.CreateTabActiveStyle();
        _tabInactiveStyle = DebugTheme.CreateTabInactiveStyle();
        _tabButtons = new Button[TabCount];

        for (int i = 0; i < TabCount; i++)
        {
            Button tabButton = _tabBar.GetChild<Button>(i);
            int tabIndex = i;
            tabButton.Pressed += () => SwitchToTab(tabIndex);
            _tabButtons[i] = tabButton;
        }

        // Create tab contents
        _tabs = new IDebugTab[TabCount];
        _tabContents = new Control[TabCount];

        AddTab(0, new RenderingTab(_optimizationFlags, _eventBus, _performanceMonitor));
        AddTab(1, new WorldTab(_debugData, _performanceMonitor));
        AddTab(2, new PerformanceTab(_sampler, _performanceMonitor, _pipelineMetrics));
        AddTab(3, new BiomeTab(_debugData, _chunkDebugProvider));
        AddTab(4, new EntitiesTab(_debugData));
        AddTab(5, new SystemTab(_sampler, _performanceMonitor));

        SwitchToTab(0);
    }

    /// <summary>
    /// Updates the active tab display. Called by DebugManager.
    /// </summary>
    /// <param name="delta">Frame delta time in seconds.</param>
    public void UpdateDisplay(double delta)
    {
        _tabs[_activeTab].UpdateDisplay(delta);
    }

    private void AddTab(int index, Control tabControl)
    {
        tabControl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        tabControl.Visible = false;
        _contentArea.AddChild(tabControl);

        _tabContents[index] = tabControl;
        _tabs[index] = (IDebugTab)tabControl;
    }

    private void SwitchToTab(int index)
    {
        _activeTab = index;

        for (int i = 0; i < TabCount; i++)
        {
            _tabContents[i].Visible = i == index;
            _tabButtons[i].AddThemeStyleboxOverride(
                "normal",
                i == index ? _tabActiveStyle : _tabInactiveStyle);
        }
    }
}
#endif
