using Godot;

namespace MineRPG.Godot.Entities;

/// <summary>
/// Centralized StringName constants for all input actions.
/// Avoids string allocation on every Input.IsActionPressed call.
/// Names must match Godot's InputMap configuration.
/// </summary>
public static class InputActions
{
    /// <summary>The move forward input action.</summary>
    public static readonly StringName MoveForward = new("move_forward");

    /// <summary>The move backward input action.</summary>
    public static readonly StringName MoveBack = new("move_back");

    /// <summary>The move left input action.</summary>
    public static readonly StringName MoveLeft = new("move_left");

    /// <summary>The move right input action.</summary>
    public static readonly StringName MoveRight = new("move_right");

    /// <summary>The jump input action.</summary>
    public static readonly StringName Jump = new("jump");

    /// <summary>The sprint input action.</summary>
    public static readonly StringName Sprint = new("sprint");

    /// <summary>The attack (left click / break block) input action.</summary>
    public static readonly StringName Attack = new("attack");

    /// <summary>The interact (right click / place block) input action.</summary>
    public static readonly StringName Interact = new("interact");

    /// <summary>The pause / cancel input action.</summary>
    public static readonly StringName Pause = new("ui_cancel");

    /// <summary>The debug overlay toggle input action.</summary>
    public static readonly StringName DebugToggle = new("debug_toggle");

    /// <summary>The inventory toggle input action.</summary>
    public static readonly StringName InventoryToggle = new("inventory_toggle");

    /// <summary>Toggle fly mode input action.</summary>
    public static readonly StringName ToggleFly = new("toggle_fly");

    /// <summary>Increase fly speed input action.</summary>
    public static readonly StringName FlySpeedUp = new("fly_speed_up");

    /// <summary>Decrease fly speed input action.</summary>
    public static readonly StringName FlySpeedDown = new("fly_speed_down");
}
