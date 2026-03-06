#if DEBUG
using Godot;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.Events;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Godot.UI.Debug.Tabs;

namespace MineRPG.Godot.UI.Debug;

/// <summary>
/// F1 debug menu panel. Displays a side panel with tabbed content for
/// rendering toggles, world controls, performance metrics, biome info,
/// entity controls, and system information.
/// </summary>
public sealed partial class DebugMenuPanel : PanelContainer
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
    private int _activeTab;

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
        CustomMinimumSize = new Vector2(DebugTheme.MenuPanelWidth, 0);
        SetAnchorsPreset(LayoutPreset.LeftWide);
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AddThemeStyleboxOverride("panel", DebugTheme.CreatePanelStyle());
        MouseFilter = MouseFilterEnum.Stop;

        VBoxContainer root = new();
        root.SetAnchorsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 4);
        AddChild(root);

        // Title
        Label title = new();
        title.Text = "Debug Menu (F1)";
        DebugTheme.ApplyLabelStyle(title, DebugTheme.TextAccent, DebugTheme.FontSizeTitle);
        root.AddChild(title);

        // Tab bar
        HBoxContainer tabBar = new();
        tabBar.AddThemeConstantOverride("separation", 2);
        root.AddChild(tabBar);

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
        root.AddChild(separator);

        // Scrollable content area
        ScrollContainer scroll = new();
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        root.AddChild(scroll);

        VBoxContainer contentArea = new();
        contentArea.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        contentArea.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.AddChild(contentArea);

        _tabs = new IDebugTab[TabCount];
        _tabContents = new Control[TabCount];

        AddTab(contentArea, 0, new RenderingTab(_optimizationFlags, _eventBus, _performanceMonitor));
        AddTab(contentArea, 1, new WorldTab(_debugData, _performanceMonitor));
        AddTab(contentArea, 2, new PerformanceTab(_sampler, _performanceMonitor, _pipelineMetrics));
        AddTab(contentArea, 3, new BiomeTab(_debugData, _chunkDebugProvider));
        AddTab(contentArea, 4, new EntitiesTab(_debugData));
        AddTab(contentArea, 5, new SystemTab(_sampler, _performanceMonitor));

        SwitchToTab(0);
    }

    /// <summary>
    /// Updates the active tab display. Called by DebugManager.
    /// </summary>
    public void UpdateDisplay()
    {
        _tabs[_activeTab].UpdateDisplay();
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
                i == index ? DebugTheme.CreateTabActiveStyle() : DebugTheme.CreateTabInactiveStyle());
        }
    }
}
#endif
