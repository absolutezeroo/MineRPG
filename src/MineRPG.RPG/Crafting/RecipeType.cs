namespace MineRPG.RPG.Crafting;

/// <summary>
/// Types of crafting recipes determining how ingredients are matched.
/// </summary>
public enum RecipeType
{
    /// <summary>Requires exact placement in a crafting grid pattern.</summary>
    Shaped,

    /// <summary>Requires only the correct ingredients, regardless of placement.</summary>
    Shapeless,

    /// <summary>Requires a furnace or smelter to process.</summary>
    Smelting,

    /// <summary>Requires a smithing table to upgrade or transform items.</summary>
    Smithing,

    /// <summary>Requires a brewing stand for potion creation.</summary>
    Brewing,
}
