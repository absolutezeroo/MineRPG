namespace MineRPG.Entities.Player.Survival;

/// <summary>
/// Hunger and saturation tuning parameters loaded from Data/Player/survival_settings.json.
/// </summary>
public sealed class HungerSettings
{
    private const float DefaultMaxHunger = 20f;
    private const float DefaultMaxSaturation = 20f;
    private const float DefaultInitialSaturation = 5f;
    private const float DefaultPassiveDecayRate = 0.05f;
    private const float DefaultSprintDecayRate = 0.1f;

    /// <summary>Maximum hunger points.</summary>
    public float MaxHunger { get; init; } = DefaultMaxHunger;

    /// <summary>Maximum saturation points. Capped at current hunger level.</summary>
    public float MaxSaturation { get; init; } = DefaultMaxSaturation;

    /// <summary>Saturation level when the player spawns or respawns.</summary>
    public float InitialSaturation { get; init; } = DefaultInitialSaturation;

    /// <summary>Hunger points lost per second from passive metabolism.</summary>
    public float PassiveDecayRate { get; init; } = DefaultPassiveDecayRate;

    /// <summary>Additional hunger points lost per second while sprinting.</summary>
    public float SprintDecayRate { get; init; } = DefaultSprintDecayRate;
}
