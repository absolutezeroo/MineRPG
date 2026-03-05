using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI;

/// <summary>
/// Pause menu overlay. Shows when <see cref="GamePausedEvent"/> fires with IsPaused=true.
/// Handles Resume, Options, Return to Menu, and Quit buttons.
/// ProcessMode is set to WhenPaused so the menu remains interactive while the tree is paused.
/// </summary>
public sealed partial class PauseMenuNode : Control
{
    private const float PanelWidth = 320f;
    private const float ButtonHeight = 44f;
    private const float ButtonSpacing = 8f;
    private const int TitleFontSize = 32;
    private const int ButtonFontSize = 18;

    private static readonly Color OverlayColor = new(0f, 0f, 0f, 0.55f);
    private static readonly Color PanelBgColor = new(0.18f, 0.15f, 0.12f, 0.95f);
    private static readonly Color TitleColor = new(1f, 1f, 1f, 1f);

    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private VBoxContainer _buttonStack = null!;
    private OptionsPanelNode? _optionsPanel;

    /// <inheritdoc />
    public override void _Ready()
    {
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        // Must process when paused so the menu stays interactive
        ProcessMode = ProcessModeEnum.WhenPaused;

        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Visible = false;

        // Semi-transparent overlay (ignore mouse so panel buttons receive clicks)
        ColorRect overlay = new();
        overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        overlay.Color = OverlayColor;
        overlay.MouseFilter = MouseFilterEnum.Stop;
        AddChild(overlay);

        // Center panel via CenterContainer
        CenterContainer panelCenter = new();
        panelCenter.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panelCenter.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(panelCenter);

        PanelContainer panel = new();
        panel.CustomMinimumSize = new Vector2(PanelWidth, 320f);

        StyleBoxFlat panelStyle = new();
        panelStyle.BgColor = PanelBgColor;
        panelStyle.SetBorderWidthAll(2);
        panelStyle.BorderColor = new Color(0.3f, 0.25f, 0.2f, 1f);
        panelStyle.SetContentMarginAll(16);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        panelCenter.AddChild(panel);

        VBoxContainer layout = new();
        layout.AddThemeConstantOverride("separation", 10);
        panel.AddChild(layout);

        // Title
        Label title = new();
        title.Text = "Game Paused";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", TitleColor);
        title.AddThemeFontSizeOverride("font_size", TitleFontSize);
        layout.AddChild(title);

        HSeparator separator = new();
        layout.AddChild(separator);

        // Button stack
        _buttonStack = new VBoxContainer();
        _buttonStack.AddThemeConstantOverride("separation", (int)ButtonSpacing);
        layout.AddChild(_buttonStack);

        Button resumeButton = CreatePauseButton("Resume");
        resumeButton.Pressed += OnResumePressed;
        _buttonStack.AddChild(resumeButton);

        Button optionsButton = CreatePauseButton("Options");
        optionsButton.Pressed += OnOptionsPressed;
        _buttonStack.AddChild(optionsButton);

        Button menuButton = CreatePauseButton("Return to Main Menu");
        menuButton.Pressed += OnReturnToMenuPressed;
        _buttonStack.AddChild(menuButton);

        Button quitButton = CreatePauseButton("Quit Game");
        quitButton.Pressed += OnQuitPressed;
        _buttonStack.AddChild(quitButton);

        _eventBus.Subscribe<GamePausedEvent>(OnGamePaused);

        _logger.Info("PauseMenuNode ready.");
    }

    /// <inheritdoc />
    public override void _ExitTree() => _eventBus?.Unsubscribe<GamePausedEvent>(OnGamePaused);

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        if (!Visible)
        {
            return;
        }

        // ESC while pause menu is open -> resume
        if (@event.IsActionPressed(InputActionNames.Pause))
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

            // Hide options panel if it was open
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
            _optionsPanel = new OptionsPanelNode();
            _optionsPanel.Name = "OptionsPanel";
            _optionsPanel.BackRequested += OnBackFromOptions;
            GetParent().AddChild(_optionsPanel);
            _optionsPanel.ProcessMode = ProcessModeEnum.WhenPaused;
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

    private static Button CreatePauseButton(string text)
    {
        Button button = new();
        button.Text = text;
        button.CustomMinimumSize = new Vector2(0f, ButtonHeight);
        button.AddThemeFontSizeOverride("font_size", ButtonFontSize);
        return button;
    }
}
