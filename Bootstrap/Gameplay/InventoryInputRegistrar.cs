using Godot;

using MineRPG.Core.Logging;
using MineRPG.Godot.UI;

namespace MineRPG.Game.Bootstrap.Gameplay;

/// <summary>
/// Registers the inventory toggle input action into Godot's InputMap at startup.
/// Called from <see cref="GameBootstrapper._Ready"/>.
/// </summary>
public static class InventoryInputRegistrar
{
    /// <summary>
    /// Registers the "inventory_toggle" action mapped to the E key.
    /// Skips registration if the action already exists (e.g., defined in project.godot).
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public static void Register(ILogger logger)
    {
        StringName actionName = InputActionNames.InventoryToggle;

        if (InputMap.HasAction(actionName))
        {
            return;
        }

        InputMap.AddAction(actionName);

        InputEventKey keyEvent = new();
        keyEvent.PhysicalKeycode = Key.E;
        InputMap.ActionAddEvent(actionName, keyEvent);

        logger.Debug("InventoryInputRegistrar: Registered '{0}' -> E", actionName);
    }
}
