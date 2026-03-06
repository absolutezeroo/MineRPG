using MineRPG.RPG.Items;

namespace MineRPG.RPG.Inventory;

/// <summary>
/// Pure C# logic for Minecraft-style inventory slot click interactions.
/// All methods are static and fully testable without Godot.
/// </summary>
public static class InventorySlotInteraction
{
    /// <summary>
    /// Handles a left-click on a slot. Implements pick-up, place, swap, and merge logic.
    /// </summary>
    /// <param name="inventory">The inventory containing the clicked slot.</param>
    /// <param name="slotIndex">The index of the clicked slot.</param>
    /// <param name="cursor">The cursor item holder.</param>
    /// <param name="itemRegistry">The item registry for definition lookups.</param>
    public static void HandleLeftClick(
        Inventory inventory,
        int slotIndex,
        CursorItemHolder cursor,
        ItemRegistry itemRegistry)
    {
        ItemInstance? slotItem = inventory.GetSlot(slotIndex);
        ItemInstance? cursorItem = cursor.HeldItem;

        // Empty cursor + empty slot → nothing
        if (cursorItem == null && slotItem == null)
        {
            return;
        }

        // Empty cursor + filled slot → pick up
        if (cursorItem == null)
        {
            ItemInstance? taken = inventory.RemoveAt(slotIndex, slotItem!.Count);
            cursor.SetItem(taken);
            return;
        }

        // Filled cursor + empty slot → place
        if (slotItem == null)
        {
            if (!CanAcceptItem(inventory, slotIndex, cursorItem, itemRegistry))
            {
                return;
            }

            ItemInstance? surplus = inventory.AddItemAt(slotIndex, cursorItem);
            cursor.SetItem(surplus);
            return;
        }

        // Both filled + compatible → merge
        if (cursorItem.CanStackWith(slotItem))
        {
            if (!itemRegistry.TryGet(cursorItem.DefinitionId, out ItemDefinition definition))
            {
                return;
            }

            int maxStack = definition.MaxStackSize;

            if (slotItem.Count >= maxStack)
            {
                return;
            }

            int spaceAvailable = maxStack - slotItem.Count;
            int toTransfer = Math.Min(spaceAvailable, cursorItem.Count);
            slotItem.Count += toTransfer;
            cursorItem.Count -= toTransfer;

            inventory.NotifySlotChanged(slotIndex);

            if (cursorItem.Count <= 0)
            {
                cursor.Clear();
            }
            else
            {
                cursor.NotifyChanged();
            }

            return;
        }

        // Both filled + incompatible → swap
        if (!CanAcceptItem(inventory, slotIndex, cursorItem, itemRegistry))
        {
            return;
        }

        ItemInstance? previousSlotItem = inventory.RemoveAt(slotIndex, slotItem.Count);
        inventory.AddItemAt(slotIndex, cursorItem);
        cursor.SetItem(previousSlotItem);
    }

    /// <summary>
    /// Handles a right-click on a slot. Implements place-one and split-half logic.
    /// </summary>
    /// <param name="inventory">The inventory containing the clicked slot.</param>
    /// <param name="slotIndex">The index of the clicked slot.</param>
    /// <param name="cursor">The cursor item holder.</param>
    /// <param name="itemRegistry">The item registry for definition lookups.</param>
    public static void HandleRightClick(
        Inventory inventory,
        int slotIndex,
        CursorItemHolder cursor,
        ItemRegistry itemRegistry)
    {
        ItemInstance? slotItem = inventory.GetSlot(slotIndex);
        ItemInstance? cursorItem = cursor.HeldItem;

        // Empty cursor + empty slot → nothing
        if (cursorItem == null && slotItem == null)
        {
            return;
        }

        // Empty cursor + filled slot → pick up half
        if (cursorItem == null)
        {
            int halfCount = (slotItem!.Count + 1) / 2;
            ItemInstance? taken = inventory.RemoveAt(slotIndex, halfCount);
            cursor.SetItem(taken);
            return;
        }

        // Filled cursor + empty slot → place one
        if (slotItem == null)
        {
            if (!CanAcceptItem(inventory, slotIndex, cursorItem, itemRegistry))
            {
                return;
            }

            if (cursorItem.Count <= 1)
            {
                inventory.AddItemAt(slotIndex, cursorItem);
                cursor.Clear();
            }
            else
            {
                ItemInstance singleItem = cursorItem.Split(1);
                inventory.AddItemAt(slotIndex, singleItem);
                cursor.NotifyChanged();
            }

            return;
        }

        // Filled cursor + filled slot with same item → place one on top
        if (cursorItem.CanStackWith(slotItem))
        {
            if (!itemRegistry.TryGet(cursorItem.DefinitionId, out ItemDefinition definition))
            {
                return;
            }

            if (slotItem.Count >= definition.MaxStackSize)
            {
                return;
            }

            slotItem.Count += 1;
            cursorItem.Count -= 1;

            inventory.NotifySlotChanged(slotIndex);

            if (cursorItem.Count <= 0)
            {
                cursor.Clear();
            }
            else
            {
                cursor.NotifyChanged();
            }

            return;
        }

        // Filled cursor + filled slot with different item → swap (same as left-click)
        HandleLeftClick(inventory, slotIndex, cursor, itemRegistry);
    }

    /// <summary>
    /// Handles a shift-click on a slot. Quick-moves items between hotbar and main inventory.
    /// </summary>
    /// <param name="playerInventory">The player's full inventory structure.</param>
    /// <param name="sourceInventory">The inventory containing the clicked slot.</param>
    /// <param name="slotIndex">The index of the clicked slot.</param>
    /// <param name="itemRegistry">The item registry for definition lookups.</param>
    public static void HandleShiftClick(
        PlayerInventory playerInventory,
        Inventory sourceInventory,
        int slotIndex,
        ItemRegistry itemRegistry)
    {
        ItemInstance? slotItem = sourceInventory.GetSlot(slotIndex);

        if (slotItem == null)
        {
            return;
        }

        // Determine destination: if source is hotbar → main, if source is main → hotbar
        Inventory destination;

        if (sourceInventory == playerInventory.Hotbar)
        {
            destination = playerInventory.Main;
        }
        else if (sourceInventory == playerInventory.Main)
        {
            destination = playerInventory.Hotbar;
        }
        else if (sourceInventory == playerInventory.Armor || sourceInventory == playerInventory.Offhand)
        {
            // Armor/offhand → hotbar first, then main
            destination = playerInventory.Hotbar;
        }
        else
        {
            return;
        }

        // Remove from source
        ItemInstance? taken = sourceInventory.RemoveAt(slotIndex, slotItem.Count);

        if (taken == null)
        {
            return;
        }

        // Try to add to destination
        int remaining = destination.TryAdd(taken);

        if (remaining <= 0)
        {
            return;
        }

        // If hotbar was full and source was armor/offhand, try main
        if (destination == playerInventory.Hotbar
            && (sourceInventory == playerInventory.Armor || sourceInventory == playerInventory.Offhand))
        {
            taken.Count = remaining;
            remaining = playerInventory.Main.TryAdd(taken);
        }

        // If there's still overflow, put it back in the source
        if (remaining > 0)
        {
            taken.Count = remaining;
            sourceInventory.AddItemAt(slotIndex, taken);
        }
    }

    private static bool CanAcceptItem(
        Inventory inventory,
        int slotIndex,
        ItemInstance item,
        ItemRegistry itemRegistry)
    {
        if (!itemRegistry.TryGet(item.DefinitionId, out ItemDefinition definition))
        {
            return false;
        }

        return inventory.Slots[slotIndex].CanAccept(item, definition);
    }
}
