namespace MineRPG.RPG.Crafting;

/// <summary>
/// A single ingredient requirement in a recipe.
/// </summary>
public sealed record RecipeIngredient(int ItemDefinitionId, int Quantity);
