namespace MineRPG.RPG.Stats;

/// <summary>
/// A modifier applied to a stat. Flat adds a fixed amount,
/// PercentAdd sums before multiplying, PercentMultiply chains multiplicatively.
/// </summary>
/// <param name="Type">How this modifier is applied to the base value.</param>
/// <param name="Value">The numeric value of the modifier.</param>
/// <param name="Source">Optional identifier of the source that applied this modifier.</param>
public readonly record struct StatModifier(ModifierType Type, float Value, string? Source = null);
