namespace MineRPG.RPG.Crafting;

/// <summary>
/// Data-driven recipe definition loaded from Data/Recipes/*.json.
/// Supports shaped, shapeless, and smelting recipe types.
/// </summary>
public sealed class RecipeDefinition
{
    /// <summary>Unique identifier for this recipe.</summary>
    public string Id { get; init; } = "";

    /// <summary>The type of recipe determining matching behavior.</summary>
    public RecipeType Type { get; init; }

    /// <summary>Crafting category such as tools, building, or smelting.</summary>
    public string Category { get; init; } = "";

    /// <summary>List of ingredients required to craft this recipe.</summary>
    public IReadOnlyList<RecipeIngredient> Ingredients { get; init; } = [];

    /// <summary>
    /// Pattern rows for shaped recipes (e.g., "III", " S ", " S ").
    /// Null for shapeless and smelting recipes.
    /// </summary>
    public IReadOnlyList<string>? Pattern { get; init; }

    /// <summary>
    /// Character-to-item mapping for shaped recipe patterns.
    /// Null for shapeless and smelting recipes.
    /// </summary>
    public IReadOnlyDictionary<char, string>? PatternKey { get; init; }

    /// <summary>Item definition identifier of the crafted output.</summary>
    public string OutputItemId { get; init; } = "";

    /// <summary>Number of output items produced per craft.</summary>
    public int OutputQuantity { get; init; } = 1;

    /// <summary>Optional crafting station required, or null if hand-craftable.</summary>
    public string? RequiredStation { get; init; }

    /// <summary>Minimum player level required to craft this recipe.</summary>
    public int RequiredLevel { get; init; }

    /// <summary>Time in seconds required to complete the craft.</summary>
    public float CraftingTime { get; init; }

    /// <summary>
    /// Smelting time in seconds. Only used for smelting recipes.
    /// </summary>
    public float SmeltTime { get; init; }

    /// <summary>
    /// Experience reward for completing this recipe.
    /// </summary>
    public float ExperienceReward { get; init; }

    /// <summary>
    /// Optional unlock condition that must be met before the recipe is available.
    /// Null means always available.
    /// </summary>
    public string? UnlockCondition { get; init; }
}
