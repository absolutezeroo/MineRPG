using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces.Lifecycle;
using MineRPG.Core.Logging;
using MineRPG.Game.Bootstrap.Input;

using MineRPG.Godot.UI.Options;

namespace MineRPG.Godot.UI.Screens;

/// <summary>
/// Pause menu overlay. Shows when <see cref="GamePausedEvent"/> fires with IsPaused=true.
/// Layout is defined in Scenes/UI/PauseMenu.tscn; this script contains only
/// signal handlers, event bus subscriptions, and options panel management.
/// </summary>
public sealed partial class PauseMenuNode : Control
{
    private const string OptionsScenePath = "res://Scenes/UI/Options.tscn";

    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private VBoxContainer _buttonStack = null!;
    private Label _title = null!;
    private OptionsPanelNode? _optionsPanel;

    /// <inheritdoc />
    public override void _Ready()
    {
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        GameTheme.Apply(this);

        Visible = false;

        _title = GetNode<Label>("CenterContainer/PanelContainer/VBoxContainer/Title");
        _buttonStack = GetNode<VBoxContainer>(
            "CenterContainer/PanelContainer/VBoxContainer/ButtonStack");

        _title.ThemeTypeVariation = ThemeTypeVariations.ScreenTitleLabel;

        Button resumeButton = GetNode<Button>(
            "CenterContainer/PanelContainer/VBoxContainer/ButtonStack/ResumeButton");
        resumeButton.Pressed += OnResumePressed;

        Button optionsButton = GetNode<Button>(
            "CenterContainer/PanelContainer/VBoxContainer/ButtonStack/OptionsButton");
        optionsButton.Pressed += OnOptionsPressed;

        Button menuButton = GetNode<Button>(
            "CenterContainer/PanelContainer/VBoxContainer/ButtonStack/MenuButton");
        menuButton.Pressed += OnReturnToMenuPressed;

        Button quitButton = GetNode<Button>(
            "CenterContainer/PanelContainer/VBoxContainer/ButtonStack/QuitButton");
        quitButton.Pressed += OnQuitPressed;

        _eventBus.Subscribe<GamePausedEvent>(OnGamePaused);

        _logger.Info("PauseMenuNode ready.");
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (_eventBus is not null)
        {
            _eventBus.Unsubscribe<GamePausedEvent>(OnGamePaused);
        }
    }

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        if (!Visible)
        {
            return;
        }

        if (@event.IsActionPressed(InputActions.Pause))
        {
            OnResumePressed();
            GetViewport().SetInputAsHandled();
        }
    }

    private void OnGamePaused(GamePausedEvent evt)
    {
        Visible = evt.IsPaused;

        if (evt.IsPaused)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;

            if (_optionsPanel is not null)
            {
                _optionsPanel.Visible = false;
            }

            _buttonStack.Visible = true;
        }
        else
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    private void OnResumePressed()
    {
        if (ServiceLocator.Instance.TryGet<IGameStateController>(
            out IGameStateController? controller))
        {
            controller.RequestResume();
        }
    }

    private void OnOptionsPressed()
    {
        _buttonStack.Visible = false;

        if (_optionsPanel is null)
        {
            PackedScene optionsScene = GD.Load<PackedScene>(OptionsScenePath);
            _optionsPanel = optionsScene.Instantiate<OptionsPanelNode>();
            _optionsPanel.Name = "OptionsPanel";
            _optionsPanel.BackRequested += OnBackFromOptions;
            GetParent().AddChild(_optionsPanel);
        }
        else
        {
            _optionsPanel.Visible = true;
        }
    }

    private void OnBackFromOptions()
    {
        if (_optionsPanel is not null)
        {
            _optionsPanel.Visible = false;
        }

        _buttonStack.Visible = true;
    }

    private void OnReturnToMenuPressed()
    {
        _logger.Info("PauseMenuNode: Return to main menu.");
        _eventBus.Publish(new ReturnToMainMenuEvent());
    }

    private void OnQuitPressed()
    {
        _logger.Info("PauseMenuNode: Quit.");
        _eventBus.Publish(new GameQuitRequestedEvent());
    }
}
