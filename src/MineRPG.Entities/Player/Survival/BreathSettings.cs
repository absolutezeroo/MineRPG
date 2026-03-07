namespace MineRPG.Entities.Player.Survival;

/// <summary>
/// Breath tuning parameters loaded from Data/Player/survival_settings.json.
/// </summary>
public sealed class BreathSettings
{
    private const float DefaultMaxBreath = 15f;
    private const float DefaultDrainRate = 1f;
    private const float DefaultRegenRate = 4f;
    private const float DefaultDrowningDamageInterval = 1f;
    private const float DefaultDrowningDamageAmount = 2f;

    /// <summary>Maximum breath in seconds of air supply.</summary>
    public float MaxBreath { get; init; } = DefaultMaxBreath;

    /// <summary>Breath points lost per second while submerged.</summary>
    public float DrainRate { get; init; } = DefaultDrainRate;

    /// <summary>Breath points recovered per second while above water.</summary>
    public float RegenRate { get; init; } = DefaultRegenRate;

    /// <summary>Seconds between drowning damage ticks when breath reaches zero.</summary>
    public float DrowningDamageInterval { get; init; } = DefaultDrowningDamageInterval;

    /// <summary>Damage dealt per drowning tick.</summary>
    public float DrowningDamageAmount { get; init; } = DefaultDrowningDamageAmount;
}
