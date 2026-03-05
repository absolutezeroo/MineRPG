using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.Screens;

/// <summary>
/// Root control for the main menu screen.
/// Minecraft-style: dark dirt-like background, large title, centered buttons.
/// All children built programmatically — no .tscn dependency.
/// </summary>
public sealed partial class MainMenuNode : Control
{
    private const float ButtonWidth = 300f;
    private const float ButtonHeight = 48f;
    private const float ButtonSpacing = 8f;
    private const int TitleFontSize = 56;
    private const int ButtonFontSize = 20;
    private const float TitleTopMargin = 80f;

    private static readonly Color BackgroundColor = new(0.22f, 0.17f, 0.13f, 1f);
    private static readonly Color TitleColor = new(1f, 1f, 1f, 1f);
    private static readonly Color TitleShadowColor = new(0.15f, 0.1f, 0.05f, 0.8f);
    private static readonly Color ButtonBgColor = new(0.35f, 0.35f, 0.35f, 0.85f);
    private static readonly Color ButtonBgHoverColor = new(0.45f, 0.45f, 0.5f, 0.9f);
    private static readonly Color ButtonBorderColor = new(0.2f, 0.2f, 0.2f, 1f);
    private static readonly Color VersionColor = new(0.6f, 0.6f, 0.6f, 0.8f);

    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private VBoxContainer _buttonStack = null!;
    private WorldSelectionPanelNode? _worldSelectionPanel;

    /// <inheritdoc />
    public override void _Ready()
    {
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        SetAnchorsPreset(LayoutPreset.FullRect);

        // Dark background (ignore mouse so buttons receive clicks)
        ColorRect background = new();
        background.SetAnchorsPreset(LayoutPreset.FullRect);
        background.Color = BackgroundColor;
        background.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(background);

        // Title
        Label title = new();
        title.Text = "MineRPG";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", TitleColor);
        title.AddThemeColorOverride("font_shadow_color", TitleShadowColor);
        title.AddThemeConstantOverride("shadow_offset_x", 3);
        title.AddThemeConstantOverride("shadow_offset_y", 3);
        title.AddThemeFontSizeOverride("font_size", TitleFontSize);
        title.SetAnchorsPreset(LayoutPreset.TopWide);
        title.OffsetTop = TitleTopMargin;
        title.OffsetBottom = TitleTopMargin + 80f;
        AddChild(title);

        // Button container (centered via CenterContainer)
        CenterContainer buttonCenter = new();
        buttonCenter.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        buttonCenter.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(buttonCenter);

        _buttonStack = new VBoxContainer();
        _buttonStack.AddThemeConstantOverride("separation", (int)ButtonSpacing);
        buttonCenter.AddChild(_buttonStack);

        Button singleplayerButton = CreateMenuButton("Singleplayer");
        singleplayerButton.Pressed += OnSingleplayerPressed;
        _buttonStack.AddChild(singleplayerButton);

        Button quitButton = CreateMenuButton("Quit Game");
        quitButton.Pressed += OnQuitPressed;
        _buttonStack.AddChild(quitButton);

        // Version label
        Label version = new();
        version.Text = "MineRPG Alpha 0.1";
        version.AddThemeColorOverride("font_color", VersionColor);
        version.AddThemeFontSizeOverride("font_size", 14);
        version.SetAnchorsPreset(LayoutPreset.BottomLeft);
        version.OffsetLeft = 8f;
        version.OffsetBottom = -8f;
        version.OffsetTop = -30f;
        AddChild(version);

        // Ensure mouse is visible on main menu
        Input.MouseMode = Input.MouseModeEnum.Visible;

        _logger.Info("MainMenuNode ready.");
    }

    private static Button CreateMenuButton(string text)
    {
        Button button = new();
        button.Text = text;
        button.CustomMinimumSize = new Vector2(ButtonWidth, ButtonHeight);
        button.AddThemeFontSizeOverride("font_size", ButtonFontSize);

        StyleBoxFlat normalStyle = new();
        normalStyle.BgColor = ButtonBgColor;
        normalStyle.BorderColor = ButtonBorderColor;
        normalStyle.SetBorderWidthAll(2);
        normalStyle.SetCornerRadiusAll(0);
        normalStyle.SetContentMarginAll(8);
        button.AddThemeStyleboxOverride("normal", normalStyle);

        StyleBoxFlat hoverStyle = new();
        hoverStyle.BgColor = ButtonBgHoverColor;
        hoverStyle.BorderColor = new Color(0.5f, 0.5f, 0.6f, 1f);
        hoverStyle.SetBorderWidthAll(2);
        hoverStyle.SetCornerRadiusAll(0);
        hoverStyle.SetContentMarginAll(8);
        button.AddThemeStyleboxOverride("hover", hoverStyle);

        StyleBoxFlat pressedStyle = new();
        pressedStyle.BgColor = new Color(0.3f, 0.3f, 0.35f, 0.95f);
        pressedStyle.BorderColor = ButtonBorderColor;
        pressedStyle.SetBorderWidthAll(2);
        pressedStyle.SetCornerRadiusAll(0);
        pressedStyle.SetContentMarginAll(8);
        button.AddThemeStyleboxOverride("pressed", pressedStyle);

        return button;
    }

    private void OnSingleplayerPressed()
    {
        _buttonStack.Visible = false;

        if (_worldSelectionPanel is null)
        {
            _worldSelectionPanel = new WorldSelectionPanelNode();
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
