using MineRPG.RPG.Items;

namespace MineRPG.RPG.Inventory;

/// <summary>
/// Generic inventory contract shared by player, chests, and merchant NPCs.
/// </summary>
public interface IInventory
{
    /// <summary>Total number of slots in this inventory.</summary>
    int SlotCount { get; }

    /// <summary>
    /// Returns the item instance at the given slot index, or null if the slot is empty.
    /// </summary>
    /// <param name="index">Zero-based slot index.</param>
    /// <returns>The item in the slot, or <c>null</c> if empty.</returns>
    ItemInstance? GetSlot(int index);

    /// <summary>
    /// Attempts to add the item. Returns the remaining quantity that could not fit.
    /// </summary>
    /// <param name="item">The item instance to add.</param>
    /// <returns>The quantity that could not be added due to insufficient space.</returns>
    int TryAdd(ItemInstance item);

    /// <summary>
    /// Removes up to <paramref name="quantity"/> of item with the given definition ID.
    /// Returns the actual quantity removed.
    /// </summary>
    /// <param name="definitionId">The item definition identifier to remove.</param>
    /// <param name="quantity">The maximum quantity to remove.</param>
    /// <returns>The actual number of items removed.</returns>
    int Remove(string definitionId, int quantity);

    /// <summary>
    /// Checks whether the inventory contains at least the specified quantity of the given item.
    /// </summary>
    /// <param name="definitionId">The item definition identifier to search for.</param>
    /// <param name="quantity">The minimum required quantity.</param>
    /// <returns><c>true</c> if the inventory contains enough of the item; otherwise, <c>false</c>.</returns>
    bool Contains(string definitionId, int quantity);

    /// <summary>
    /// Returns a read-only snapshot of all non-null item instances in the inventory.
    /// </summary>
    /// <returns>A list of all item instances currently stored.</returns>
    IReadOnlyList<ItemInstance> GetAll();
}
