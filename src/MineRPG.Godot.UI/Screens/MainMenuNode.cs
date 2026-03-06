using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.Screens;

/// <summary>
/// Root control for the main menu screen.
/// Layout is defined in Scenes/UI/MainMenu.tscn; this script contains only
/// signal handlers, event bus logic, and world-selection panel management.
/// </summary>
public sealed partial class MainMenuNode : Control
{
    private const string WorldSelectionScenePath = "res://Scenes/UI/WorldSelection.tscn";

    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private VBoxContainer _buttonStack = null!;
    private Label _title = null!;
    private Label _titleShadow = null!;
    private Label _versionLabel = null!;
    private WorldSelectionPanelNode? _worldSelectionPanel;

    /// <inheritdoc />
    public override void _Ready()
    {
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        GameTheme.Apply(this);

        _title = GetNode<Label>("Title");
        _titleShadow = GetNode<Label>("TitleShadow");
        _buttonStack = GetNode<VBoxContainer>("CenterContainer/ButtonStack");
        _versionLabel = GetNode<Label>("VersionLabel");

        // Title styling (hero size, not part of global theme)
        _title.AddThemeFontSizeOverride("font_size", GameTheme.FontSizeHero);
        _title.AddThemeColorOverride("font_color", GameTheme.TextTitle);

        _titleShadow.AddThemeFontSizeOverride("font_size", GameTheme.FontSizeHero);
        _titleShadow.AddThemeColorOverride("font_color", GameTheme.TitleShadow);

        _versionLabel.AddThemeColorOverride("font_color", GameTheme.TextVersion);
        _versionLabel.AddThemeFontSizeOverride("font_size", GameTheme.FontSizeSmall);

        // Button font size override for large menu buttons
        Button singleplayerButton = GetNode<Button>("CenterContainer/ButtonStack/SingleplayerButton");
        singleplayerButton.AddThemeFontSizeOverride("font_size", GameTheme.FontSizeButtonLarge);
        singleplayerButton.Pressed += OnSingleplayerPressed;

        Button quitButton = GetNode<Button>("CenterContainer/ButtonStack/QuitButton");
        quitButton.AddThemeFontSizeOverride("font_size", GameTheme.FontSizeButtonLarge);
        quitButton.Pressed += OnQuitPressed;

        Input.MouseMode = Input.MouseModeEnum.Visible;

        _logger.Info("MainMenuNode ready.");
    }

    private void OnSingleplayerPressed()
    {
        _buttonStack.Visible = false;

        if (_worldSelectionPanel is null)
        {
            PackedScene worldSelectionScene = GD.Load<PackedScene>(WorldSelectionScenePath);
            _worldSelectionPanel = worldSelectionScene.Instantiate<WorldSelectionPanelNode>();
            _worldSelectionPanel.Name = "WorldSelectionPanel";
            _worldSelectionPanel.BackRequested += OnBackFromWorldSelection;
            AddChild(_worldSelectionPanel);
        }
        else
        {
            _worldSelectionPanel.Visible = true;
            _worldSelectionPanel.RefreshWorldList();
        }

        _logger.Info("MainMenuNode: Showing world selection.");
    }

    private void OnBackFromWorldSelection()
    {
        if (_worldSelectionPanel is not null)
        {
            _worldSelectionPanel.Visible = false;
        }

        _buttonStack.Visible = true;
    }

    private void OnQuitPressed()
    {
        _logger.Info("MainMenuNode: Quit pressed.");
        _eventBus.Publish(new GameQuitRequestedEvent());
    }
}
