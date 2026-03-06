using System;

using Godot;

namespace MineRPG.Godot.UI.Inventory;

/// <summary>
/// Event arguments for when an inventory slot is clicked.
/// </summary>
public sealed class SlotClickedEventArgs : EventArgs
{
    /// <summary>
    /// Creates event args for a slot click.
    /// </summary>
    /// <param name="slot">The slot node that was clicked.</param>
    /// <param name="button">The mouse button used.</param>
    public SlotClickedEventArgs(InventorySlotNode slot, MouseButton button)
    {
        Slot = slot;
        Button = button;
    }

    /// <summary>The slot node that was clicked.</summary>
    public InventorySlotNode Slot { get; }

    /// <summary>The mouse button that was pressed.</summary>
    public MouseButton Button { get; }
}
