namespace MineRPG.Entities.Player;

/// <summary>
/// Loaded from Data/Player/movement_settings.json.
/// All movement tuning lives here — nothing hardcoded in the bridge.
/// </summary>
public sealed class PlayerMovementSettings
{
    public float WalkSpeed { get; init; } = 5.0f;
    public float SprintSpeed { get; init; } = 8.0f;
    public float JumpVelocity { get; init; } = 7.0f;
    public float Gravity { get; init; } = 20.0f;
    public float MouseSensitivity { get; init; } = 0.002f;
    public float InteractionRange { get; init; } = 5.0f;
    public int HotbarSize { get; init; } = 9;
}
