using MineRPG.Core.Registry;
using MineRPG.RPG.Inventory;

namespace MineRPG.RPG.Crafting;

/// <summary>
/// Registry of all crafting recipes, loaded from Data/Recipes/*.json.
/// Provides lookup by category, result, and type.
/// </summary>
public sealed class RecipeRegistry
{
    private readonly Registry<string, RecipeDefinition> _inner = new();
    private readonly Dictionary<string, List<RecipeDefinition>> _byCategory = new();
    private readonly Dictionary<string, List<RecipeDefinition>> _byResult = new();
    private readonly Dictionary<RecipeType, List<RecipeDefinition>> _byType = new();

    /// <summary>Number of registered recipes.</summary>
    public int Count => _inner.Count;

    /// <summary>Whether the registry has been frozen.</summary>
    public bool IsFrozen => _inner.IsFrozen;

    /// <summary>
    /// Registers a recipe definition.
    /// </summary>
    /// <param name="recipe">The recipe to register.</param>
    public void Register(RecipeDefinition recipe)
    {
        if (recipe == null)
        {
            throw new ArgumentNullException(nameof(recipe));
        }

        _inner.Register(recipe.Id, recipe);

        AddToIndex(_byCategory, recipe.Category, recipe);
        AddToIndex(_byResult, recipe.OutputItemId, recipe);
        AddToIndex(_byType, recipe.Type, recipe);
    }

    /// <summary>
    /// Freezes the registry.
    /// </summary>
    public void Freeze()
    {
        _inner.Freeze();
    }

    /// <summary>
    /// Gets a recipe by ID. Throws if not found.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    /// <returns>The recipe definition.</returns>
    public RecipeDefinition Get(string id) => _inner.Get(id);

    /// <summary>
    /// Tries to get a recipe by ID.
    /// </summary>
    /// <param name="id">The recipe ID.</param>
    /// <param name="recipe">The found recipe, or null.</param>
    /// <returns>True if found.</returns>
    public bool TryGet(string id, out RecipeDefinition recipe) => _inner.TryGet(id, out recipe!);

    /// <summary>
    /// Returns all registered recipes.
    /// </summary>
    /// <returns>All recipes.</returns>
    public IReadOnlyList<RecipeDefinition> GetAll() => _inner.GetAll();

    /// <summary>
    /// Returns all recipes in the specified category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>Matching recipes.</returns>
    public IReadOnlyList<RecipeDefinition> GetByCategory(string category)
    {
        if (_byCategory.TryGetValue(category, out List<RecipeDefinition>? list))
        {
            return list;
        }

        return [];
    }

    /// <summary>
    /// Returns all recipes that produce the specified item.
    /// </summary>
    /// <param name="itemId">The output item ID.</param>
    /// <returns>Matching recipes.</returns>
    public IReadOnlyList<RecipeDefinition> GetByResult(string itemId)
    {
        if (_byResult.TryGetValue(itemId, out List<RecipeDefinition>? list))
        {
            return list;
        }

        return [];
    }

    /// <summary>
    /// Returns all recipes of the specified type.
    /// </summary>
    /// <param name="type">The recipe type.</param>
    /// <returns>Matching recipes.</returns>
    public IReadOnlyList<RecipeDefinition> GetByType(RecipeType type)
    {
        if (_byType.TryGetValue(type, out List<RecipeDefinition>? list))
        {
            return list;
        }

        return [];
    }

    /// <summary>
    /// Returns all recipes the player can currently craft with their inventory.
    /// </summary>
    /// <param name="inventory">The player's inventory to check.</param>
    /// <returns>Craftable recipes.</returns>
    public IReadOnlyList<RecipeDefinition> GetCraftableWith(IInventory inventory)
    {
        IReadOnlyList<RecipeDefinition> allRecipes = _inner.GetAll();
        List<RecipeDefinition> craftable = new();

        for (int i = 0; i < allRecipes.Count; i++)
        {
            RecipeDefinition recipe = allRecipes[i];
            bool hasAllIngredients = true;

            for (int j = 0; j < recipe.Ingredients.Count; j++)
            {
                if (!inventory.Contains(recipe.Ingredients[j].ItemId, recipe.Ingredients[j].Quantity))
                {
                    hasAllIngredients = false;
                    break;
                }
            }

            if (hasAllIngredients)
            {
                craftable.Add(recipe);
            }
        }

        return craftable;
    }

    private static void AddToIndex<TKey>(
        Dictionary<TKey, List<RecipeDefinition>> index,
        TKey key,
        RecipeDefinition recipe) where TKey : notnull
    {
        if (!index.TryGetValue(key, out List<RecipeDefinition>? list))
        {
            list = new List<RecipeDefinition>();
            index[key] = list;
        }

        list.Add(recipe);
    }
}
