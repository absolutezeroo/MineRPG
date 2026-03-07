using Godot;

using MineRPG.Core.Logging;

namespace MineRPG.Game.Bootstrap.Input;

/// <summary>
/// Registers all input actions that are not defined in project.godot into
/// Godot's InputMap at startup. Called once from <see cref="GameBootstrapper"/>.
/// Skips any action that already exists — project.godot definitions take priority.
/// </summary>
public static class InputActionRegistrar
{
    /// <summary>
    /// Registers all gameplay and (in DEBUG builds) debug input actions.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public static void RegisterAll(ILogger logger)
    {
        int registered = 0;

        registered += RegisterKey(InputActions.InventoryToggle, Key.E, logger);

#if DEBUG
        registered += RegisterKey(InputActions.DebugMenu, Key.F1, logger);
        registered += RegisterKey(InputActions.DebugHud, Key.F3, logger);
        registered += RegisterKey(InputActions.DebugChunkMap, Key.F4, logger);
        registered += RegisterKey(InputActions.DebugChunkBorder, Key.F5, logger);
        registered += RegisterKey(InputActions.DebugPerfGraph, Key.F6, logger);
        registered += RegisterKey(InputActions.DebugBiomeOverlay, Key.F7, logger);
        registered += RegisterShiftKey(InputActions.DebugBiomeOverlayMode, Key.F7, logger);
        registered += RegisterKey(InputActions.DebugBlockInspector, Key.F8, logger);
#endif

        logger.Info("InputActionRegistrar: Registered {0} input actions.", registered);
    }

    private static int RegisterKey(StringName actionName, Key key, ILogger logger)
    {
        if (InputMap.HasAction(actionName))
        {
            return 0;
        }

        InputMap.AddAction(actionName);

        InputEventKey keyEvent = new();
        keyEvent.PhysicalKeycode = key;
        InputMap.ActionAddEvent(actionName, keyEvent);

        logger.Debug("InputActionRegistrar: Registered '{0}' -> {1}", actionName, key);
        return 1;
    }

    private static int RegisterShiftKey(StringName actionName, Key key, ILogger logger)
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

        logger.Debug("InputActionRegistrar: Registered '{0}' -> Shift+{1}", actionName, key);
        return 1;
    }
}
