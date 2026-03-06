using MineRPG.RPG.Items;

namespace MineRPG.RPG.Inventory;

/// <summary>
/// Read-only query methods for <see cref="Inventory"/>.
/// Separated into a partial file to keep the main file focused on mutations.
/// </summary>
public sealed partial class Inventory
{
    /// <inheritdoc />
    public bool Contains(string definitionId, int quantity)
    {
        return CountItem(definitionId) >= quantity;
    }

    /// <summary>
    /// Counts the total quantity of items with the given definition ID.
    /// </summary>
    /// <param name="definitionId">The item definition ID to count.</param>
    /// <returns>Total count across all slots.</returns>
    public int CountItem(string definitionId)
    {
        int total = 0;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i].IsEmpty && _slots[i].Item!.DefinitionId == definitionId)
            {
                total += _slots[i].Item!.Count;
            }
        }

        return total;
    }

    /// <summary>
    /// Finds the first slot containing the specified item.
    /// </summary>
    /// <param name="definitionId">The item definition ID to find.</param>
    /// <returns>Slot index, or -1 if not found.</returns>
    public int FindFirstSlot(string definitionId)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i].IsEmpty && _slots[i].Item!.DefinitionId == definitionId)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Finds the first empty slot.
    /// </summary>
    /// <returns>Slot index, or -1 if all slots are occupied.</returns>
    public int FindFirstEmptySlot()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].IsEmpty)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Checks whether all slots are occupied.
    /// </summary>
    /// <returns>True if every slot contains an item.</returns>
    public bool IsFull()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].IsEmpty)
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public IReadOnlyList<ItemInstance> GetAll()
    {
        List<ItemInstance> items = new();

        for (int i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i].IsEmpty)
            {
                items.Add(_slots[i].Item!);
            }
        }

        return items;
    }
}
