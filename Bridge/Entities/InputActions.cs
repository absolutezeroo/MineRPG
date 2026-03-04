using Godot;

namespace MineRPG.Godot.Entities;

/// <summary>
/// Centralized StringName constants for all input actions.
/// Avoids string allocation on every Input.IsActionPressed call.
/// Names must match Godot's InputMap configuration.
/// </summary>
public static class InputActions
{
    public static readonly StringName MoveForward = new("move_forward");
    public static readonly StringName MoveBack = new("move_back");
    public static readonly StringName MoveLeft = new("move_left");
    public static readonly StringName MoveRight = new("move_right");
    public static readonly StringName Jump = new("jump");
    public static readonly StringName Sprint = new("sprint");
    public static readonly StringName Attack = new("attack");
    public static readonly StringName Interact = new("interact");
    public static readonly StringName Pause = new("ui_cancel");
    public static readonly StringName DebugToggle = new("debug_toggle");
}
