using System;

namespace MineRPG.Godot.UI.Inventory;

/// <summary>
/// Event arguments for when the mouse enters or exits an inventory slot.
/// </summary>
public sealed class SlotHoverEventArgs : EventArgs
{
    /// <summary>
    /// Creates event args for a slot hover event.
    /// </summary>
    /// <param name="slot">The slot node being hovered.</param>
    public SlotHoverEventArgs(InventorySlotNode slot)
    {
        Slot = slot;
    }

    /// <summary>The slot node being hovered.</summary>
    public InventorySlotNode Slot { get; }
}
