namespace MineRPG.RPG.Items;

/// <summary>
/// Categories of items that determine their primary behavior and UI grouping.
/// </summary>
public enum ItemCategory
{
    /// <summary>Placeable block items.</summary>
    Block,

    /// <summary>Tools for mining, chopping, digging.</summary>
    Tool,

    /// <summary>Weapons for combat.</summary>
    Weapon,

    /// <summary>Armor for defense.</summary>
    Armor,

    /// <summary>Consumable items like food and potions.</summary>
    Consumable,

    /// <summary>Raw materials and crafting ingredients.</summary>
    Material,

    /// <summary>Miscellaneous items: quest items, decorations, keys.</summary>
    Misc,
}
