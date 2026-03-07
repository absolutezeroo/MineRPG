using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.Options;

/// <summary>
/// Tabbed options panel. Layout is defined in Scenes/UI/Options.tscn; this script
/// manages tab switching and hosts the dynamically created tab content panels
/// (Game, Graphics, Controls). Accessible from the pause menu.
/// </summary>
public sealed partial class OptionsPanelNode : Control
{
    private const int TabCount = 3;

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

        GameTheme.Apply(this);

        Label title = GetNode<Label>(
            "CenterContainer/PanelContainer/VBoxContainer/Title");
        title.ThemeTypeVariation = ThemeTypeVariations.PanelTitleLabel;

        // Tab strip buttons
        Button gameTab = GetNode<Button>(
            "CenterContainer/PanelContainer/VBoxContainer/TabStrip/GameTab");
        Button graphicsTab = GetNode<Button>(
            "CenterContainer/PanelContainer/VBoxContainer/TabStrip/GraphicsTab");
        Button controlsTab = GetNode<Button>(
            "CenterContainer/PanelContainer/VBoxContainer/TabStrip/ControlsTab");

        _tabButtons = [gameTab, graphicsTab, controlsTab];

        gameTab.Pressed += () => SetActiveTab(0);
        graphicsTab.Pressed += () => SetActiveTab(1);
        controlsTab.Pressed += () => SetActiveTab(2);

        // Build tab content panels programmatically into the scene-defined TabContent container
        VBoxContainer tabContent = GetNode<VBoxContainer>(
            "CenterContainer/PanelContainer/VBoxContainer/ContentArea/ScrollContainer/TabContent");

        _tabContents = new Control[TabCount];

        GameTabPanel gameTabPanel = new();
        gameTabPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        tabContent.AddChild(gameTabPanel);
        _tabContents[0] = gameTabPanel;

        GraphicsTabPanel graphicsTabPanel = new();
        graphicsTabPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        graphicsTabPanel.Visible = false;
        tabContent.AddChild(graphicsTabPanel);
        _tabContents[1] = graphicsTabPanel;

        ControlsTabPanel controlsTabPanel = new();
        controlsTabPanel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        controlsTabPanel.Visible = false;
        tabContent.AddChild(controlsTabPanel);
        _tabContents[2] = controlsTabPanel;

        // Back button
        Button backButton = GetNode<Button>(
            "CenterContainer/PanelContainer/VBoxContainer/BackButton");
        backButton.Pressed += OnBackPressed;

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

    private static void ApplyTabButtonStyle(Button button, bool isActive)
    {
        StyleBoxFlat style = GameTheme.CreateTabStyle(isActive);

        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
        button.AddThemeStyleboxOverride("focus", style);
    }
}
