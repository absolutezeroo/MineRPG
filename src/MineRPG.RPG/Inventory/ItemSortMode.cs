namespace MineRPG.RPG.Inventory;

/// <summary>
/// Sorting modes for inventory contents.
/// </summary>
public enum ItemSortMode : byte
{
    /// <summary>Sort items alphabetically by display name.</summary>
    ByName,

    /// <summary>Sort items by their category.</summary>
    ByCategory,

    /// <summary>Sort items by rarity tier (common first).</summary>
    ByRarity,

    /// <summary>Sort items alphabetically by definition ID.</summary>
    ById,
}
