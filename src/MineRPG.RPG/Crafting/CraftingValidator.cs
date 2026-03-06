using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.RPG.Crafting;

/// <summary>
/// Validates and executes crafting operations against recipes and inventories.
/// </summary>
public sealed class CraftingValidator : ICraftingValidator
{
    private readonly RecipeRegistry _recipes;
    private readonly ItemRegistry _items;

    /// <summary>
    /// Creates a crafting validator with the required registries.
    /// </summary>
    /// <param name="recipes">The recipe registry.</param>
    /// <param name="items">The item registry.</param>
    public CraftingValidator(RecipeRegistry recipes, ItemRegistry items)
    {
        _recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    /// <inheritdoc />
    public bool CanCraft(RecipeDefinition recipe, IInventory inventory, int playerLevel)
    {
        return GetFailureReason(recipe, inventory, playerLevel) == null;
    }

    /// <inheritdoc />
    public string? GetFailureReason(RecipeDefinition recipe, IInventory inventory, int playerLevel)
    {
        if (recipe == null)
        {
            return "Recipe is null.";
        }

        if (inventory == null)
        {
            return "Inventory is null.";
        }

        if (playerLevel < recipe.RequiredLevel)
        {
            return $"Requires level {recipe.RequiredLevel}, current level is {playerLevel}.";
        }

        for (int i = 0; i < recipe.Ingredients.Count; i++)
        {
            RecipeIngredient ingredient = recipe.Ingredients[i];

            if (!inventory.Contains(ingredient.ItemId, ingredient.Quantity))
            {
                int available = 0;

                IReadOnlyList<ItemInstance> items = inventory.GetAll();
                for (int j = 0; j < items.Count; j++)
                {
                    if (items[j].DefinitionId == ingredient.ItemId)
                    {
                        available += items[j].Count;
                    }
                }

                return $"Missing ingredient: {ingredient.ItemId} (need {ingredient.Quantity}, have {available}).";
            }
        }

        return null;
    }

    /// <summary>
    /// Executes a crafting operation: validates, consumes ingredients, and produces output.
    /// </summary>
    /// <param name="recipe">The recipe to craft.</param>
    /// <param name="inventory">The inventory to consume ingredients from and add output to.</param>
    /// <param name="playerLevel">The player's current level.</param>
    /// <returns>The result of the crafting operation.</returns>
    public CraftResult ExecuteCraft(RecipeDefinition recipe, IInventory inventory, int playerLevel)
    {
        string? failReason = GetFailureReason(recipe, inventory, playerLevel);

        if (failReason != null)
        {
            return new CraftResult
            {
                Success = false,
                FailReason = failReason,
            };
        }

        // Consume ingredients
        for (int i = 0; i < recipe.Ingredients.Count; i++)
        {
            RecipeIngredient ingredient = recipe.Ingredients[i];
            inventory.Remove(ingredient.ItemId, ingredient.Quantity);
        }

        // Create output item
        int durability = -1;

        if (_items.TryGet(recipe.OutputItemId, out ItemDefinition outputDef) && outputDef.HasDurability)
        {
            durability = outputDef.MaxDurability;
        }

        ItemInstance resultItem = new ItemInstance(
            recipe.OutputItemId,
            recipe.OutputQuantity,
            durability);

        return new CraftResult
        {
            Success = true,
            ResultItem = resultItem,
            ExperienceReward = recipe.ExperienceReward,
        };
    }
}
