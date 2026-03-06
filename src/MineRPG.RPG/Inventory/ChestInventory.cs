using MineRPG.RPG.Items;

namespace MineRPG.RPG.Inventory;

/// <summary>
/// A chest inventory with configurable size and no slot restrictions.
/// </summary>
public sealed class ChestInventory
{
    /// <summary>
    /// Creates a chest inventory with the specified size.
    /// </summary>
    /// <param name="size">Number of slots (27 for single chest, 54 for double).</param>
    /// <param name="itemRegistry">The item registry for definition lookups.</param>
    public ChestInventory(ItemRegistry itemRegistry, int size = 27)
    {
        Storage = new Inventory(size, itemRegistry);
    }

    /// <summary>The chest's storage inventory.</summary>
    public Inventory Storage { get; }
}
