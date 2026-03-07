#if DEBUG
using Godot;

using MineRPG.Core.Logging;
using MineRPG.Game.Bootstrap.Input;

namespace MineRPG.Godot.UI.Debug;

/// <summary>
/// Handles all debug key bindings (F1-F8) and dispatches toggle commands
/// back to the <see cref="DebugManager"/>.
/// </summary>
internal sealed class DebugInputHandler
{
    private readonly DebugManager _manager;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a debug input handler.
    /// </summary>
    /// <param name="manager">The debug manager to dispatch toggle commands to.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public DebugInputHandler(DebugManager manager, ILogger logger)
    {
        _manager = manager;
        _logger = logger;
    }

    /// <summary>
    /// Processes an input event and dispatches to the appropriate toggle method.
    /// </summary>
    /// <param name="event">The input event to handle.</param>
    /// <param name="viewport">The viewport for consuming input.</param>
    /// <returns>True if the input was handled.</returns>
    public bool HandleInput(InputEvent @event, Viewport viewport)
    {
        if (@event.IsActionPressed(InputActions.DebugMenu))
        {
            _manager.ToggleDebugMenu();
            viewport.SetInputAsHandled();
            return true;
        }

        if (@event.IsActionPressed(InputActions.DebugHud))
        {
            _manager.ToggleHudPanel();
            viewport.SetInputAsHandled();
            return true;
        }

        if (@event.IsActionPressed(InputActions.DebugChunkMap))
        {
            _manager.ToggleChunkMapPanel();
            viewport.SetInputAsHandled();
            return true;
        }

        if (@event.IsActionPressed(InputActions.DebugChunkBorder))
        {
            _manager.ToggleChunkBorder();
            viewport.SetInputAsHandled();
            return true;
        }

        if (@event.IsActionPressed(InputActions.DebugPerfGraph))
        {
            _manager.TogglePerfGraph();
            viewport.SetInputAsHandled();
            return true;
        }

        if (@event.IsActionPressed(InputActions.DebugBiomeOverlay))
        {
            _manager.ToggleBiomeOverlay();
            viewport.SetInputAsHandled();
            return true;
        }

        if (@event.IsActionPressed(InputActions.DebugBiomeOverlayMode))
        {
            _manager.CycleBiomeOverlayMode();
            viewport.SetInputAsHandled();
            return true;
        }

        if (@event.IsActionPressed(InputActions.DebugBlockInspector))
        {
            _manager.ToggleBlockInspector();
            viewport.SetInputAsHandled();
            return true;
        }

        return false;
    }
}
#endif
