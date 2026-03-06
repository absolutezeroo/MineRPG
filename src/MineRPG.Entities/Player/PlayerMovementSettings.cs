namespace MineRPG.Entities.Player;

/// <summary>
/// Loaded from Data/Player/movement_settings.json.
/// All movement tuning lives here — nothing hardcoded in the bridge.
/// </summary>
public sealed class PlayerMovementSettings
{
    /// <summary>Default walk speed in blocks per second.</summary>
    private const float DefaultWalkSpeed = 5.0f;

    /// <summary>Default sprint speed in blocks per second.</summary>
    private const float DefaultSprintSpeed = 8.0f;

    /// <summary>Default jump velocity in blocks per second.</summary>
    private const float DefaultJumpVelocity = 7.0f;

    /// <summary>Default gravity acceleration in blocks per second squared.</summary>
    private const float DefaultGravity = 20.0f;

    /// <summary>Default mouse sensitivity in radians per pixel.</summary>
    private const float DefaultMouseSensitivity = 0.002f;

    /// <summary>Default interaction range in blocks.</summary>
    private const float DefaultInteractionRange = 5.0f;

    /// <summary>Default number of hotbar slots.</summary>
    private const int DefaultHotbarSize = 9;

    /// <summary>Default fly speed in blocks per second.</summary>
    private const float DefaultFlySpeed = 10.0f;

    /// <summary>Minimum fly speed in blocks per second.</summary>
    private const float DefaultMinFlySpeed = 1.0f;

    /// <summary>Maximum fly speed in blocks per second.</summary>
    private const float DefaultMaxFlySpeed = 50.0f;

    /// <summary>Speed increment per key press in blocks per second.</summary>
    private const float DefaultFlySpeedStep = 2.0f;

    /// <summary>Walk speed in blocks per second.</summary>
    public float WalkSpeed { get; init; } = DefaultWalkSpeed;

    /// <summary>Sprint speed in blocks per second.</summary>
    public float SprintSpeed { get; init; } = DefaultSprintSpeed;

    /// <summary>Upward velocity applied when jumping, in blocks per second.</summary>
    public float JumpVelocity { get; init; } = DefaultJumpVelocity;

    /// <summary>Gravity acceleration in blocks per second squared.</summary>
    public float Gravity { get; init; } = DefaultGravity;

    /// <summary>Mouse sensitivity in radians per pixel.</summary>
    public float MouseSensitivity { get; set; } = DefaultMouseSensitivity;

    /// <summary>Maximum distance the player can interact with blocks, in blocks.</summary>
    public float InteractionRange { get; init; } = DefaultInteractionRange;

    /// <summary>Number of slots in the player hotbar.</summary>
    public int HotbarSize { get; init; } = DefaultHotbarSize;

    /// <summary>Default fly speed in blocks per second.</summary>
    public float FlySpeed { get; init; } = DefaultFlySpeed;

    /// <summary>Minimum fly speed in blocks per second.</summary>
    public float MinFlySpeed { get; init; } = DefaultMinFlySpeed;

    /// <summary>Maximum fly speed in blocks per second.</summary>
    public float MaxFlySpeed { get; init; } = DefaultMaxFlySpeed;

    /// <summary>Speed change per key press in blocks per second.</summary>
    public float FlySpeedStep { get; init; } = DefaultFlySpeedStep;
}
