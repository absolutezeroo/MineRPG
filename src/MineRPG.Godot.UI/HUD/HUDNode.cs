using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Logging;

#if DEBUG
using MineRPG.Godot.UI.Debug;
#endif

namespace MineRPG.Godot.UI.HUD;

/// <summary>
/// CanvasLayer container for all in-game HUD elements.
/// Creates and owns CrosshairNode programmatically, and instantiates
/// Hotbar.tscn and PauseMenu.tscn from packed scenes.
/// In DEBUG builds, also creates the DebugManager for all debug overlays.
/// </summary>
public sealed partial class HUDNode : CanvasLayer
{
    private const string CrosshairScenePath = "res://Scenes/UI/HUD/Crosshair.tscn";
    private const string HotbarScenePath = "res://Scenes/UI/HUD/Hotbar.tscn";
    private const string PauseMenuScenePath = "res://Scenes/UI/PauseMenu.tscn";
    private const string InventoryScenePath = "res://Scenes/UI/Inventory.tscn";

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

        PackedScene crosshairScene = GD.Load<PackedScene>(CrosshairScenePath);
        Node crosshair = crosshairScene.Instantiate();
        crosshair.Name = "Crosshair";
        AddChild(crosshair);

#if DEBUG
        _debugManager = new DebugManager();
        _debugManager.Name = "DebugManager";
        AddChild(_debugManager);
#endif

        PackedScene hotbarScene = GD.Load<PackedScene>(HotbarScenePath);
        Node hotbar = hotbarScene.Instantiate();
        hotbar.Name = "Hotbar";
        AddChild(hotbar);

        PackedScene pauseMenuScene = GD.Load<PackedScene>(PauseMenuScenePath);
        Node pauseMenu = pauseMenuScene.Instantiate();
        pauseMenu.Name = "PauseMenu";
        AddChild(pauseMenu);

        PackedScene inventoryScene = GD.Load<PackedScene>(InventoryScenePath);
        Node inventory = inventoryScene.Instantiate();
        inventory.Name = "Inventory";
        AddChild(inventory);

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
