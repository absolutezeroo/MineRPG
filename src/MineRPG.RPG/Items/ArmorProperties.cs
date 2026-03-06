namespace MineRPG.RPG.Items;

/// <summary>
/// Properties specific to armor items.
/// Loaded from JSON as part of an <see cref="ItemDefinition"/>.
/// </summary>
public sealed class ArmorProperties
{
    /// <summary>The equipment slot this armor occupies.</summary>
    public ArmorSlotType Slot { get; init; }

    /// <summary>Base defense value reducing incoming damage.</summary>
    public float Defense { get; init; }

    /// <summary>Toughness value reducing high-damage hits.</summary>
    public float Toughness { get; init; }

    /// <summary>Weight affecting movement speed.</summary>
    public float Weight { get; init; }

    /// <summary>Damage types this armor provides resistance against.</summary>
    public IReadOnlyList<string> Resistances { get; init; } = [];
}
