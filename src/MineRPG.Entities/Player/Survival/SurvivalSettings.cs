namespace MineRPG.Entities.Player.Survival;

/// <summary>
/// Root container for all survival tuning parameters.
/// Loaded from Data/Player/survival_settings.json.
/// </summary>
public sealed class SurvivalSettings
{
    /// <summary>Health settings.</summary>
    public HealthSettings Health { get; init; } = new();

    /// <summary>Hunger and saturation settings.</summary>
    public HungerSettings Hunger { get; init; } = new();

    /// <summary>Thirst settings.</summary>
    public ThirstSettings Thirst { get; init; } = new();

    /// <summary>Stamina settings.</summary>
    public StaminaSettings Stamina { get; init; } = new();

    /// <summary>Breath and drowning settings.</summary>
    public BreathSettings Breath { get; init; } = new();

    /// <summary>Temperature and comfort zone settings.</summary>
    public TemperatureSettings Temperature { get; init; } = new();
}
