using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.Options;

/// <summary>
/// Tabbed options panel. Layout is defined in Scenes/UI/Options.tscn; this script
/// manages tab switching and hosts the pre-placed tab content sub-scenes
/// (Game, Graphics, Controls). Accessible from the pause menu.
/// </summary>
public sealed partial class OptionsPanelNode : Control
{
    private const int TabCount = 3;

    /// <summary>Emitted when the player clicks the Back button.</summary>
    [Signal]
    public delegate void BackRequestedEventHandler();

    [Export] private Button _gameTabButton = null!;
    [Export] private Button _graphicsTabButton = null!;
    [Export] private Button _controlsTabButton = null!;
    [Export] private GameTabPanel _gameTabPanel = null!;
    [Export] private GraphicsTabPanel _graphicsTabPanel = null!;
    [Export] private ControlsTabPanel _controlsTabPanel = null!;
    [Export] private Button _backButton = null!;

    private ILogger _logger = null!;
    private Button[] _tabButtons = null!;
    private Control[] _tabContents = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        _logger = ServiceLocator.Instance.Get<ILogger>();

        // [Export] node references may not auto-resolve from NodePath;
        // fallback to GetNode for reliable resolution.
        _gameTabButton ??= GetNode<Button>("CenterContainer/PanelContainer/VBoxContainer/TabStrip/GameTab");
        _graphicsTabButton ??= GetNode<Button>("CenterContainer/PanelContainer/VBoxContainer/TabStrip/GraphicsTab");
        _controlsTabButton ??= GetNode<Button>("CenterContainer/PanelContainer/VBoxContainer/TabStrip/ControlsTab");
        _gameTabPanel ??= GetNode<GameTabPanel>("CenterContainer/PanelContainer/VBoxContainer/ContentArea/ScrollContainer/TabContent/GameTab");
        _graphicsTabPanel ??= GetNode<GraphicsTabPanel>("CenterContainer/PanelContainer/VBoxContainer/ContentArea/ScrollContainer/TabContent/GraphicsTab");
        _controlsTabPanel ??= GetNode<ControlsTabPanel>("CenterContainer/PanelContainer/VBoxContainer/ContentArea/ScrollContainer/TabContent/ControlsTab");
        _backButton ??= GetNode<Button>("CenterContainer/PanelContainer/VBoxContainer/BackButton");

        _tabButtons = [_gameTabButton, _graphicsTabButton, _controlsTabButton];
        _tabContents = [_gameTabPanel, _graphicsTabPanel, _controlsTabPanel];

        _gameTabButton.Pressed += () => SetActiveTab(0);
        _graphicsTabButton.Pressed += () => SetActiveTab(1);
        _controlsTabButton.Pressed += () => SetActiveTab(2);

        _backButton.Pressed += OnBackPressed;

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
