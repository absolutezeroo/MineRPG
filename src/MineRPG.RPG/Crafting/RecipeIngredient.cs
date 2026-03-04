namespace MineRPG.RPG.Crafting;

/// <summary>
/// A single ingredient requirement in a recipe.
/// </summary>
/// <param name="ItemDefinitionId">The item definition identifier of the required ingredient.</param>
/// <param name="Quantity">The number of items required.</param>
public readonly record struct RecipeIngredient(int ItemDefinitionId, int Quantity);
