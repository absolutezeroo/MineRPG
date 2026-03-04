namespace MineRPG.RPG.Combat;

/// <summary>
/// Strategy interface for damage calculation.
/// Implementations are swappable via data configuration,
/// enabling different formulas for PvE, PvP, or custom modes.
/// </summary>
public interface IDamageFormula
{
    HitResult Calculate(AttackData attack, DefenseData defense, Random rng);
}
