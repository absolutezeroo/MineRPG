namespace MineRPG.RPG.Crafting;

/// <summary>
/// Data-driven recipe definition loaded from Data/Recipes/*.json.
/// </summary>
public sealed class RecipeDefinition
{
    public string Id { get; init; } = "";
    public string Category { get; init; } = "";
    public IReadOnlyList<RecipeIngredient> Ingredients { get; init; } = [];
    public int OutputItemId { get; init; }
    public int OutputQuantity { get; init; } = 1;
    public string? RequiredStation { get; init; }
    public int RequiredLevel { get; init; }
    public float CraftingTime { get; init; }
}
