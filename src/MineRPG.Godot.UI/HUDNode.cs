using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI;

/// <summary>
/// CanvasLayer container for all in-game HUD elements.
/// Creates and owns DebugOverlayNode, CrosshairNode, and HotbarNode.
/// The Camera3D reference is wired via [Export] with a fallback
/// resolution in CallDeferred (same pattern as PlayerNode).
/// </summary>
public sealed partial class HUDNode : CanvasLayer
{
    [Export] private Camera3D _camera = null!;

    private DebugOverlayNode _debugOverlay = null!;
    private ILogger _logger = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        _logger = ServiceLocator.Instance.Get<ILogger>();

        CrosshairNode crosshair = new();
        crosshair.Name = "Crosshair";
        AddChild(crosshair);

        _debugOverlay = new DebugOverlayNode();
        _debugOverlay.Name = "DebugOverlay";
        AddChild(_debugOverlay);

        HotbarNode hotbar = new();
        hotbar.Name = "Hotbar";
        AddChild(hotbar);

        Callable.From(InjectCamera).CallDeferred();

        _logger.Info("HUDNode ready.");
    }

    private void InjectCamera()
    {
        // [Export] NodePath may not auto-resolve on private fields in CanvasLayer.
        // Fallback: use the viewport's active Camera3D (no hardcoded paths).
        _camera ??= GetViewport().GetCamera3D();

        if (_camera is null)
        {
            _logger.Warning("HUDNode: Camera3D not found -- debug look direction unavailable.");
            return;
        }

        _debugOverlay.SetCamera(_camera);
        _logger.Info("HUDNode: Camera3D injected into DebugOverlay.");
    }
}
