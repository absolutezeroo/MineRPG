using MineRPG.RPG.Items;

namespace MineRPG.RPG.Equipment;

/// <summary>
/// Event data for when an equipment slot changes.
/// </summary>
public sealed class EquipmentChangedEventArgs : EventArgs
{
    /// <summary>The armor slot that changed.</summary>
    public ArmorSlotType Slot { get; }

    /// <summary>The previously equipped item, or null if the slot was empty.</summary>
    public ItemInstance? PreviousItem { get; }

    /// <summary>The newly equipped item, or null if the slot was unequipped.</summary>
    public ItemInstance? NewItem { get; }

    /// <summary>
    /// Creates event data for an equipment change.
    /// </summary>
    /// <param name="slot">The armor slot that changed.</param>
    /// <param name="previousItem">The previously equipped item.</param>
    /// <param name="newItem">The newly equipped item.</param>
    public EquipmentChangedEventArgs(ArmorSlotType slot, ItemInstance? previousItem, ItemInstance? newItem)
    {
        Slot = slot;
        PreviousItem = previousItem;
        NewItem = newItem;
    }
}
