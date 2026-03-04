namespace MineRPG.RPG.Combat;

/// <summary>
/// Result of a damage calculation. Passed to HealthComponent and EventBus.
/// </summary>
public sealed record HitResult(
    int FinalDamage,
    bool IsCritical,
    DamageType DamageType,
    int SourceEntityId,
    int TargetEntityId);
