using Godot;

namespace MineRPG.Godot.UI;

/// <summary>
/// StringName constants for input actions used by UI nodes.
/// Avoids cross-referencing MineRPG.Godot.Entities (bridge projects
/// must never reference each other).
/// </summary>
public static class InputActionNames
{
    /// <summary>The debug overlay toggle input action (F3 - kept for compatibility).</summary>
    public static readonly StringName DebugToggle = new("debug_toggle");

    /// <summary>The pause/escape input action.</summary>
    public static readonly StringName Pause = new("ui_cancel");

#if DEBUG
    /// <summary>Debug menu toggle (F1).</summary>
    public static readonly StringName DebugMenu = new("debug_menu");

    /// <summary>Debug HUD toggle (F3).</summary>
    public static readonly StringName DebugHud = new("debug_hud");

    /// <summary>Chunk map toggle (F4).</summary>
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
}
