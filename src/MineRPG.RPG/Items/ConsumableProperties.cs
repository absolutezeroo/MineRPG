namespace MineRPG.RPG.Items;

/// <summary>
/// Properties specific to consumable items (food, potions, scrolls).
/// Loaded from JSON as part of an <see cref="ItemDefinition"/>.
/// </summary>
public sealed class ConsumableProperties
{
    /// <summary>The type of consumable determining usage behavior.</summary>
    public ConsumableType Type { get; init; }

    /// <summary>Time in seconds required to consume the item.</summary>
    public float UseTime { get; init; }

    /// <summary>Health points restored on consumption.</summary>
    public float HealthRestore { get; init; }

    /// <summary>Mana points restored on consumption.</summary>
    public float ManaRestore { get; init; }

    /// <summary>Hunger points restored on consumption.</summary>
    public float HungerRestore { get; init; }

    /// <summary>Saturation points restored on consumption.</summary>
    public float SaturationRestore { get; init; }

    /// <summary>Status effects applied on consumption.</summary>
    public IReadOnlyList<StatusEffectApplication> Effects { get; init; } = [];
}
