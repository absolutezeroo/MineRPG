namespace MineRPG.Entities.Player.Survival;

/// <summary>
/// Stamina tuning parameters loaded from Data/Player/survival_settings.json.
/// </summary>
public sealed class StaminaSettings
{
    private const float DefaultMaxStamina = 100f;
    private const float DefaultSprintDrainRate = 20f;
    private const float DefaultRegenRate = 15f;
    private const float DefaultRegenDelay = 1.5f;
    private const float DefaultMinStaminaToSprint = 10f;

    /// <summary>Maximum stamina points.</summary>
    public float MaxStamina { get; init; } = DefaultMaxStamina;

    /// <summary>Stamina points drained per second while sprinting.</summary>
    public float SprintDrainRate { get; init; } = DefaultSprintDrainRate;

    /// <summary>Stamina points regenerated per second when not sprinting.</summary>
    public float RegenRate { get; init; } = DefaultRegenRate;

    /// <summary>Seconds after sprinting stops before regen begins.</summary>
    public float RegenDelay { get; init; } = DefaultRegenDelay;

    /// <summary>Minimum stamina required to begin sprinting.</summary>
    public float MinStaminaToSprint { get; init; } = DefaultMinStaminaToSprint;
}
