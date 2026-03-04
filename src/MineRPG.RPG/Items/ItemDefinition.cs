namespace MineRPG.RPG.Items;

/// <summary>
/// Data-driven item definition loaded from Data/Items/*.json.
/// Registered in an <see cref="MineRPG.Core.Registry.IRegistry{TKey, TValue}"/>.
/// </summary>
public sealed class ItemDefinition
{
    /// <summary>Unique numeric identifier for this item.</summary>
    public int Id { get; init; }

    /// <summary>Display name of the item.</summary>
    public string Name { get; init; } = "";

    /// <summary>Item category such as Weapon, Armor, or Consumable.</summary>
    public string Type { get; init; } = "";

    /// <summary>Rarity tier used for loot generation and display color coding.</summary>
    public ItemRarity Rarity { get; init; }

    /// <summary>Maximum number of items that can be stacked in a single slot.</summary>
    public int MaxStack { get; init; } = 64;

    /// <summary>Equipment slot this item occupies, or null if not equippable.</summary>
    public string? EquipmentSlot { get; init; }

    /// <summary>Reference to a loot table for nested drops, or null if none.</summary>
    public string? LootTableRef { get; init; }
}
