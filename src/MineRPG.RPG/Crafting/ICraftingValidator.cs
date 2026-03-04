using MineRPG.RPG.Inventory;

namespace MineRPG.RPG.Crafting;

/// <summary>
/// Validates whether a recipe can be crafted given the player's current state.
/// </summary>
public interface ICraftingValidator
{
    /// <summary>
    /// Determines whether the given recipe can be crafted with the available inventory and player level.
    /// </summary>
    /// <param name="recipe">The recipe to validate.</param>
    /// <param name="inventory">The inventory to check for required ingredients.</param>
    /// <param name="playerLevel">The current level of the player attempting to craft.</param>
    /// <returns><c>true</c> if all crafting requirements are met; otherwise, <c>false</c>.</returns>
    public bool CanCraft(RecipeDefinition recipe, IInventory inventory, int playerLevel);

    /// <summary>
    /// Returns a human-readable reason for failure, or null if craftable.
    /// </summary>
    /// <param name="recipe">The recipe to validate.</param>
    /// <param name="inventory">The inventory to check for required ingredients.</param>
    /// <param name="playerLevel">The current level of the player attempting to craft.</param>
    /// <returns>A failure reason string, or <c>null</c> if the recipe can be crafted.</returns>
    public string? GetFailureReason(RecipeDefinition recipe, IInventory inventory, int playerLevel);
}
