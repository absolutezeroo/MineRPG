using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Logging;

#if DEBUG
using MineRPG.Godot.UI.Debug;
#endif

using MineRPG.Godot.UI.Screens;

namespace MineRPG.Godot.UI.HUD;

/// <summary>
/// CanvasLayer container for all in-game HUD elements.
/// Creates and owns CrosshairNode, HotbarNode, PauseMenuNode, and
/// (in DEBUG builds) DebugManager for all debug overlays.
/// The Camera3D reference is wired via [Export] with a fallback
/// resolution in CallDeferred (same pattern as PlayerNode).
/// </summary>
public sealed partial class HUDNode : CanvasLayer
{
    [Export] private Camera3D _camera = null!;

#if DEBUG
    private DebugManager _debugManager = null!;
#endif
    private ILogger _logger = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        _logger = ServiceLocator.Instance.Get<ILogger>();

        // HUD must always process (even when paused) so pause menu works
        ProcessMode = ProcessModeEnum.Always;

        CrosshairNode crosshair = new();
        crosshair.Name = "Crosshair";
        AddChild(crosshair);

#if DEBUG
        _debugManager = new DebugManager();
        _debugManager.Name = "DebugManager";
        AddChild(_debugManager);
#endif

        HotbarNode hotbar = new();
        hotbar.Name = "Hotbar";
        AddChild(hotbar);

        PauseMenuNode pauseMenu = new();
        pauseMenu.Name = "PauseMenu";
        AddChild(pauseMenu);

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

#if DEBUG
        _debugManager.SetCamera(_camera);
#endif
        _logger.Info("HUDNode: Camera3D injected.");
    }
}
