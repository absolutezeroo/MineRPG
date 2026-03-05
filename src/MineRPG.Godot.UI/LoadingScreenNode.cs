using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI;

/// <summary>
/// Full-screen loading overlay displayed during the initial chunk preload.
/// Rendered on CanvasLayer 128 so it covers all gameplay nodes.
/// Polls <see cref="PreloadProgress"/> each frame to update the progress bar
/// and chunk count label. Dismisses itself when <see cref="WorldReadyEvent"/> fires.
/// All UI built programmatically — no .tscn dependency.
/// </summary>
public sealed partial class LoadingScreenNode : CanvasLayer
{
    private const int CanvasLayerOrder = 128;
    private const int TitleFontSize = 36;
    private const int StatusFontSize = 18;
    private const float BarWidth = 420f;
    private const float BarHeight = 28f;
    private const float PanelWidth = 500f;
    private const float PanelHeight = 180f;

    private static readonly Color BackgroundColor = new(0.08f, 0.08f, 0.08f, 1f);
    private static readonly Color TitleColor = new(1f, 1f, 1f, 1f);
    private static readonly Color StatusColor = new(0.75f, 0.75f, 0.75f, 1f);
    private static readonly Color BarBgColor = new(0.2f, 0.2f, 0.2f, 1f);
    private static readonly Color BarFillColor = new(0.25f, 0.65f, 0.35f, 1f);
    private static readonly Color PanelColor = new(0.12f, 0.12f, 0.12f, 0.98f);
    private static readonly Color PanelBorderColor = new(0.3f, 0.3f, 0.3f, 1f);
    private static readonly Color BarBorderColor = new(0.35f, 0.35f, 0.35f, 1f);

    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private PreloadProgress? _progress;
    private ProgressBar _progressBar = null!;
    private Label _statusLabel = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        if (ServiceLocator.Instance.TryGet<PreloadProgress>(out PreloadProgress? progress))
        {
            _progress = progress;
        }

        Layer = CanvasLayerOrder;
        ProcessMode = ProcessModeEnum.Always;

        BuildUI();

        _eventBus.Subscribe<WorldReadyEvent>(OnWorldReady);

        _logger.Info("LoadingScreenNode ready.");
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        _eventBus?.Unsubscribe<WorldReadyEvent>(OnWorldReady);
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (_progress is null)
        {
            return;
        }

        int meshed = _progress.MeshedCount;
        int required = _progress.Required;

        _progressBar.Value = required > 0 ? (double)meshed / required * 100.0 : 0.0;
        _statusLabel.Text = $"Loading terrain... {meshed}/{required} chunks";
    }

    private void OnWorldReady(WorldReadyEvent evt)
    {
        _logger.Info("LoadingScreenNode: World ready — dismissing loading screen.");
        QueueFree();
    }

    private void BuildUI()
    {
        // Full-screen dark background
        ColorRect background = new();
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        background.Color = BackgroundColor;
        background.MouseFilter = Control.MouseFilterEnum.Stop;
        AddChild(background);

        // Center panel via CenterContainer
        CenterContainer center = new();
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        center.MouseFilter = Control.MouseFilterEnum.Ignore;
        background.AddChild(center);

        PanelContainer panel = new();
        panel.CustomMinimumSize = new Vector2(PanelWidth, PanelHeight);

        StyleBoxFlat panelStyle = new();
        panelStyle.BgColor = PanelColor;
        panelStyle.SetBorderWidthAll(1);
        panelStyle.BorderColor = PanelBorderColor;
        panelStyle.SetContentMarginAll(24f);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        center.AddChild(panel);

        VBoxContainer layout = new();
        layout.AddThemeConstantOverride("separation", 16);
        panel.AddChild(layout);

        // Title
        Label title = new();
        title.Text = "Loading World...";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeColorOverride("font_color", TitleColor);
        title.AddThemeFontSizeOverride("font_size", TitleFontSize);
        layout.AddChild(title);

        // Progress bar
        _progressBar = new ProgressBar();
        _progressBar.CustomMinimumSize = new Vector2(BarWidth, BarHeight);
        _progressBar.MinValue = 0.0;
        _progressBar.MaxValue = 100.0;
        _progressBar.Value = 0.0;
        _progressBar.ShowPercentage = false;

        StyleBoxFlat barBg = new();
        barBg.BgColor = BarBgColor;
        barBg.SetBorderWidthAll(1);
        barBg.BorderColor = BarBorderColor;
        _progressBar.AddThemeStyleboxOverride("background", barBg);

        StyleBoxFlat barFill = new();
        barFill.BgColor = BarFillColor;
        _progressBar.AddThemeStyleboxOverride("fill", barFill);

        layout.AddChild(_progressBar);

        // Status label
        int required = _progress?.Required ?? 49;
        _statusLabel = new Label();
        _statusLabel.Text = $"Loading terrain... 0/{required} chunks";
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statusLabel.AddThemeColorOverride("font_color", StatusColor);
        _statusLabel.AddThemeFontSizeOverride("font_size", StatusFontSize);
        layout.AddChild(_statusLabel);
    }
}
