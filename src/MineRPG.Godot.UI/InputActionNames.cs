using Godot;

namespace MineRPG.Godot.UI;

/// <summary>
/// StringName constants for input actions used by UI nodes.
/// Avoids cross-referencing MineRPG.Godot.Entities (bridge projects
/// must never reference each other).
/// </summary>
public static class InputActionNames
{
    /// <summary>The debug overlay toggle input action.</summary>
    public static readonly StringName DebugToggle = new("debug_toggle");

    /// <summary>The pause/escape input action.</summary>
    public static readonly StringName Pause = new("ui_cancel");
}
