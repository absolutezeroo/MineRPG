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
    private const float DefaultMaxHealth = 20f;
    private const float DefaultMaxHunger = 20f;
    private const float DefaultStartSaturation = 5f;
    private const float DefaultMaxThirst = 20f;
    private const float DefaultMaxStamina = 100f;
    private const float DefaultMaxBreath = 15f;

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

    /// <summary>Gets or sets the player's health at save time.</summary>
    public float Health { get; set; } = DefaultMaxHealth;

    /// <summary>Gets or sets the player's hunger at save time.</summary>
    public float Hunger { get; set; } = DefaultMaxHunger;

    /// <summary>Gets or sets the player's saturation at save time.</summary>
    public float Saturation { get; set; } = DefaultStartSaturation;

    /// <summary>Gets or sets the player's thirst at save time.</summary>
    public float Thirst { get; set; } = DefaultMaxThirst;

    /// <summary>Gets or sets the player's stamina at save time.</summary>
    public float Stamina { get; set; } = DefaultMaxStamina;

    /// <summary>Gets or sets the player's breath at save time.</summary>
    public float Breath { get; set; } = DefaultMaxBreath;

    /// <summary>Gets or sets the player's body temperature at save time (normalized [-1, 1]).</summary>
    public float BodyTemperature { get; set; } = 0.1f;

    /// <summary>Gets or sets the world spawn X.</summary>
    public float SpawnX { get; set; } = DefaultSpawnX;

    /// <summary>Gets or sets the world spawn Y.</summary>
    public float SpawnY { get; set; } = DefaultSpawnY;

    /// <summary>Gets or sets the world spawn Z.</summary>
    public float SpawnZ { get; set; } = DefaultSpawnZ;
}
