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
/// Uses Control base (not PanelContainer) so anchors control sizing directly.
/// </summary>
public sealed partial class DebugMenuPanel : Control
{
    private const int TabCount = 6;

    private readonly IDebugDataProvider _debugData;
    private readonly PerformanceSampler _sampler;
    private readonly PerformanceMonitor _performanceMonitor;
    private readonly PipelineMetrics _pipelineMetrics;
    private readonly OptimizationFlags _optimizationFlags;
    private readonly IEventBus _eventBus;
    private readonly IChunkDebugProvider? _chunkDebugProvider;

    private static readonly string[] TabNames =
    {
        "Rendering",
        "World",
        "Perf",
        "Biomes",
        "Entities",
        "System",
    };

    private Button[] _tabButtons = null!;
    private Control[] _tabContents = null!;
    private IDebugTab[] _tabs = null!;
    private StyleBoxFlat _tabActiveStyle = null!;
    private StyleBoxFlat _tabInactiveStyle = null!;
    private int _activeTab;

    private ILogger _logger = null!;
    private bool _sizeLogged;

    // Diagnostic references
    private PanelContainer _panel = null!;
    private VBoxContainer _root = null!;
    private ScrollContainer _scroll = null!;
    private VBoxContainer _contentArea = null!;

    /// <summary>
    /// Creates the debug menu panel.
    /// </summary>
    public DebugMenuPanel(
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

        // Size directly from viewport — parent Control under CanvasLayer has size 0
        Rect2 viewportRect = GetViewportRect();
        Position = Vector2.Zero;
        Size = new Vector2(DebugTheme.MenuPanelWidth, viewportRect.Size.Y);
        MouseFilter = MouseFilterEnum.Stop;
        GetViewport().SizeChanged += () =>
        {
            Size = new Vector2(DebugTheme.MenuPanelWidth, GetViewportRect().Size.Y);
        };

        // PanelContainer child: fills Control via FullRect, provides background + margins
        _panel = new PanelContainer();
        _panel.SetAnchorsPreset(LayoutPreset.FullRect);
        _panel.AddThemeStyleboxOverride("panel", DebugTheme.CreatePanelStyle());
        _panel.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_panel);

        // Root layout — managed by PanelContainer, NO anchors
        _root = new VBoxContainer();
        _root.AddThemeConstantOverride("separation", 4);
        _panel.AddChild(_root);

        // Title
        Label title = new();
        title.Text = "Debug Menu (F1)";
        DebugTheme.ApplyLabelStyle(title, DebugTheme.TextAccent, DebugTheme.FontSizeTitle);
        _root.AddChild(title);

        // Tab bar
        HBoxContainer tabBar = new();
        tabBar.AddThemeConstantOverride("separation", 2);
        _root.AddChild(tabBar);

        _tabActiveStyle = DebugTheme.CreateTabActiveStyle();
        _tabInactiveStyle = DebugTheme.CreateTabInactiveStyle();
        _tabButtons = new Button[TabCount];

        for (int i = 0; i < TabCount; i++)
        {
            Button tabButton = new();
            tabButton.Text = TabNames[i];
            tabButton.AddThemeFontSizeOverride("font_size", DebugTheme.FontSizeSmall);
            tabButton.AddThemeColorOverride("font_color", DebugTheme.TextPrimary);

            int tabIndex = i;
            tabButton.Pressed += () => SwitchToTab(tabIndex);

            tabBar.AddChild(tabButton);
            _tabButtons[i] = tabButton;
        }

        // Separator
        HSeparator separator = new();
        _root.AddChild(separator);

        // Scrollable content area (doc: ScrollContainer with single VBoxContainer child)
        _scroll = new ScrollContainer();
        _scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        _scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        _root.AddChild(_scroll);

        _contentArea = new VBoxContainer();
        _contentArea.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _contentArea.SizeFlagsVertical = SizeFlags.ExpandFill;
        _scroll.AddChild(_contentArea);

        _tabs = new IDebugTab[TabCount];
        _tabContents = new Control[TabCount];

        AddTab(_contentArea, 0, new RenderingTab(_optimizationFlags, _eventBus, _performanceMonitor));
        AddTab(_contentArea, 1, new WorldTab(_debugData, _performanceMonitor));
        AddTab(_contentArea, 2, new PerformanceTab(_sampler, _performanceMonitor, _pipelineMetrics));
        AddTab(_contentArea, 3, new BiomeTab(_debugData, _chunkDebugProvider));
        AddTab(_contentArea, 4, new EntitiesTab(_debugData));
        AddTab(_contentArea, 5, new SystemTab(_sampler, _performanceMonitor));

        SwitchToTab(0);
    }

    /// <summary>
    /// Updates the active tab display. Called by DebugManager.
    /// </summary>
    /// <param name="delta">Frame delta time in seconds.</param>
    public void UpdateDisplay(double delta)
    {
        LogDiagnosticSizes();
        _tabs[_activeTab].UpdateDisplay(delta);
    }

    private void AddTab(VBoxContainer parent, int index, Control tabControl)
    {
        tabControl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        tabControl.Visible = false;
        parent.AddChild(tabControl);

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

    private void LogDiagnosticSizes()
    {
        if (_sizeLogged)
        {
            return;
        }

        _sizeLogged = true;
        Control? parentControl = GetParent() as Control;
        _logger.Debug(
            "DebugMenuPanel DIAG: Parent={0} ParentSize={1} Viewport={2}",
            GetParent().Name, parentControl?.Size ?? Vector2.Zero, GetViewportRect().Size);
        _logger.Debug(
            "DebugMenuPanel DIAG: Self={0} Panel={1} Root={2}",
            Size, _panel.Size, _root.Size);
        _logger.Debug(
            "DebugMenuPanel DIAG: Scroll={0} Content={1} Children={2}",
            _scroll.Size, _contentArea.Size, _contentArea.GetChildCount());
        _logger.Debug(
            "DebugMenuPanel DIAG: Tab0.Vis={0} Tab0.Size={1} Tab0.Children={2}",
            _tabContents[0].Visible, _tabContents[0].Size,
            ((Node)_tabContents[0]).GetChildCount());
    }
}
#endif
