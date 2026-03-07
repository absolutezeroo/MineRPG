namespace MineRPG.Entities.Player.Survival;

/// <summary>
/// Temperature tuning parameters loaded from Data/Player/survival_settings.json.
/// </summary>
public sealed class TemperatureSettings
{
    private const float DefaultComfortMin = -0.15f;
    private const float DefaultComfortMax = 0.35f;
    private const float DefaultAltitudeCoolRate = 0.0015f;
    private const int DefaultAltitudeBaseY = 64;
    private const float DefaultHotDamageInterval = 3f;
    private const float DefaultHotDamageAmount = 1f;
    private const float DefaultColdDamageInterval = 3f;
    private const float DefaultColdDamageAmount = 1f;
    private const float DefaultAdaptationRate = 0.05f;

    /// <summary>Lower bound of the comfort zone in normalized temperature [-1, 1].</summary>
    public float ComfortMin { get; init; } = DefaultComfortMin;

    /// <summary>Upper bound of the comfort zone in normalized temperature [-1, 1].</summary>
    public float ComfortMax { get; init; } = DefaultComfortMax;

    /// <summary>Temperature decrease per block above the altitude baseline.</summary>
    public float AltitudeCoolRate { get; init; } = DefaultAltitudeCoolRate;

    /// <summary>Y level considered the altitude baseline for temperature calculations.</summary>
    public int AltitudeBaseY { get; init; } = DefaultAltitudeBaseY;

    /// <summary>Seconds between heat damage ticks when overheating.</summary>
    public float HotDamageInterval { get; init; } = DefaultHotDamageInterval;

    /// <summary>Damage dealt per overheating tick.</summary>
    public float HotDamageAmount { get; init; } = DefaultHotDamageAmount;

    /// <summary>Seconds between cold damage ticks when freezing.</summary>
    public float ColdDamageInterval { get; init; } = DefaultColdDamageInterval;

    /// <summary>Damage dealt per freezing tick.</summary>
    public float ColdDamageAmount { get; init; } = DefaultColdDamageAmount;

    /// <summary>Speed at which body temperature adapts toward the environment per second.</summary>
    public float AdaptationRate { get; init; } = DefaultAdaptationRate;
}
