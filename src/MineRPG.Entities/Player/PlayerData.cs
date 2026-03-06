using MineRPG.RPG.Inventory;

namespace MineRPG.Entities.Player;

/// <summary>
/// Pure data container for the player. All gameplay state lives here.
/// The Godot bridge reads from this — it does not store its own state.
/// </summary>
public sealed class PlayerData
{
    /// <summary>Default block ID selected in the hotbar.</summary>
    private const ushort DefaultSelectedBlockId = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerData"/> class.
    /// </summary>
    /// <param name="settings">Movement tuning parameters loaded from data files.</param>
    public PlayerData(PlayerMovementSettings settings)
    {
        MovementSettings = settings;
        CurrentFlySpeed = settings.FlySpeed;
    }

    /// <summary>Movement tuning parameters for this player.</summary>
    public PlayerMovementSettings MovementSettings { get; }

    /// <summary>World X position of the player.</summary>
    public float PositionX { get; set; }

    /// <summary>World Y position of the player.</summary>
    public float PositionY { get; set; }

    /// <summary>World Z position of the player.</summary>
    public float PositionZ { get; set; }

    /// <summary>X component of the player velocity.</summary>
    public float VelocityX { get; set; }

    /// <summary>Y component of the player velocity.</summary>
    public float VelocityY { get; set; }

    /// <summary>Z component of the player velocity.</summary>
    public float VelocityZ { get; set; }

    /// <summary>Camera pitch angle in radians.</summary>
    public float CameraPitch { get; set; }

    /// <summary>Camera yaw angle in radians.</summary>
    public float CameraYaw { get; set; }

    /// <summary>Whether the player is currently sprinting.</summary>
    public bool IsSprinting { get; set; }

    /// <summary>Whether the player is currently in fly mode.</summary>
    public bool IsFlying { get; set; }

    /// <summary>Current fly speed in blocks per second. Adjustable at runtime.</summary>
    public float CurrentFlySpeed { get; set; }

    /// <summary>Block ID currently selected in the hotbar (legacy, used for placement).</summary>
    public ushort SelectedBlockId { get; set; } = DefaultSelectedBlockId;

    /// <summary>The player's inventory system. Null until initialized by CompositionRoot.</summary>
    public PlayerInventory? Inventory { get; set; }

    /// <summary>The index of the currently selected hotbar slot (0-8).</summary>
    public int SelectedHotbarSlot { get; set; }
}
