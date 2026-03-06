using System;

using FluentAssertions;

using MineRPG.RPG.Items;

namespace MineRPG.Tests.RPG;

public sealed class ItemRegistryTests
{
    private static readonly string[] FuelCraftingTags = ["fuel", "crafting"];
    private static readonly string[] FuelWoodTags = ["fuel", "wood"];
    private static readonly string[] BuildingTags = ["building"];

    [Fact]
    public void Register_AddsItem()
    {
        ItemRegistry registry = new ItemRegistry();

        registry.Register(new ItemDefinition { Id = "stone", Category = ItemCategory.Material });

        registry.Count.Should().Be(1);
        registry.Contains("stone").Should().BeTrue();
    }

    [Fact]
    public void Register_DuplicateKey_Throws()
    {
        ItemRegistry registry = new ItemRegistry();

        registry.Register(new ItemDefinition { Id = "stone" });

        Action act = () => registry.Register(new ItemDefinition { Id = "stone" });

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Get_ReturnsRegisteredItem()
    {
        ItemRegistry registry = new ItemRegistry();
        ItemDefinition def = new ItemDefinition { Id = "stone", DisplayName = "Stone" };

        registry.Register(def);

        registry.Get("stone").Should().BeSameAs(def);
    }

    [Fact]
    public void Get_NotFound_Throws()
    {
        ItemRegistry registry = new ItemRegistry();

        Action act = () => registry.Get("nonexistent");

        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void TryGet_WhenFound_ReturnsTrueAndValue()
    {
        ItemRegistry registry = new ItemRegistry();
        registry.Register(new ItemDefinition { Id = "stone" });

        bool found = registry.TryGet("stone", out ItemDefinition def);

        found.Should().BeTrue();
        def.Id.Should().Be("stone");
    }

    [Fact]
    public void TryGet_WhenNotFound_ReturnsFalse()
    {
        ItemRegistry registry = new ItemRegistry();

        bool found = registry.TryGet("missing", out _);

        found.Should().BeFalse();
    }

    [Fact]
    public void GetByCategory_ReturnsMatchingItems()
    {
        ItemRegistry registry = new ItemRegistry();

        registry.Register(new ItemDefinition { Id = "stone", Category = ItemCategory.Material });
        registry.Register(new ItemDefinition { Id = "sword", Category = ItemCategory.Weapon });
        registry.Register(new ItemDefinition { Id = "iron", Category = ItemCategory.Material });

        IReadOnlyList<ItemDefinition> materials = registry.GetByCategory(ItemCategory.Material);

        materials.Should().HaveCount(2);
    }

    [Fact]
    public void GetByTag_ReturnsMatchingItems()
    {
        ItemRegistry registry = new ItemRegistry();

        registry.Register(new ItemDefinition
        {
            Id = "coal",
            Tags = FuelCraftingTags,
        });

        registry.Register(new ItemDefinition
        {
            Id = "stick",
            Tags = FuelWoodTags,
        });

        registry.Register(new ItemDefinition
        {
            Id = "stone",
            Tags = BuildingTags,
        });

        IReadOnlyList<ItemDefinition> fuelItems = registry.GetByTag("fuel");

        fuelItems.Should().HaveCount(2);
    }

    [Fact]
    public void Freeze_PreventsFurtherRegistration()
    {
        ItemRegistry registry = new ItemRegistry();
        registry.Register(new ItemDefinition { Id = "stone" });
        registry.Freeze();

        Action act = () => registry.Register(new ItemDefinition { Id = "dirt" });

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetAll_ReturnsAllItems()
    {
        ItemRegistry registry = new ItemRegistry();

        registry.Register(new ItemDefinition { Id = "stone" });
        registry.Register(new ItemDefinition { Id = "dirt" });

        registry.GetAll().Should().HaveCount(2);
    }
}
