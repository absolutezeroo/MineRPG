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
}
