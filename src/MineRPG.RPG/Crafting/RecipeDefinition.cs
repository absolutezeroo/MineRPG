namespace MineRPG.RPG.Crafting;

/// <summary>
/// Data-driven recipe definition loaded from Data/Recipes/*.json.
/// </summary>
public sealed class RecipeDefinition
{
    /// <summary>Unique identifier for this recipe.</summary>
    public string Id { get; init; } = "";

    /// <summary>Crafting category such as forge, alchemy, or cooking.</summary>
    public string Category { get; init; } = "";

    /// <summary>List of ingredients required to craft this recipe.</summary>
    public IReadOnlyList<RecipeIngredient> Ingredients { get; init; } = [];

    /// <summary>Item definition identifier of the crafted output.</summary>
    public int OutputItemId { get; init; }

    /// <summary>Number of output items produced per craft.</summary>
    public int OutputQuantity { get; init; } = 1;

    /// <summary>Optional crafting station required, or null if hand-craftable.</summary>
    public string? RequiredStation { get; init; }

    /// <summary>Minimum player level required to craft this recipe.</summary>
    public int RequiredLevel { get; init; }

    /// <summary>Time in seconds required to complete the craft.</summary>
    public float CraftingTime { get; init; }
}
