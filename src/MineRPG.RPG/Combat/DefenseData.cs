namespace MineRPG.RPG.Combat;

/// <summary>
/// Immutable snapshot of defender stats at the moment of receiving an attack.
/// </summary>
/// <param name="Armor">Physical damage reduction value.</param>
/// <param name="Resistance">Elemental damage reduction value.</param>
/// <param name="Weakness">Optional damage type the defender is weak to, or null if none.</param>
/// <param name="DefenderEntityId">Unique identifier of the defending entity.</param>
public readonly record struct DefenseData(
    float Armor,
    float Resistance,
    DamageType? Weakness,
    int DefenderEntityId);
