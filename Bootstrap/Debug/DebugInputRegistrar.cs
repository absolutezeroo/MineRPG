#if DEBUG
using Godot;

using MineRPG.Core.Logging;

namespace MineRPG.Game.Bootstrap.Debug;

/// <summary>
/// Registers debug input actions (F1-F8) into Godot's InputMap at startup.
/// Called from <see cref="GameBootstrapper"/> under #if DEBUG.
/// These actions correspond to <see cref="MineRPG.Godot.UI.InputActionNames"/>.
/// </summary>
public static class DebugInputRegistrar
{
    /// <summary>
    /// Registers all debug input actions in the InputMap.
    /// Skips any action that already exists (e.g., defined in project.godot).
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public static void RegisterAll(ILogger logger)
    {
        int registered = 0;

        registered += RegisterKeyAction("debug_menu", Key.F1, logger);
        registered += RegisterKeyAction("debug_hud", Key.F3, logger);
        registered += RegisterKeyAction("debug_chunk_map", Key.F4, logger);
        registered += RegisterKeyAction("debug_chunk_border", Key.F5, logger);
        registered += RegisterKeyAction("debug_perf_graph", Key.F6, logger);
        registered += RegisterKeyAction("debug_biome_overlay", Key.F7, logger);
        registered += RegisterShiftKeyAction("debug_biome_overlay_mode", Key.F7, logger);
        registered += RegisterKeyAction("debug_block_inspector", Key.F8, logger);

        logger.Debug("DebugInputRegistrar: Registered {0} debug input actions.", registered);
    }

    private static int RegisterKeyAction(string actionName, Key key, ILogger logger)
    {
        if (InputMap.HasAction(actionName))
        {
            return 0;
        }

        InputMap.AddAction(actionName);

        InputEventKey keyEvent = new();
        keyEvent.PhysicalKeycode = key;
        InputMap.ActionAddEvent(actionName, keyEvent);

        logger.Debug("DebugInputRegistrar: Registered '{0}' -> {1}", actionName, key);
        return 1;
    }

    private static int RegisterShiftKeyAction(string actionName, Key key, ILogger logger)
    {
        if (InputMap.HasAction(actionName))
        {
            return 0;
        }

        InputMap.AddAction(actionName);

        InputEventKey keyEvent = new();
        keyEvent.PhysicalKeycode = key;
        keyEvent.ShiftPressed = true;
        InputMap.ActionAddEvent(actionName, keyEvent);

        logger.Debug("DebugInputRegistrar: Registered '{0}' -> Shift+{1}", actionName, key);
        return 1;
    }
}
#endif
