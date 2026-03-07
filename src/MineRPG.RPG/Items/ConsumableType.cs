namespace MineRPG.RPG.Items;

/// <summary>
/// Types of consumable items that determine usage behavior and effects.
/// </summary>
public enum ConsumableType
{
    /// <summary>Food that restores hunger and saturation.</summary>
    Food,

    /// <summary>Potion that applies status effects.</summary>
    Potion,

    /// <summary>Scroll that triggers a one-time magical effect.</summary>
    Scroll,

    /// <summary>Drink that restores thirst.</summary>
    Drink,

    /// <summary>Throwable item that creates an area effect.</summary>
    Throwable,
}
