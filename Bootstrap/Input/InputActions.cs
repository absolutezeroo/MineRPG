using Godot;

namespace MineRPG.Game.Bootstrap.Input;

/// <summary>
/// Single source of truth for all InputMap action name constants.
/// Every <see cref="StringName"/> is allocated once at startup and reused
/// thereafter, avoiding repeated string allocations on every input poll.
///
/// Names must match the Godot InputMap configuration (project.godot or
/// registered programmatically by <see cref="InputActionRegistrar"/>).
/// </summary>
public static class InputActions
{
    // ------------------------------------------------------------------ Movement

    /// <summary>Move forward action (W).</summary>
    public static readonly StringName MoveForward = new("move_forward");

    /// <summary>Move backward action (S).</summary>
    public static readonly StringName MoveBack = new("move_back");

    /// <summary>Strafe left action (A).</summary>
    public static readonly StringName MoveLeft = new("move_left");

    /// <summary>Strafe right action (D).</summary>
    public static readonly StringName MoveRight = new("move_right");

    /// <summary>Jump action (Space).</summary>
    public static readonly StringName Jump = new("jump");

    /// <summary>Sprint action (Shift).</summary>
    public static readonly StringName Sprint = new("sprint");

    // ------------------------------------------------------------------ Interaction

    /// <summary>Attack / break block action (Left Mouse Button, held).</summary>
    public static readonly StringName Attack = new("attack");

    /// <summary>Interact / place block action (Right Mouse Button).</summary>
    public static readonly StringName Interact = new("interact");

    // ------------------------------------------------------------------ UI / System

    /// <summary>Pause / cancel action (Escape). Maps to Godot's built-in ui_cancel.</summary>
    public static readonly StringName Pause = new("ui_cancel");

    /// <summary>Toggle inventory action (E).</summary>
    public static readonly StringName InventoryToggle = new("inventory_toggle");

    /// <summary>Toggle debug overlay action (F3).</summary>
    public static readonly StringName DebugToggle = new("debug_toggle");

    // ------------------------------------------------------------------ Fly mode

    /// <summary>Toggle fly mode action (F).</summary>
    public static readonly StringName ToggleFly = new("toggle_fly");

    /// <summary>Increase fly speed action.</summary>
    public static readonly StringName FlySpeedUp = new("fly_speed_up");

    /// <summary>Decrease fly speed action.</summary>
    public static readonly StringName FlySpeedDown = new("fly_speed_down");

#if DEBUG
    // ------------------------------------------------------------------ Debug (F1-F8)

    /// <summary>Debug menu toggle (F1).</summary>
    public static readonly StringName DebugMenu = new("debug_menu");

    /// <summary>Debug HUD panel toggle (F3).</summary>
    public static readonly StringName DebugHud = new("debug_hud");

    /// <summary>Chunk map panel toggle (F4).</summary>
    public static readonly StringName DebugChunkMap = new("debug_chunk_map");

    /// <summary>Chunk border wireframe toggle (F5).</summary>
    public static readonly StringName DebugChunkBorder = new("debug_chunk_border");

    /// <summary>Performance graphs toggle (F6).</summary>
    public static readonly StringName DebugPerfGraph = new("debug_perf_graph");

    /// <summary>Biome overlay toggle (F7).</summary>
    public static readonly StringName DebugBiomeOverlay = new("debug_biome_overlay");

    /// <summary>Biome overlay sub-mode cycle (Shift+F7).</summary>
    public static readonly StringName DebugBiomeOverlayMode = new("debug_biome_overlay_mode");

    /// <summary>Block inspector toggle (F8).</summary>
    public static readonly StringName DebugBlockInspector = new("debug_block_inspector");
#endif

    // ------------------------------------------------------------------ Rebind metadata

    /// <summary>
    /// Ordered list of all player-rebindable actions shown in the Controls options tab.
    /// Display order matches the array order. Debug-only actions are excluded.
    /// </summary>
    public static readonly RebindableActionData[] RebindableActions =
    [
        new("move_forward", "Move Forward"),
        new("move_back", "Move Backward"),
        new("move_left", "Strafe Left"),
        new("move_right", "Strafe Right"),
        new("jump", "Jump"),
        new("sprint", "Sprint"),
        new("attack", "Break Block"),
        new("interact", "Place Block"),
        new("inventory_toggle", "Open Inventory"),
        new("debug_toggle", "Debug Overlay"),
        new("toggle_fly", "Toggle Fly"),
        new("fly_speed_up", "Fly Speed Up"),
        new("fly_speed_down", "Fly Speed Down"),
    ];
}
