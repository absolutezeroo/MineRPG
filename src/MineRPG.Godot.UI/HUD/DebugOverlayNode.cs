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
/// Layout is defined in Scenes/UI/HUD/DebugOverlay.tscn; this script handles
/// data binding and visibility toggling.
/// Text formatting is delegated to <see cref="DebugOverlayFormatter"/>.
/// </summary>
public sealed partial class DebugOverlayNode : Control
{
    private const int StringBuilderCapacity = 512;

    [Export] private Label _positionLabel = null!;
    [Export] private Label _worldLabel = null!;
    [Export] private Label _performanceLabel = null!;

    private readonly StringBuilder _positionBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _worldBuilder = new(StringBuilderCapacity);
    private readonly StringBuilder _performanceBuilder = new(StringBuilderCapacity);

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
        // [Export] node references may not auto-resolve from NodePath;
        // fallback to GetNode for reliable resolution.
        _positionLabel ??= GetNode<Label>("LeftColumn/PositionPanel/PositionContent/PositionData");
        _worldLabel ??= GetNode<Label>("LeftColumn/WorldPanel/WorldContent/WorldData");
        _performanceLabel ??= GetNode<Label>("LeftColumn/PerformancePanel/PerformanceContent/PerformanceData");

        _debugData = ServiceLocator.Instance.Get<IDebugDataProvider>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

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
}
