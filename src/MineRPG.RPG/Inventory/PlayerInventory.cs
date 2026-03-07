using MineRPG.RPG.Items;

namespace MineRPG.RPG.Inventory;

/// <summary>
/// Player-specific inventory containing main storage, hotbar, armor slots, and offhand.
/// Items are added to the hotbar first, then to the main inventory.
/// </summary>
public sealed class PlayerInventory
{
    /// <summary>Number of main inventory slots (3 rows of 9).</summary>
    public const int MainSlotCount = 27;

    /// <summary>Number of hotbar slots.</summary>
    public const int HotbarSlotCount = 9;

    /// <summary>Number of armor slots.</summary>
    public const int ArmorSlotCount = 4;

    private readonly ItemRegistry _itemRegistry;

    /// <summary>
    /// Creates a new player inventory with default slot configuration.
    /// </summary>
    /// <param name="itemRegistry">The item registry for definition lookups.</param>
    public PlayerInventory(ItemRegistry itemRegistry)
    {
        _itemRegistry = itemRegistry ?? throw new ArgumentNullException(nameof(itemRegistry));

        Main = new Inventory(MainSlotCount, itemRegistry);
        Hotbar = new Inventory(HotbarSlotCount, itemRegistry);

        List<SlotFilter> armorFilters = new()
        {
            new SlotFilter { RequiredArmorSlot = ArmorSlotType.Helmet },
            new SlotFilter { RequiredArmorSlot = ArmorSlotType.Chestplate },
            new SlotFilter { RequiredArmorSlot = ArmorSlotType.Leggings },
            new SlotFilter { RequiredArmorSlot = ArmorSlotType.Boots },
        };

        Armor = new Inventory(armorFilters, itemRegistry);
        Offhand = new Inventory(1, itemRegistry);
    }

    /// <summary>The 27-slot main inventory.</summary>
    public Inventory Main { get; }

    /// <summary>The 9-slot hotbar.</summary>
    public Inventory Hotbar { get; }

    /// <summary>The 4-slot armor inventory with slot-type filters.</summary>
    public Inventory Armor { get; }

    /// <summary>The single-slot offhand inventory for shields, torches, etc.</summary>
    public Inventory Offhand { get; }

    /// <summary>Currently selected hotbar slot index (0-8).</summary>
    public int SelectedHotbarIndex { get; set; }

    /// <summary>The item in the currently selected hotbar slot, or null.</summary>
    public ItemInstance? SelectedItem => Hotbar.GetSlot(SelectedHotbarIndex);

    /// <summary>
    /// Adds an item to the player's inventory, prioritizing hotbar then main.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns>Surplus quantity that could not be stored.</returns>
    public int AddItem(ItemInstance item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        int remaining = Hotbar.TryAdd(item);

        if (remaining <= 0)
        {
            return 0;
        }

        item.Count = remaining;
        return Main.TryAdd(item);
    }

    /// <summary>
    /// Removes items from the player's inventory, searching all sections.
    /// </summary>
    /// <param name="definitionId">The item definition ID to remove.</param>
    /// <param name="count">Number of items to remove.</param>
    /// <returns>Actual number of items removed.</returns>
    public int RemoveItem(string definitionId, int count)
    {
        int remaining = count;

        int removedFromMain = Main.Remove(definitionId, remaining);
        remaining -= removedFromMain;

        if (remaining <= 0)
        {
            return count;
        }

        int removedFromHotbar = Hotbar.Remove(definitionId, remaining);
        remaining -= removedFromHotbar;

        return count - remaining;
    }

    /// <summary>
    /// Checks whether the player has at least the specified quantity of an item.
    /// </summary>
    /// <param name="definitionId">The item definition ID to search for.</param>
    /// <param name="count">Required quantity.</param>
    /// <returns>True if the player has enough.</returns>
    public bool HasItem(string definitionId, int count) => CountItem(definitionId) >= count;

    /// <summary>
    /// Counts the total quantity of an item across all inventory sections.
    /// </summary>
    /// <param name="definitionId">The item definition ID to count.</param>
    /// <returns>Total count across hotbar and main inventory.</returns>
    public int CountItem(string definitionId) => Main.CountItem(definitionId) + Hotbar.CountItem(definitionId);

    /// <summary>
    /// Clears all inventory sections and returns all non-null items.
    /// Used when the player dies to drop all items.
    /// </summary>
    /// <returns>A list of all items that were in the inventory.</returns>
    public IReadOnlyList<ItemInstance> ClearAll()
    {
        List<ItemInstance> allItems = new();

        IReadOnlyList<ItemInstance> hotbarItems = Hotbar.ClearAndReturn();
        IReadOnlyList<ItemInstance> mainItems = Main.ClearAndReturn();
        IReadOnlyList<ItemInstance> armorItems = Armor.ClearAndReturn();
        IReadOnlyList<ItemInstance> offhandItems = Offhand.ClearAndReturn();

        for (int i = 0; i < hotbarItems.Count; i++)
        {
            allItems.Add(hotbarItems[i]);
        }

        for (int i = 0; i < mainItems.Count; i++)
        {
            allItems.Add(mainItems[i]);
        }

        for (int i = 0; i < armorItems.Count; i++)
        {
            allItems.Add(armorItems[i]);
        }

        for (int i = 0; i < offhandItems.Count; i++)
        {
            allItems.Add(offhandItems[i]);
        }

        return allItems;
    }

    /// <summary>
    /// Calculates total defense from all equipped armor.
    /// </summary>
    /// <returns>Combined defense value.</returns>
    public float GetTotalDefense()
    {
        float total = 0f;

        for (int i = 0; i < ArmorSlotCount; i++)
        {
            ItemInstance? armorItem = Armor.GetSlot(i);

            if (armorItem == null)
            {
                continue;
            }

            if (_itemRegistry.TryGet(armorItem.DefinitionId, out ItemDefinition definition)
                && definition.Armor != null)
            {
                total += definition.Armor.Defense;
            }
        }

        return total;
    }

    /// <summary>
    /// Calculates total toughness from all equipped armor.
    /// </summary>
    /// <returns>Combined toughness value.</returns>
    public float GetTotalToughness()
    {
        float total = 0f;

        for (int i = 0; i < ArmorSlotCount; i++)
        {
            ItemInstance? armorItem = Armor.GetSlot(i);

            if (armorItem == null)
            {
                continue;
            }

            if (_itemRegistry.TryGet(armorItem.DefinitionId, out ItemDefinition definition)
                && definition.Armor != null)
            {
                total += definition.Armor.Toughness;
            }
        }

        return total;
    }

    /// <summary>
    /// Calculates total weight from all equipped armor.
    /// </summary>
    /// <returns>Combined weight value.</returns>
    public float GetTotalWeight()
    {
        float total = 0f;

        for (int i = 0; i < ArmorSlotCount; i++)
        {
            ItemInstance? armorItem = Armor.GetSlot(i);

            if (armorItem == null)
            {
                continue;
            }

            if (_itemRegistry.TryGet(armorItem.DefinitionId, out ItemDefinition definition)
                && definition.Armor != null)
            {
                total += definition.Armor.Weight;
            }
        }

        return total;
    }
}
