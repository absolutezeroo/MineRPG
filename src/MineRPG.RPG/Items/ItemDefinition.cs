namespace MineRPG.RPG.Items;

/// <summary>
/// Immutable definition of an item type, loaded from Data/Items/*.json at startup.
/// Describes the static properties shared by all instances of this item.
/// Registered in an <see cref="ItemRegistry"/> keyed by <see cref="Id"/>.
/// </summary>
public sealed class ItemDefinition
{
    /// <summary>Unique string identifier for this item type.</summary>
    public string Id { get; init; } = "";

    /// <summary>Localized display name shown in the UI.</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>Flavor text describing the item.</summary>
    public string Description { get; init; } = "";

    /// <summary>Primary category determining behavior and UI grouping.</summary>
    public ItemCategory Category { get; init; }

    /// <summary>Rarity tier used for loot generation and display color coding.</summary>
    public ItemRarity Rarity { get; init; }

    /// <summary>Maximum number of items that can be stacked in a single slot.</summary>
    public int MaxStackSize { get; init; } = 64;

    /// <summary>Whether this item type supports stacking.</summary>
    public bool IsStackable => MaxStackSize > 1;

    /// <summary>Whether this item has durability that degrades with use.</summary>
    public bool HasDurability { get; init; }

    /// <summary>Maximum durability points when fully repaired.</summary>
    public int MaxDurability { get; init; }

    /// <summary>Identifier into the icon texture atlas for UI display.</summary>
    public string IconAtlasId { get; init; } = "";

    /// <summary>Optional 3D model identifier for in-hand rendering.</summary>
    public string? ModelId { get; init; }

    /// <summary>Block ID to place when this item is used. Null if not a block item.</summary>
    public string? PlacesBlockId { get; init; }

    /// <summary>Tool-specific properties. Null if this item is not a tool.</summary>
    public ToolProperties? Tool { get; init; }

    /// <summary>Weapon-specific properties. Null if this item is not a weapon.</summary>
    public WeaponProperties? Weapon { get; init; }

    /// <summary>Armor-specific properties. Null if this item is not armor.</summary>
    public ArmorProperties? Armor { get; init; }

    /// <summary>Consumable-specific properties. Null if this item is not consumable.</summary>
    public ConsumableProperties? Consumable { get; init; }

    /// <summary>Flexible tags for custom behaviors and queries.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Sale price at merchants.</summary>
    public int SellValue { get; init; }

    /// <summary>Purchase price at merchants.</summary>
    public int BuyValue { get; init; }
}
