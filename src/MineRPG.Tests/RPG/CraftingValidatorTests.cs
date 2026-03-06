using FluentAssertions;

using MineRPG.RPG.Crafting;
using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.Tests.RPG;

public sealed class CraftingValidatorTests
{
    private static ItemRegistry CreateItemRegistry()
    {
        ItemRegistry registry = new ItemRegistry();

        registry.Register(new ItemDefinition
        {
            Id = "iron_ingot",
            MaxStackSize = 64,
            Category = ItemCategory.Material,
        });

        registry.Register(new ItemDefinition
        {
            Id = "stick",
            MaxStackSize = 64,
            Category = ItemCategory.Material,
        });

        registry.Register(new ItemDefinition
        {
            Id = "iron_pickaxe",
            MaxStackSize = 1,
            Category = ItemCategory.Tool,
            HasDurability = true,
            MaxDurability = 250,
        });

        registry.Freeze();
        return registry;
    }

    private static RecipeDefinition CreatePickaxeRecipe()
    {
        return new RecipeDefinition
        {
            Id = "iron_pickaxe",
            Type = RecipeType.Shaped,
            Category = "tools",
            Ingredients = new[]
            {
                new RecipeIngredient("iron_ingot", 3),
                new RecipeIngredient("stick", 2),
            },
            OutputItemId = "iron_pickaxe",
            OutputQuantity = 1,
        };
    }

    [Fact]
    public void CanCraft_WithSufficientIngredients_ReturnsTrue()
    {
        ItemRegistry itemRegistry = CreateItemRegistry();
        RecipeRegistry recipeRegistry = new RecipeRegistry();
        CraftingValidator validator = new CraftingValidator(recipeRegistry, itemRegistry);

        Inventory inventory = new Inventory(9, itemRegistry);
        inventory.TryAdd(new ItemInstance("iron_ingot", 5));
        inventory.TryAdd(new ItemInstance("stick", 10));

        RecipeDefinition recipe = CreatePickaxeRecipe();

        validator.CanCraft(recipe, inventory, 1).Should().BeTrue();
    }

    [Fact]
    public void CanCraft_WithInsufficientIngredients_ReturnsFalse()
    {
        ItemRegistry itemRegistry = CreateItemRegistry();
        RecipeRegistry recipeRegistry = new RecipeRegistry();
        CraftingValidator validator = new CraftingValidator(recipeRegistry, itemRegistry);

        Inventory inventory = new Inventory(9, itemRegistry);
        inventory.TryAdd(new ItemInstance("iron_ingot", 2));

        RecipeDefinition recipe = CreatePickaxeRecipe();

        validator.CanCraft(recipe, inventory, 1).Should().BeFalse();
    }

    [Fact]
    public void CanCraft_WithInsufficientLevel_ReturnsFalse()
    {
        ItemRegistry itemRegistry = CreateItemRegistry();
        RecipeRegistry recipeRegistry = new RecipeRegistry();
        CraftingValidator validator = new CraftingValidator(recipeRegistry, itemRegistry);

        Inventory inventory = new Inventory(9, itemRegistry);
        inventory.TryAdd(new ItemInstance("iron_ingot", 5));
        inventory.TryAdd(new ItemInstance("stick", 5));

        RecipeDefinition recipe = CreatePickaxeRecipe();
        RecipeDefinition leveledRecipe = new RecipeDefinition
        {
            Id = recipe.Id,
            Ingredients = recipe.Ingredients,
            OutputItemId = recipe.OutputItemId,
            RequiredLevel = 10,
        };

        validator.CanCraft(leveledRecipe, inventory, 5).Should().BeFalse();
    }

    [Fact]
    public void GetFailureReason_WhenCraftable_ReturnsNull()
    {
        ItemRegistry itemRegistry = CreateItemRegistry();
        RecipeRegistry recipeRegistry = new RecipeRegistry();
        CraftingValidator validator = new CraftingValidator(recipeRegistry, itemRegistry);

        Inventory inventory = new Inventory(9, itemRegistry);
        inventory.TryAdd(new ItemInstance("iron_ingot", 5));
        inventory.TryAdd(new ItemInstance("stick", 5));

        RecipeDefinition recipe = CreatePickaxeRecipe();

        validator.GetFailureReason(recipe, inventory, 1).Should().BeNull();
    }

    [Fact]
    public void ExecuteCraft_ConsumesIngredientsAndReturnsResult()
    {
        ItemRegistry itemRegistry = CreateItemRegistry();
        RecipeRegistry recipeRegistry = new RecipeRegistry();
        CraftingValidator validator = new CraftingValidator(recipeRegistry, itemRegistry);

        Inventory inventory = new Inventory(9, itemRegistry);
        inventory.TryAdd(new ItemInstance("iron_ingot", 5));
        inventory.TryAdd(new ItemInstance("stick", 5));

        RecipeDefinition recipe = CreatePickaxeRecipe();
        CraftResult result = validator.ExecuteCraft(recipe, inventory, 1);

        result.Success.Should().BeTrue();
        result.ResultItem.Should().NotBeNull();
        result.ResultItem!.DefinitionId.Should().Be("iron_pickaxe");
        result.ResultItem!.Count.Should().Be(1);
        result.ResultItem!.HasDurability.Should().BeTrue();

        inventory.CountItem("iron_ingot").Should().Be(2);
        inventory.CountItem("stick").Should().Be(3);
    }

    [Fact]
    public void ExecuteCraft_WhenCannotCraft_ReturnsFailed()
    {
        ItemRegistry itemRegistry = CreateItemRegistry();
        RecipeRegistry recipeRegistry = new RecipeRegistry();
        CraftingValidator validator = new CraftingValidator(recipeRegistry, itemRegistry);

        Inventory inventory = new Inventory(9, itemRegistry);

        RecipeDefinition recipe = CreatePickaxeRecipe();
        CraftResult result = validator.ExecuteCraft(recipe, inventory, 1);

        result.Success.Should().BeFalse();
        result.FailReason.Should().NotBeNullOrEmpty();
    }
}
