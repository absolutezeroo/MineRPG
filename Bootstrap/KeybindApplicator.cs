using System.Collections.Generic;

using Godot;

using MineRPG.Core.DataLoading;
using MineRPG.Core.Logging;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Applies saved keybind overrides from <see cref="SettingsData"/> to Godot's InputMap.
/// Called once at startup after settings are loaded.
/// </summary>
public static class KeybindApplicator
{
    /// <summary>
    /// Applies all keybind overrides from the settings snapshot to the runtime InputMap.
    /// Actions not present in the snapshot keep their project.godot defaults.
    /// </summary>
    /// <param name="settings">The loaded settings data.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public static void Apply(SettingsData settings, ILogger logger)
    {
        if (settings.Keybinds.Count == 0)
        {
            logger.Debug("KeybindApplicator: No keybind overrides to apply.");
            return;
        }

        foreach (KeyValuePair<string, KeybindData> pair in settings.Keybinds)
        {
            string actionName = pair.Key;
            KeybindData bindData = pair.Value;

            if (!InputMap.HasAction(actionName))
            {
                logger.Warning("KeybindApplicator: Action '{0}' not found in InputMap — skipping.", actionName);
                continue;
            }

            InputMap.ActionEraseEvents(actionName);

            if (bindData.PhysicalKeycode >= 0)
            {
                InputEventKey keyEvent = new();
                keyEvent.PhysicalKeycode = (Key)bindData.PhysicalKeycode;
                InputMap.ActionAddEvent(actionName, keyEvent);
                logger.Debug("KeybindApplicator: '{0}' -> Key {1}", actionName, (Key)bindData.PhysicalKeycode);
            }
            else if (bindData.MouseButton >= 0)
            {
                InputEventMouseButton mouseEvent = new();
                mouseEvent.ButtonIndex = (MouseButton)bindData.MouseButton;
                InputMap.ActionAddEvent(actionName, mouseEvent);
                logger.Debug("KeybindApplicator: '{0}' -> MouseButton {1}", actionName, (MouseButton)bindData.MouseButton);
            }
        }

        logger.Info("KeybindApplicator: Applied {0} keybind overrides.", settings.Keybinds.Count);
    }
}
