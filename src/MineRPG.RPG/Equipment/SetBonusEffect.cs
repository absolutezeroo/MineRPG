namespace MineRPG.RPG.Equipment;

/// <summary>
/// A single stat modification granted by an equipment set bonus.
/// </summary>
public sealed class SetBonusEffect
{
    /// <summary>The stat being modified.</summary>
    public string Stat { get; init; } = "";

    /// <summary>Modification type: flatAdd, percentAdd, or percentMultiply.</summary>
    public string ModifierType { get; init; } = "";

    /// <summary>The numeric value of the modification.</summary>
    public float Value { get; init; }
}
