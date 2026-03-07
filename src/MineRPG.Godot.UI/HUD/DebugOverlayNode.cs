using System.Text;

using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;
using MineRPG.Game.Bootstrap.Input;

namespace MineRPG.Godot.UI.HUD;

/// <summary>
/// Minecraft-style F3 debug overlay. Displays grouped info in semi-transparent
/// dark panels: Position, World, and Performance sections.
/// Text formatting is delegated to <see cref="DebugOverlayFormatter"/>.
/// </summary>
public sealed partial class DebugOverlayNode : Control
{
    private const int StringBuilderCapacity = 512;
    private const float PanelMarginX = 4f;
    private const float PanelMarginY = 4f;
    private const float PanelPaddingX = 6f;
    private const float PanelPaddingY = 4f;
    private const float SectionSpacing = 6f;
    private const float PanelBackgroundAlpha = 0.45f;
    private const float ShadowColorAlpha = 0.75f;
    private const int ShadowOffsetPixels = 1;
    private const int FontSize = 14;

    private static readonly Color PanelColor = new(0f, 0f, 0f, PanelBackgroundAlpha);
    private static readonly Color TextColor = new(1f, 1f, 1f, 1f);
    private static readonly Color ShadowColor = new(0f, 0f, 0f, ShadowColorAlpha);
    private static readonly Color HeaderColor = new(0.55f, 0.85f, 0.55f, 1f);

    private readonly StringBuilder _positionBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _worldBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _performanceBuilder = new(StringBuilderCapacity);

    private VBoxContainer _leftColumn = null!;
    private Label _positionLabel = null!;
    private Label _worldLabel = null!;
    private Label _performanceLabel = null!;

    private IDebugDataProvider _debugData = null!;
    private ILogger _logger = null!;
    private Camera3D? _camera;

    /// <summary>
    /// Called by HUDNode once the Camera3D reference is available.
    /// </summary>
    /// <param name="camera">The active 3D camera.</param>
    public void SetCamera(Camera3D camera) => _camera = camera;

    /// <inheritdoc />
    public override void _Ready()
    {
        _debugData = ServiceLocator.Instance.Get<IDebugDataProvider>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        _leftColumn = new VBoxContainer();
        _leftColumn.SetAnchorsPreset(LayoutPreset.TopLeft);
        _leftColumn.Position = new Vector2(PanelMarginX, PanelMarginY);
        _leftColumn.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_leftColumn);

        _positionLabel = CreateSection("--- Position ---");
        AddSpacer();
        _worldLabel = CreateSection("--- World ---");
        AddSpacer();
        _performanceLabel = CreateSection("--- Performance ---");

        Visible = false;
        _logger.Info("DebugOverlayNode ready.");
    }

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        if (!@event.IsActionPressed(InputActions.DebugToggle))
        {
            return;
        }

        Visible = !Visible;
        _logger.Info("DebugOverlay toggled: Visible={0}", Visible);
        GetViewport().SetInputAsHandled();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (!Visible)
        {
            return;
        }

        DebugOverlayFormatter.FormatPositionSection(_positionBuilder, _debugData, _camera);
        _positionLabel.Text = _positionBuilder.ToString();

        DebugOverlayFormatter.FormatWorldSection(_worldBuilder, _debugData);
        _worldLabel.Text = _worldBuilder.ToString();

        DebugOverlayFormatter.FormatPerformanceSection(_performanceBuilder, _debugData);
        _performanceLabel.Text = _performanceBuilder.ToString();
    }

    private Label CreateSection(string headerText)
    {
        PanelContainer panel = CreatePanel();
        _leftColumn.AddChild(panel);

        VBoxContainer content = new();
        content.MouseFilter = MouseFilterEnum.Ignore;
        panel.AddChild(content);

        Label header = CreateHeaderLabel(headerText);
        content.AddChild(header);

        Label dataLabel = CreateDataLabel();
        content.AddChild(dataLabel);

        return dataLabel;
    }

    private static PanelContainer CreatePanel()
    {
        PanelContainer panel = new();
        panel.MouseFilter = MouseFilterEnum.Ignore;

        StyleBoxFlat style = new();
        style.BgColor = PanelColor;
        style.ContentMarginLeft = PanelPaddingX;
        style.ContentMarginRight = PanelPaddingX;
        style.ContentMarginTop = PanelPaddingY;
        style.ContentMarginBottom = PanelPaddingY;
        style.CornerRadiusTopLeft = 2;
        style.CornerRadiusTopRight = 2;
        style.CornerRadiusBottomLeft = 2;
        style.CornerRadiusBottomRight = 2;

        panel.AddThemeStyleboxOverride("panel", style);
        return panel;
    }

    private static Label CreateHeaderLabel(string text)
    {
        Label header = new();
        header.Text = text;
        header.AddThemeColorOverride("font_color", HeaderColor);
        header.AddThemeColorOverride("font_shadow_color", ShadowColor);
        header.AddThemeConstantOverride("shadow_offset_x", ShadowOffsetPixels);
        header.AddThemeConstantOverride("shadow_offset_y", ShadowOffsetPixels);
        header.AddThemeFontSizeOverride("font_size", FontSize);
        header.MouseFilter = MouseFilterEnum.Ignore;
        return header;
    }

    private static Label CreateDataLabel()
    {
        Label label = new();
        label.AddThemeColorOverride("font_color", TextColor);
        label.AddThemeColorOverride("font_shadow_color", ShadowColor);
        label.AddThemeConstantOverride("shadow_offset_x", ShadowOffsetPixels);
        label.AddThemeConstantOverride("shadow_offset_y", ShadowOffsetPixels);
        label.AddThemeFontSizeOverride("font_size", FontSize);
        label.MouseFilter = MouseFilterEnum.Ignore;
        return label;
    }

    private void AddSpacer()
    {
        Control spacer = new();
        spacer.CustomMinimumSize = new Vector2(0, SectionSpacing);
        spacer.MouseFilter = MouseFilterEnum.Ignore;
        _leftColumn.AddChild(spacer);
    }
}
