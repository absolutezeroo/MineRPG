namespace MineRPG.Core.DataLoading;

/// <summary>
/// Serializable descriptor for a single input action binding.
/// Uses raw integer keycodes to avoid a GodotSharp dependency in Core.
/// </summary>
public sealed class KeybindData
{
    /// <summary>Physical keycode (Godot Key enum as int). -1 if this is a mouse button binding.</summary>
    public int PhysicalKeycode { get; set; } = -1;

    /// <summary>Mouse button index (Godot MouseButton enum as int). -1 if this is a key binding.</summary>
    public int MouseButton { get; set; } = -1;
}
