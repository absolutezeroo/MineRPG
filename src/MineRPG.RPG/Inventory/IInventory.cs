using MineRPG.RPG.Items;

namespace MineRPG.RPG.Inventory;

/// <summary>
/// Generic inventory contract shared by player, chests, and merchant NPCs.
/// </summary>
public interface IInventory
{
    int SlotCount { get; }

    ItemInstance? GetSlot(int index);

    /// <summary>
    /// Attempts to add the item. Returns the remaining quantity that could not fit.
    /// </summary>
    int TryAdd(ItemInstance item);

    /// <summary>
    /// Removes up to <paramref name="quantity"/> of item with the given definition ID.
    /// Returns the actual quantity removed.
    /// </summary>
    int Remove(int definitionId, int quantity);

    bool Contains(int definitionId, int quantity);

    IReadOnlyList<ItemInstance> GetAll();
}
