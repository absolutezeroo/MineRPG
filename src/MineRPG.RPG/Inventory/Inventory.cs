using MineRPG.RPG.Items;

namespace MineRPG.RPG.Inventory;

/// <summary>
/// Generic inventory container with a fixed number of slots.
/// Used for player inventories, chests, merchants, and crafting stations.
/// </summary>
public sealed partial class Inventory : IInventory
{
    private readonly InventorySlot[] _slots;
    private readonly ItemRegistry _itemRegistry;

    /// <summary>
    /// Creates an inventory with the specified number of slots, all accepting any item.
    /// </summary>
    /// <param name="size">Number of slots in the inventory.</param>
    /// <param name="itemRegistry">The item registry for definition lookups.</param>
    public Inventory(int size, ItemRegistry itemRegistry)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Inventory size must be positive.");
        }

        _itemRegistry = itemRegistry ?? throw new ArgumentNullException(nameof(itemRegistry));
        _slots = new InventorySlot[size];

        for (int i = 0; i < size; i++)
        {
            _slots[i] = new InventorySlot();
        }
    }

    /// <summary>
    /// Creates an inventory with individually filtered slots.
    /// </summary>
    /// <param name="slotFilters">A filter per slot. The count determines the inventory size.</param>
    /// <param name="itemRegistry">The item registry for definition lookups.</param>
    public Inventory(IReadOnlyList<SlotFilter> slotFilters, ItemRegistry itemRegistry)
    {
        if (slotFilters == null)
        {
            throw new ArgumentNullException(nameof(slotFilters));
        }

        if (slotFilters.Count == 0)
        {
            throw new ArgumentException("Slot filters must not be empty.", nameof(slotFilters));
        }

        _itemRegistry = itemRegistry ?? throw new ArgumentNullException(nameof(itemRegistry));
        _slots = new InventorySlot[slotFilters.Count];

        for (int i = 0; i < slotFilters.Count; i++)
        {
            _slots[i] = new InventorySlot(slotFilters[i]);
        }
    }

    /// <inheritdoc />
    public int SlotCount => _slots.Length;

    /// <summary>Read-only access to all slots.</summary>
    public IReadOnlyList<InventorySlot> Slots => _slots;

    /// <summary>Raised when a slot's contents change.</summary>
    public event EventHandler<SlotChangedEventArgs>? SlotChanged;

    /// <summary>Raised when any change occurs in the inventory.</summary>
    public event EventHandler? InventoryChanged;

    /// <inheritdoc />
    public ItemInstance? GetSlot(int index)
    {
        ValidateIndex(index);
        return _slots[index].Item;
    }

    /// <inheritdoc />
    public int TryAdd(ItemInstance item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (!_itemRegistry.TryGet(item.DefinitionId, out ItemDefinition definition))
        {
            return item.Count;
        }

        int maxStack = definition.MaxStackSize;

        // First pass: merge with existing compatible stacks
        for (int i = 0; i < _slots.Length; i++)
        {
            if (item.Count <= 0)
            {
                break;
            }

            if (_slots[i].IsEmpty)
            {
                continue;
            }

            if (_slots[i].Item!.CanStackWith(item)
                && _slots[i].Item!.Count < maxStack
                && _slots[i].CanAccept(item, definition))
            {
                ItemInstance? oldItem = CloneForEvent(_slots[i].Item);
                _slots[i].Item!.Merge(item, maxStack);
                RaiseSlotChanged(i, oldItem, _slots[i].Item);
            }
        }

        // Second pass: place in empty slots
        for (int i = 0; i < _slots.Length; i++)
        {
            if (item.Count <= 0)
            {
                break;
            }

            if (!_slots[i].IsEmpty)
            {
                continue;
            }

            if (!_slots[i].CanAccept(item, definition))
            {
                continue;
            }

            if (item.Count <= maxStack)
            {
                _slots[i].SetItem(item);
                RaiseSlotChanged(i, null, item);
                RaiseInventoryChanged();
                return 0;
            }

            ItemInstance placed = item.Split(maxStack);
            _slots[i].SetItem(placed);
            RaiseSlotChanged(i, null, placed);
        }

        RaiseInventoryChanged();
        return item.Count;
    }

    /// <summary>
    /// Adds an item at a specific slot index.
    /// </summary>
    /// <param name="slotIndex">The slot index to add to.</param>
    /// <param name="item">The item to add.</param>
    /// <returns>Surplus items that could not fit, or null.</returns>
    public ItemInstance? AddItemAt(int slotIndex, ItemInstance item)
    {
        ValidateIndex(slotIndex);

        if (item == null)
        {
            return null;
        }

        if (!_itemRegistry.TryGet(item.DefinitionId, out ItemDefinition definition))
        {
            return item;
        }

        if (!_slots[slotIndex].CanAccept(item, definition))
        {
            return item;
        }

        ItemInstance? oldItem = _slots[slotIndex].Item;
        ItemInstance? surplus = _slots[slotIndex].Place(item, definition.MaxStackSize);

        RaiseSlotChanged(slotIndex, oldItem, _slots[slotIndex].Item);
        RaiseInventoryChanged();

        return surplus;
    }

    /// <inheritdoc />
    public int Remove(string definitionId, int quantity)
    {
        int remaining = quantity;

        for (int i = _slots.Length - 1; i >= 0; i--)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (_slots[i].IsEmpty || _slots[i].Item!.DefinitionId != definitionId)
            {
                continue;
            }

            ItemInstance? oldItem = _slots[i].Item;
            int toRemove = Math.Min(remaining, _slots[i].Item!.Count);
            _slots[i].Item!.Count -= toRemove;
            remaining -= toRemove;

            if (_slots[i].Item!.Count <= 0)
            {
                _slots[i].SetItem(null);
            }

            RaiseSlotChanged(i, oldItem, _slots[i].Item);
        }

        if (remaining < quantity)
        {
            RaiseInventoryChanged();
        }

        return quantity - remaining;
    }

    /// <summary>
    /// Removes items from a specific slot.
    /// </summary>
    /// <param name="slotIndex">The slot to remove from.</param>
    /// <param name="count">Number of items to remove.</param>
    /// <returns>The removed items, or null if the slot was empty.</returns>
    public ItemInstance? RemoveAt(int slotIndex, int count)
    {
        ValidateIndex(slotIndex);

        ItemInstance? oldItem = _slots[slotIndex].Item;
        ItemInstance? taken = _slots[slotIndex].Take(count);

        if (taken != null)
        {
            RaiseSlotChanged(slotIndex, oldItem, _slots[slotIndex].Item);
            RaiseInventoryChanged();
        }

        return taken;
    }

    /// <summary>
    /// Swaps the contents of two slots.
    /// </summary>
    /// <param name="fromSlot">First slot index.</param>
    /// <param name="toSlot">Second slot index.</param>
    public void SwapSlots(int fromSlot, int toSlot)
    {
        ValidateIndex(fromSlot);
        ValidateIndex(toSlot);

        if (fromSlot == toSlot)
        {
            return;
        }

        ItemInstance? fromItem = _slots[fromSlot].Item;
        ItemInstance? toItem = _slots[toSlot].Item;

        _slots[fromSlot].SetItem(toItem);
        _slots[toSlot].SetItem(fromItem);

        RaiseSlotChanged(fromSlot, fromItem, toItem);
        RaiseSlotChanged(toSlot, toItem, fromItem);
        RaiseInventoryChanged();
    }

    /// <summary>
    /// Removes all items and returns them.
    /// </summary>
    /// <returns>All items that were in the inventory.</returns>
    public IReadOnlyList<ItemInstance> ClearAndReturn()
    {
        List<ItemInstance> items = new();

        for (int i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i].IsEmpty)
            {
                items.Add(_slots[i].Item!);
                ItemInstance? oldItem = _slots[i].Item;
                _slots[i].SetItem(null);
                RaiseSlotChanged(i, oldItem, null);
            }
        }

        if (items.Count > 0)
        {
            RaiseInventoryChanged();
        }

        return items;
    }

    /// <summary>
    /// Removes all items from the inventory.
    /// </summary>
    public void Clear()
    {
        ClearAndReturn();
    }

    private void ValidateIndex(int index)
    {
        if (index < 0 || index >= _slots.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index), index, $"Slot index must be between 0 and {_slots.Length - 1}.");
        }
    }

    private void RaiseSlotChanged(int index, ItemInstance? oldItem, ItemInstance? newItem)
    {
        SlotChanged?.Invoke(this, new SlotChangedEventArgs(index, oldItem, newItem));
    }

    private void RaiseInventoryChanged()
    {
        InventoryChanged?.Invoke(this, EventArgs.Empty);
    }

    private static ItemInstance? CloneForEvent(ItemInstance? item)
    {
        if (item == null)
        {
            return null;
        }

        return new ItemInstance(item.DefinitionId, item.Count, item.CurrentDurability);
    }
}
