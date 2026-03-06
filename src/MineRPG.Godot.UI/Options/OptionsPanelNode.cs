using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Logging;
namespace MineRPG.Godot.UI.Options;

/// <summary>
/// Tabbed options panel. Hosts three tabs - Game, Graphics, Controls -
/// and manages tab switching. Accessible from the pause menu.
/// Built programmatically to match the existing Minecraft-style dark palette.
/// </summary>
public sealed partial class OptionsPanelNode : Control
{
    private const float PanelWidth = 640f;
    private const float PanelHeight = 540f;
    private const float TabButtonWidth = 120f;
    private const float TabButtonHeight = 36f;
    private const int TitleFontSize = 26;
    private const int TabFontSize = 16;
    private const int BackButtonFontSize = 18;
    private const float BackButtonHeight = 42f;

    private static readonly Color PanelBgColor = new(0.18f, 0.15f, 0.12f, 0.95f);
    private static readonly Color TitleColor = new(1f, 1f, 1f, 1f);
    private static readonly Color TabActiveColor = new(0.35f, 0.30f, 0.22f, 1f);
    private static readonly Color TabInactiveColor = new(0.22f, 0.19f, 0.15f, 1f);
    private static readonly Color TabBorderColor = new(0.45f, 0.38f, 0.28f, 1f);
    private static readonly Color ContentBgColor = new(0.14f, 0.12f, 0.10f, 0.85f);
    private static readonly Color BorderColor = new(0.3f, 0.25f, 0.2f, 1f);

    /// <summary>Emitted when the player clicks the Back button.</summary>
    [Signal]
    public delegate void BackRequestedEventHandler();

    private ILogger _logger = null!;
    private Button[] _tabButtons = null!;
    private Control[] _tabContents = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        _logger = ServiceLocator.Instance.Get<ILogger>();

        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        // Center panel
        CenterContainer panelCenter = new();
        panelCenter.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panelCenter.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(panelCenter);

        PanelContainer panel = new();
        panel.CustomMinimumSize = new Vector2(PanelWidth, PanelHeight);

        StyleBoxFlat panelStyle = new();
        panelStyle.BgColor = PanelBgColor;
        panelStyle.SetBorderWidthAll(2);
        panelStyle.BorderColor = BorderColor;
        panelStyle.SetContentMarginAll(16);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        panelCenter.AddChild(panel);

        VBoxContainer outerLayout = new();
        outerLayout.AddThemeConstantOverride("separation", 8);
        panel.AddChild(outerLayout);

        // Title
        Label title = new();
        title.Text = "Options";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", TitleColor);
        title.AddThemeFontSizeOverride("font_size", TitleFontSize);
        outerLayout.AddChild(title);

        outerLayout.AddChild(new HSeparator());

        // Tab strip
        HBoxContainer tabStrip = new();
        tabStrip.AddThemeConstantOverride("separation", 4);
        tabStrip.Alignment = BoxContainer.AlignmentMode.Center;
        outerLayout.AddChild(tabStrip);

        string[] tabNames = ["Game", "Graphics", "Controls"];
        _tabButtons = new Button[tabNames.Length];

        for (int i = 0; i < tabNames.Length; i++)
        {
            int capturedIndex = i;
            Button tabButton = CreateTabButton(tabNames[i]);
            tabButton.Pressed += () => SetActiveTab(capturedIndex);
            tabStrip.AddChild(tabButton);
            _tabButtons[i] = tabButton;
        }

        // Content area with scroll support
        PanelContainer contentPanel = new();
        contentPanel.SizeFlagsVertical = SizeFlags.ExpandFill;

        StyleBoxFlat contentStyle = new();
        contentStyle.BgColor = ContentBgColor;
        contentStyle.SetBorderWidthAll(1);
        contentStyle.BorderColor = BorderColor;
        contentStyle.SetContentMarginAll(12);
        contentPanel.AddThemeStyleboxOverride("panel", contentStyle);
        outerLayout.AddChild(contentPanel);

        ScrollContainer scrollContainer = new();
        scrollContainer.SizeFlagsVertical = SizeFlags.ExpandFill;
        scrollContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        contentPanel.AddChild(scrollContainer);

        // Build tab content panels
        _tabContents = new Control[3];

        GameTabPanel gameTab = new();
        gameTab.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scrollContainer.AddChild(gameTab);
        _tabContents[0] = gameTab;

        GraphicsTabPanel graphicsTab = new();
        graphicsTab.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        graphicsTab.Visible = false;
        scrollContainer.AddChild(graphicsTab);
        _tabContents[1] = graphicsTab;

        ControlsTabPanel controlsTab = new();
        controlsTab.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        controlsTab.Visible = false;
        scrollContainer.AddChild(controlsTab);
        _tabContents[2] = controlsTab;

        // Back button
        Button backButton = new();
        backButton.Text = "Back";
        backButton.CustomMinimumSize = new Vector2(120f, BackButtonHeight);
        backButton.AddThemeFontSizeOverride("font_size", BackButtonFontSize);
        backButton.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        backButton.Pressed += OnBackPressed;
        outerLayout.AddChild(backButton);

        // Activate first tab
        SetActiveTab(0);

        _logger.Info("OptionsPanelNode (tabbed) ready.");
    }

    private void SetActiveTab(int index)
    {
        for (int i = 0; i < _tabContents.Length; i++)
        {
            _tabContents[i].Visible = (i == index);
            ApplyTabButtonStyle(_tabButtons[i], isActive: i == index);
        }
    }

    private void OnBackPressed() => EmitSignal(SignalName.BackRequested);

    private static Button CreateTabButton(string text)
    {
        Button button = new();
        button.Text = text;
        button.CustomMinimumSize = new Vector2(TabButtonWidth, TabButtonHeight);
        button.AddThemeFontSizeOverride("font_size", TabFontSize);
        return button;
    }

    private static void ApplyTabButtonStyle(Button button, bool isActive)
    {
        StyleBoxFlat style = new();
        style.BgColor = isActive ? TabActiveColor : TabInactiveColor;
        style.SetBorderWidthAll(1);
        style.BorderColor = TabBorderColor;
        style.SetContentMarginAll(6);

        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
        button.AddThemeStyleboxOverride("focus", style);
    }
}
