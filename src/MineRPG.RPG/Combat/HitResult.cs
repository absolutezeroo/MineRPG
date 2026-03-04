namespace MineRPG.RPG.Combat;

/// <summary>
/// Result of a damage calculation. Passed to HealthComponent and EventBus.
/// </summary>
/// <param name="FinalDamage">The computed damage after all modifiers and reductions.</param>
/// <param name="IsCritical">Whether the hit was a critical strike.</param>
/// <param name="DamageType">The elemental or physical damage type applied.</param>
/// <param name="SourceEntityId">Unique identifier of the attacking entity.</param>
/// <param name="TargetEntityId">Unique identifier of the entity receiving damage.</param>
public readonly record struct HitResult(
    int FinalDamage,
    bool IsCritical,
    DamageType DamageType,
    int SourceEntityId,
    int TargetEntityId);
