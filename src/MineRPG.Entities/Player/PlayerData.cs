namespace MineRPG.Entities.Player;

/// <summary>
/// Pure data container for the player. All gameplay state lives here.
/// The Godot bridge reads from this — it does not store its own state.
/// </summary>
public sealed class PlayerData(PlayerMovementSettings settings)
{
    public PlayerMovementSettings MovementSettings { get; } = settings;

    public float PositionX { get; set; }

    public float PositionY { get; set; }

    public float PositionZ { get; set; }

    public float VelocityX { get; set; }

    public float VelocityY { get; set; }

    public float VelocityZ { get; set; }

    public float CameraPitch { get; set; }

    public float CameraYaw { get; set; }

    public bool IsSprinting { get; set; }

    public ushort SelectedBlockId { get; set; } = 1;
}
