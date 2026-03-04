namespace MineRPG.RPG.Combat;

/// <summary>
/// Strategy interface for damage calculation.
/// Implementations are swappable via data configuration,
/// enabling different formulas for PvE, PvP, or custom modes.
/// </summary>
public interface IDamageFormula
{
    /// <summary>
    /// Calculates the result of an attack against a defender.
    /// </summary>
    /// <param name="attack">Snapshot of the attacker's offensive stats.</param>
    /// <param name="defense">Snapshot of the defender's defensive stats.</param>
    /// <param name="rng">Random number generator for crit rolls and variance.</param>
    /// <returns>A hit result containing final damage, crit status, and entity identifiers.</returns>
    HitResult Calculate(AttackData attack, DefenseData defense, Random rng);
}
