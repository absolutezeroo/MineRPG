namespace MineRPG.Entities.Player.Survival;

/// <summary>
/// Health tuning parameters loaded from Data/Player/survival_settings.json.
/// </summary>
public sealed class HealthSettings
{
    private const float DefaultMaxHealth = 20f;
    private const float DefaultNaturalRegenRate = 1f;
    private const float DefaultNaturalRegenDelay = 2.5f;
    private const float DefaultHungerRegenThreshold = 18f;
    private const float DefaultStarveDamageRate = 1f;
    private const float DefaultStarveDamageInterval = 4f;

    /// <summary>Maximum health points.</summary>
    public float MaxHealth { get; init; } = DefaultMaxHealth;

    /// <summary>Health points regenerated per second when conditions are met.</summary>
    public float NaturalRegenRate { get; init; } = DefaultNaturalRegenRate;

    /// <summary>Seconds after taking damage before natural regen resumes.</summary>
    public float NaturalRegenDelay { get; init; } = DefaultNaturalRegenDelay;

    /// <summary>Minimum hunger level required for natural health regeneration.</summary>
    public float HungerRegenThreshold { get; init; } = DefaultHungerRegenThreshold;

    /// <summary>Damage dealt per tick when starving.</summary>
    public float StarveDamageRate { get; init; } = DefaultStarveDamageRate;

    /// <summary>Seconds between starvation damage ticks.</summary>
    public float StarveDamageInterval { get; init; } = DefaultStarveDamageInterval;
}
