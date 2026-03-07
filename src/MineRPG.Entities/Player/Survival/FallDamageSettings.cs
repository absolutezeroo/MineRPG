namespace MineRPG.Entities.Player.Survival;

/// <summary>
/// Tuning parameters for Minecraft-style fall damage.
/// Falls below <see cref="SafeFallDistance"/> cause no damage.
/// Each block beyond that deals <see cref="DamagePerBlock"/> HP.
/// </summary>
public sealed class FallDamageSettings
{
    /// <summary>
    /// Maximum number of blocks a player can fall without taking damage.
    /// Minecraft default is 3 blocks.
    /// </summary>
    public float SafeFallDistance { get; init; } = 3.0f;

    /// <summary>
    /// HP damage dealt per block fallen beyond <see cref="SafeFallDistance"/>.
    /// </summary>
    public float DamagePerBlock { get; init; } = 1.0f;

    /// <summary>
    /// Hard cap on fall damage regardless of distance fallen.
    /// </summary>
    public float MaxFallDamage { get; init; } = 200.0f;

    /// <summary>
    /// When true, landing in water negates all fall damage.
    /// </summary>
    public bool WaterNegatesFallDamage { get; init; } = true;
}
