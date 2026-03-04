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

    public override void _Ready()
    {
        _logger = ServiceLocator.Instance.Get<ILogger>();

        var crosshair = new CrosshairNode();
        crosshair.Name = "Crosshair";
        AddChild(crosshair);

        _debugOverlay = new DebugOverlayNode();
        _debugOverlay.Name = "DebugOverlay";
        AddChild(_debugOverlay);

        var hotbar = new HotbarNode();
        hotbar.Name = "Hotbar";
        AddChild(hotbar);

        CallDeferred(MethodName.InjectCamera);

        _logger.Info("HUDNode ready.");
    }

    private void InjectCamera()
    {
        // [Export] NodePath may not auto-resolve on private fields in CanvasLayer.
        // Fallback: find the camera via the scene tree (all _Ready calls have completed).
        _camera ??= GetTree().CurrentScene.GetNode<Camera3D>("PlayerNode/Camera3D");

        if (_camera is null)
        {
            _logger.Warning("HUDNode: Camera3D not found — debug look direction unavailable.");
            return;
        }

        _debugOverlay.SetCamera(_camera);
        _logger.Info("HUDNode: Camera3D injected into DebugOverlay.");
    }
}
