namespace MineRPG.Entities.Player.Survival;

/// <summary>
/// Thirst tuning parameters loaded from Data/Player/survival_settings.json.
/// </summary>
public sealed class ThirstSettings
{
    private const float DefaultMaxThirst = 20f;
    private const float DefaultPassiveDecayRate = 0.04f;
    private const float DefaultSprintDecayRate = 0.08f;
    private const float DefaultDehydrationDamageInterval = 5f;
    private const float DefaultDehydrationDamageAmount = 1f;

    /// <summary>Maximum thirst points.</summary>
    public float MaxThirst { get; init; } = DefaultMaxThirst;

    /// <summary>Thirst points lost per second from passive metabolism.</summary>
    public float PassiveDecayRate { get; init; } = DefaultPassiveDecayRate;

    /// <summary>Additional thirst points lost per second while sprinting.</summary>
    public float SprintDecayRate { get; init; } = DefaultSprintDecayRate;

    /// <summary>Seconds between dehydration damage ticks when thirst reaches zero.</summary>
    public float DehydrationDamageInterval { get; init; } = DefaultDehydrationDamageInterval;

    /// <summary>Damage dealt per dehydration tick.</summary>
    public float DehydrationDamageAmount { get; init; } = DefaultDehydrationDamageAmount;
}
