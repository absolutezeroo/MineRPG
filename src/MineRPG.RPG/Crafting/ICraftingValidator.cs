using MineRPG.RPG.Inventory;

namespace MineRPG.RPG.Crafting;

/// <summary>
/// Validates whether a recipe can be crafted given the player's current state.
/// </summary>
public interface ICraftingValidator
{
    bool CanCraft(RecipeDefinition recipe, IInventory inventory, int playerLevel);

    /// <summary>
    /// Returns a human-readable reason for failure, or null if craftable.
    /// </summary>
    string? GetFailureReason(RecipeDefinition recipe, IInventory inventory, int playerLevel);
}
