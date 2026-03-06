using MineRPG.RPG.Items;

namespace MineRPG.RPG.Inventory;

/// <summary>
/// A single inventory slot that can hold one <see cref="ItemInstance"/> or be empty.
/// Supports optional filtering via <see cref="SlotFilter"/>.
/// </summary>
public sealed class InventorySlot
{
    /// <summary>
    /// Creates a new inventory slot with the specified filter.
    /// </summary>
    /// <param name="filter">The filter restricting accepted items. Null defaults to AcceptAll.</param>
    public InventorySlot(SlotFilter? filter = null)
    {
        Filter = filter ?? SlotFilter.AcceptAll;
    }

    /// <summary>The item currently stored in this slot, or null if empty.</summary>
    public ItemInstance? Item { get; private set; }

    /// <summary>Whether this slot is empty.</summary>
    public bool IsEmpty => Item == null;

    /// <summary>The filter restricting which items this slot accepts.</summary>
    public SlotFilter Filter { get; }

    /// <summary>
    /// Checks whether the slot can accept the given item based on its filter.
    /// </summary>
    /// <param name="item">The item to check.</param>
    /// <param name="definition">The item's definition for filter validation.</param>
    /// <returns>True if the item can be placed in this slot.</returns>
    public bool CanAccept(ItemInstance item, ItemDefinition definition)
    {
        if (item == null || definition == null)
        {
            return false;
        }

        return Filter.Accepts(definition);
    }

    /// <summary>
    /// Places an item in this slot. If the slot already contains a compatible item,
    /// merges the stacks. Returns any surplus that could not fit.
    /// </summary>
    /// <param name="item">The item to place.</param>
    /// <param name="maxStackSize">Maximum stack size for this item type.</param>
    /// <returns>Surplus items that could not fit, or null if everything was placed.</returns>
    public ItemInstance? Place(ItemInstance item, int maxStackSize)
    {
        if (item == null)
        {
            return null;
        }

        if (IsEmpty)
        {
            if (item.Count <= maxStackSize)
            {
                Item = item;
                return null;
            }

            Item = item.Split(maxStackSize);
            return item;
        }

        if (Item != null && Item.CanStackWith(item))
        {
            int remaining = Item.Merge(item, maxStackSize);

            if (remaining <= 0)
            {
                return null;
            }

            return item;
        }

        return item;
    }

    /// <summary>
    /// Takes up to <paramref name="count"/> items from this slot.
    /// </summary>
    /// <param name="count">Number of items to take.</param>
    /// <returns>The taken items, or null if the slot is empty.</returns>
    public ItemInstance? Take(int count)
    {
        if (IsEmpty || count <= 0)
        {
            return null;
        }

        if (count >= Item!.Count)
        {
            ItemInstance taken = Item;
            Item = null;
            return taken;
        }

        return Item.Split(count);
    }

    /// <summary>
    /// Takes all items from this slot, leaving it empty.
    /// </summary>
    /// <returns>The items that were in the slot, or null if empty.</returns>
    public ItemInstance? TakeAll()
    {
        ItemInstance? taken = Item;
        Item = null;
        return taken;
    }

    /// <summary>
    /// Swaps the current item with the given item.
    /// </summary>
    /// <param name="item">The item to place. Can be null to clear the slot.</param>
    /// <returns>The item that was previously in the slot, or null.</returns>
    public ItemInstance? Swap(ItemInstance? item)
    {
        ItemInstance? previous = Item;
        Item = item;
        return previous;
    }

    /// <summary>
    /// Sets the slot contents directly without any validation.
    /// Used internally for inventory operations.
    /// </summary>
    /// <param name="item">The item to set, or null to clear.</param>
    internal void SetItem(ItemInstance? item)
    {
        Item = item;
    }
}
