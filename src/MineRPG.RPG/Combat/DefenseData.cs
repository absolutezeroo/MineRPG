namespace MineRPG.RPG.Combat;

/// <summary>
/// Immutable snapshot of defender stats at the moment of receiving an attack.
/// </summary>
public sealed record DefenseData(
    float Armor,
    float Resistance,
    DamageType? Weakness,
    int DefenderEntityId);
