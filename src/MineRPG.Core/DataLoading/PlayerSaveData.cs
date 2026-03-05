namespace MineRPG.Core.DataLoading;

/// <summary>
/// Persistent snapshot of player state written to player_save.json
/// in the world's save directory alongside world_meta.json.
/// </summary>
public sealed class PlayerSaveData
{
    private const float DefaultSpawnX = 8f;
    private const float DefaultSpawnY = 80f;
    private const float DefaultSpawnZ = 8f;
    private const ushort DefaultSelectedBlockId = 1;
    private const float DefaultMouseSensitivity = 0.002f;
    private const int DefaultRenderDistance = 32;

    /// <summary>Gets or sets the save format version for forward compatibility.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Gets or sets the world X position of the player.</summary>
    public float PositionX { get; set; } = DefaultSpawnX;

    /// <summary>Gets or sets the world Y position of the player.</summary>
    public float PositionY { get; set; } = DefaultSpawnY;

    /// <summary>Gets or sets the world Z position of the player.</summary>
    public float PositionZ { get; set; } = DefaultSpawnZ;

    /// <summary>Gets or sets the X component of the player velocity.</summary>
    public float VelocityX { get; set; }

    /// <summary>Gets or sets the Y component of the player velocity.</summary>
    public float VelocityY { get; set; }

    /// <summary>Gets or sets the Z component of the player velocity.</summary>
    public float VelocityZ { get; set; }

    /// <summary>Gets or sets the camera yaw angle in radians.</summary>
    public float CameraYaw { get; set; }

    /// <summary>Gets or sets the camera pitch angle in radians.</summary>
    public float CameraPitch { get; set; }

    /// <summary>Gets or sets whether the player was sprinting.</summary>
    public bool IsSprinting { get; set; }

    /// <summary>Gets or sets the block ID selected in the hotbar.</summary>
    public ushort SelectedBlockId { get; set; } = DefaultSelectedBlockId;

    /// <summary>Gets or sets the mouse look sensitivity in radians per pixel.</summary>
    public float MouseSensitivity { get; set; } = DefaultMouseSensitivity;

    /// <summary>Gets or sets the chunk render distance.</summary>
    public int RenderDistance { get; set; } = DefaultRenderDistance;
}
