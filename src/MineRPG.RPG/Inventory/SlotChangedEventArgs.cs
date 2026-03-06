using MineRPG.RPG.Items;

namespace MineRPG.RPG.Inventory;

/// <summary>
/// Event data for when an inventory slot's contents change.
/// </summary>
public sealed class SlotChangedEventArgs : EventArgs
{
    /// <summary>The index of the slot that changed.</summary>
    public int SlotIndex { get; }

    /// <summary>The item previously in the slot, or null if it was empty.</summary>
    public ItemInstance? OldItem { get; }

    /// <summary>The item now in the slot, or null if it is now empty.</summary>
    public ItemInstance? NewItem { get; }

    /// <summary>
    /// Creates event data for a slot change.
    /// </summary>
    /// <param name="slotIndex">The index of the slot that changed.</param>
    /// <param name="oldItem">The previous item in the slot.</param>
    /// <param name="newItem">The new item in the slot.</param>
    public SlotChangedEventArgs(int slotIndex, ItemInstance? oldItem, ItemInstance? newItem)
    {
        SlotIndex = slotIndex;
        OldItem = oldItem;
        NewItem = newItem;
    }
}
