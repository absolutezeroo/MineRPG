namespace MineRPG.RPG.Combat;

/// <summary>
/// Immutable snapshot of attacker stats at the moment of an attack.
/// </summary>
/// <param name="BaseDamage">The raw base damage before modifiers.</param>
/// <param name="DamageType">The elemental or physical damage type.</param>
/// <param name="CritChance">Probability of a critical hit, expressed as 0.0 to 1.0.</param>
/// <param name="CritMultiplier">Multiplier applied to damage on a critical hit.</param>
/// <param name="SourceEntityId">Unique identifier of the attacking entity.</param>
public readonly record struct AttackData(
    float BaseDamage,
    DamageType DamageType,
    float CritChance,
    float CritMultiplier,
    int SourceEntityId);
