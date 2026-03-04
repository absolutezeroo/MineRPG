namespace MineRPG.RPG.Stats;

/// <summary>
/// A modifier applied to a stat. Flat adds a fixed amount,
/// PercentAdd sums before multiplying, PercentMultiply chains multiplicatively.
/// </summary>
public sealed record StatModifier(ModifierType Type, float Value, string? Source = null);
