using System;

using MineRPG.RPG.Items;

namespace MineRPG.RPG.Inventory;

/// <summary>
/// Holds the item currently "on the cursor" during inventory drag interactions.
/// The cursor item is the item being moved between slots when the player clicks.
/// </summary>
public sealed class CursorItemHolder
{
    /// <summary>The item currently held on the cursor, or null if empty.</summary>
    public ItemInstance? HeldItem { get; private set; }

    /// <summary>Whether the cursor is currently holding an item.</summary>
    public bool IsEmpty => HeldItem == null;

    /// <summary>Raised when the held item changes (set, cleared, or modified).</summary>
    public event EventHandler? HeldItemChanged;

    /// <summary>
    /// Sets the cursor to hold the given item, replacing any previously held item.
    /// </summary>
    /// <param name="item">The item to hold. Can be null to clear.</param>
    public void SetItem(ItemInstance? item)
    {
        HeldItem = item;
        RaiseChanged();
    }

    /// <summary>
    /// Takes the entire held item from the cursor, leaving it empty.
    /// </summary>
    /// <returns>The item that was held, or null if already empty.</returns>
    public ItemInstance? TakeItem()
    {
        ItemInstance? taken = HeldItem;
        HeldItem = null;
        RaiseChanged();
        return taken;
    }

    /// <summary>
    /// Takes half of the held stack (rounded up), leaving the remainder on the cursor.
    /// If the held item has only 1, takes the whole item.
    /// </summary>
    /// <returns>The split-off portion, or null if cursor is empty.</returns>
    public ItemInstance? TakeHalf()
    {
        if (HeldItem == null)
        {
            return null;
        }

        if (HeldItem.Count <= 1)
        {
            return TakeItem();
        }

        int halfCount = (HeldItem.Count + 1) / 2;
        ItemInstance split = HeldItem.Split(halfCount);
        RaiseChanged();
        return split;
    }

    /// <summary>
    /// Notifies listeners that the held item was modified in-place (e.g. count changed).
    /// Call this after mutating <see cref="HeldItem"/> properties directly.
    /// </summary>
    public void NotifyChanged() => RaiseChanged();

    /// <summary>
    /// Clears the cursor, discarding any held item.
    /// </summary>
    public void Clear()
    {
        HeldItem = null;
        RaiseChanged();
    }

    private void RaiseChanged() => HeldItemChanged?.Invoke(this, EventArgs.Empty);
}
