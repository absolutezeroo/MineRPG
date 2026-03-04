namespace MineRPG.RPG.Combat;

/// <summary>
/// Immutable snapshot of attacker stats at the moment of an attack.
/// </summary>
public sealed record AttackData(
    float BaseDamage,
    DamageType DamageType,
    float CritChance,
    float CritMultiplier,
    int SourceEntityId);
