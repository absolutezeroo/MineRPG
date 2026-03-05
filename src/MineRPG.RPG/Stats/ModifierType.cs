namespace MineRPG.RPG.Stats;

/// <summary>
/// How a stat modifier is applied to the base value.
/// </summary>
public enum ModifierType
{
    /// <summary>Adds a fixed amount to the base value.</summary>
    Flat,

    /// <summary>Adds a percentage that sums with other PercentAdd modifiers before multiplying.</summary>
    PercentAdd,

    /// <summary>Multiplies the result independently, chaining with other PercentMultiply modifiers.</summary>
    PercentMultiply,
}
