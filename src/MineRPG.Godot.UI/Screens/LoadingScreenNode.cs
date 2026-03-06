using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.Screens;

/// <summary>
/// Full-screen loading overlay displayed during the initial chunk preload.
/// Layout is defined in Scenes/UI/LoadingScreen.tscn; this script only
/// polls <see cref="PreloadProgress"/> each frame and dismisses on
/// <see cref="WorldReadyEvent"/>.
/// </summary>
public sealed partial class LoadingScreenNode : CanvasLayer
{
    private const int DefaultPreloadChunkCount = 49;

    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private PreloadProgress? _progress;
    private ProgressBar _progressBar = null!;
    private Label _statusLabel = null!;
    private Label _title = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        if (ServiceLocator.Instance.TryGet<PreloadProgress>(out PreloadProgress? progress))
        {
            _progress = progress;
        }

        // Apply theme to the child Control root (CanvasLayer is not a Control)
        Control root = GetNode<Control>("Root");
        GameTheme.Apply(root);

        _title = GetNode<Label>("Root/CenterContainer/PanelContainer/VBoxContainer/Title");
        _progressBar = GetNode<ProgressBar>(
            "Root/CenterContainer/PanelContainer/VBoxContainer/ProgressBar");
        _statusLabel = GetNode<Label>(
            "Root/CenterContainer/PanelContainer/VBoxContainer/StatusLabel");

        _title.AddThemeFontSizeOverride("font_size", GameTheme.FontSizeTitleLarge);
        _title.AddThemeColorOverride("font_color", GameTheme.TextTitle);

        _statusLabel.AddThemeColorOverride("font_color", GameTheme.TextSub);

        _progressBar.MinValue = 0.0;
        _progressBar.MaxValue = 100.0;
        _progressBar.Value = 0.0;
        _progressBar.ShowPercentage = false;

        _eventBus.Subscribe<WorldReadyEvent>(OnWorldReady);

        _logger.Info("LoadingScreenNode ready.");
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (_eventBus is not null)
        {
            _eventBus.Unsubscribe<WorldReadyEvent>(OnWorldReady);
        }
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
        _logger.Info("LoadingScreenNode: World ready - dismissing loading screen.");
        QueueFree();
    }
}
