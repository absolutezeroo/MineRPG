namespace MineRPG.RPG.Items;

/// <summary>
/// Defines a status effect applied when consuming an item.
/// </summary>
public sealed class StatusEffectApplication
{
    /// <summary>Identifier of the effect to apply.</summary>
    public string EffectId { get; init; } = "";

    /// <summary>Duration of the effect in seconds.</summary>
    public float Duration { get; init; }

    /// <summary>Intensity level of the effect (1, 2, 3, etc.).</summary>
    public int Level { get; init; } = 1;

    /// <summary>Probability of the effect being applied, from 0.0 to 1.0.</summary>
    public float Chance { get; init; } = 1.0f;
}
