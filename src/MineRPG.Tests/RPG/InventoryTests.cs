using System;

using FluentAssertions;

using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.Tests.RPG;

public sealed class InventoryTests
{
    private static ItemRegistry CreateRegistry(params ItemDefinition[] definitions)
    {
        ItemRegistry registry = new ItemRegistry();

        for (int i = 0; i < definitions.Length; i++)
        {
            registry.Register(definitions[i]);
        }

        registry.Freeze();
        return registry;
    }

    private static ItemDefinition CreateMaterial(string id, int maxStack = 64)
    {
        return new ItemDefinition
        {
            Id = id,
            DisplayName = id,
            Category = ItemCategory.Material,
            MaxStackSize = maxStack,
        };
    }

    private static ItemDefinition CreateTool(string id)
    {
        return new ItemDefinition
        {
            Id = id,
            DisplayName = id,
            Category = ItemCategory.Tool,
            MaxStackSize = 1,
            HasDurability = true,
            MaxDurability = 250,
        };
    }

    [Fact]
    public void Constructor_WithValidSize_CreatesSlots()
    {
        ItemRegistry registry = CreateRegistry(CreateMaterial("stone"));
        Inventory inventory = new Inventory(9, registry);

        inventory.SlotCount.Should().Be(9);
    }

    [Fact]
    public void Constructor_WithZeroSize_Throws()
    {
        ItemRegistry registry = CreateRegistry();

        Action act = () => new Inventory(0, registry);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void TryAdd_WithSpace_ReturnsZero()
    {
        ItemDefinition stoneDef = CreateMaterial("stone");
        ItemRegistry registry = CreateRegistry(stoneDef);
        Inventory inventory = new Inventory(9, registry);

        ItemInstance stone = new ItemInstance("stone", 32);
        int remaining = inventory.TryAdd(stone);

        remaining.Should().Be(0);
        inventory.GetSlot(0).Should().NotBeNull();
        inventory.GetSlot(0)!.Count.Should().Be(32);
    }

    [Fact]
    public void TryAdd_WhenFull_ReturnsRemainder()
    {
        ItemDefinition stoneDef = CreateMaterial("stone", 64);
        ItemRegistry registry = CreateRegistry(stoneDef);
        Inventory inventory = new Inventory(1, registry);

        inventory.TryAdd(new ItemInstance("stone", 64));
        int remaining = inventory.TryAdd(new ItemInstance("stone", 10));

        remaining.Should().Be(10);
    }

    [Fact]
    public void TryAdd_MergesExistingStacks()
    {
        ItemDefinition stoneDef = CreateMaterial("stone", 64);
        ItemRegistry registry = CreateRegistry(stoneDef);
        Inventory inventory = new Inventory(2, registry);

        inventory.TryAdd(new ItemInstance("stone", 32));
        int remaining = inventory.TryAdd(new ItemInstance("stone", 16));

        remaining.Should().Be(0);
        inventory.GetSlot(0)!.Count.Should().Be(48);
    }

    [Fact]
    public void Remove_RemovesCorrectQuantity()
    {
        ItemDefinition stoneDef = CreateMaterial("stone");
        ItemRegistry registry = CreateRegistry(stoneDef);
        Inventory inventory = new Inventory(9, registry);

        inventory.TryAdd(new ItemInstance("stone", 20));

        int removed = inventory.Remove("stone", 10);

        removed.Should().Be(10);
        inventory.GetSlot(0)!.Count.Should().Be(10);
    }

    [Fact]
    public void Remove_ClearsSlotWhenEmpty()
    {
        ItemDefinition stoneDef = CreateMaterial("stone");
        ItemRegistry registry = CreateRegistry(stoneDef);
        Inventory inventory = new Inventory(9, registry);

        inventory.TryAdd(new ItemInstance("stone", 5));

        int removed = inventory.Remove("stone", 5);

        removed.Should().Be(5);
        inventory.GetSlot(0).Should().BeNull();
    }

    [Fact]
    public void Contains_WithSufficientItems_ReturnsTrue()
    {
        ItemDefinition stoneDef = CreateMaterial("stone");
        ItemRegistry registry = CreateRegistry(stoneDef);
        Inventory inventory = new Inventory(9, registry);

        inventory.TryAdd(new ItemInstance("stone", 20));

        inventory.Contains("stone", 10).Should().BeTrue();
        inventory.Contains("stone", 20).Should().BeTrue();
        inventory.Contains("stone", 21).Should().BeFalse();
    }

    [Fact]
    public void CountItem_ReturnsTotalAcrossSlots()
    {
        ItemDefinition stoneDef = CreateMaterial("stone", 10);
        ItemRegistry registry = CreateRegistry(stoneDef);
        Inventory inventory = new Inventory(3, registry);

        inventory.TryAdd(new ItemInstance("stone", 10));
        inventory.TryAdd(new ItemInstance("stone", 10));
        inventory.TryAdd(new ItemInstance("stone", 5));

        inventory.CountItem("stone").Should().Be(25);
    }

    [Fact]
    public void FindFirstSlot_ReturnsCorrectIndex()
    {
        ItemDefinition stoneDef = CreateMaterial("stone");
        ItemDefinition dirtDef = CreateMaterial("dirt");
        ItemRegistry registry = CreateRegistry(stoneDef, dirtDef);
        Inventory inventory = new Inventory(9, registry);

        inventory.TryAdd(new ItemInstance("stone", 5));
        inventory.TryAdd(new ItemInstance("dirt", 5));

        inventory.FindFirstSlot("dirt").Should().Be(1);
    }

    [Fact]
    public void FindFirstSlot_WhenNotFound_ReturnsMinusOne()
    {
        ItemDefinition stoneDef = CreateMaterial("stone");
        ItemRegistry registry = CreateRegistry(stoneDef);
        Inventory inventory = new Inventory(9, registry);

        inventory.FindFirstSlot("stone").Should().Be(-1);
    }

    [Fact]
    public void FindFirstEmptySlot_ReturnsCorrectIndex()
    {
        ItemDefinition stoneDef = CreateMaterial("stone");
        ItemRegistry registry = CreateRegistry(stoneDef);
        Inventory inventory = new Inventory(3, registry);

        inventory.TryAdd(new ItemInstance("stone", 5));

        inventory.FindFirstEmptySlot().Should().Be(1);
    }

    [Fact]
    public void IsFull_WhenAllSlotsOccupied_ReturnsTrue()
    {
        ItemDefinition stoneDef = CreateMaterial("stone", 64);
        ItemRegistry registry = CreateRegistry(stoneDef);
        Inventory inventory = new Inventory(2, registry);

        inventory.TryAdd(new ItemInstance("stone", 64));
        inventory.TryAdd(new ItemInstance("stone", 64));

        inventory.IsFull().Should().BeTrue();
    }

    [Fact]
    public void IsFull_WhenSlotsAvailable_ReturnsFalse()
    {
        ItemDefinition stoneDef = CreateMaterial("stone");
        ItemRegistry registry = CreateRegistry(stoneDef);
        Inventory inventory = new Inventory(3, registry);

        inventory.TryAdd(new ItemInstance("stone", 5));

        inventory.IsFull().Should().BeFalse();
    }

    [Fact]
    public void SwapSlots_ExchangesContents()
    {
        ItemDefinition stoneDef = CreateMaterial("stone");
        ItemDefinition dirtDef = CreateMaterial("dirt");
        ItemRegistry registry = CreateRegistry(stoneDef, dirtDef);
        Inventory inventory = new Inventory(3, registry);

        inventory.TryAdd(new ItemInstance("stone", 5));
        inventory.TryAdd(new ItemInstance("dirt", 10));

        inventory.SwapSlots(0, 1);

        inventory.GetSlot(0)!.DefinitionId.Should().Be("dirt");
        inventory.GetSlot(0)!.Count.Should().Be(10);
        inventory.GetSlot(1)!.DefinitionId.Should().Be("stone");
        inventory.GetSlot(1)!.Count.Should().Be(5);
    }

    [Fact]
    public void ClearAndReturn_RemovesAllItems()
    {
        ItemDefinition stoneDef = CreateMaterial("stone");
        ItemRegistry registry = CreateRegistry(stoneDef);
        Inventory inventory = new Inventory(3, registry);

        inventory.TryAdd(new ItemInstance("stone", 10));
        inventory.TryAdd(new ItemInstance("stone", 20));

        IReadOnlyList<ItemInstance> removed = inventory.ClearAndReturn();

        removed.Should().HaveCount(2);
        inventory.GetSlot(0).Should().BeNull();
        inventory.GetSlot(1).Should().BeNull();
    }

    [Fact]
    public void GetAll_ReturnsAllNonNullItems()
    {
        ItemDefinition stoneDef = CreateMaterial("stone");
        ItemDefinition dirtDef = CreateMaterial("dirt");
        ItemRegistry registry = CreateRegistry(stoneDef, dirtDef);
        Inventory inventory = new Inventory(5, registry);

        inventory.TryAdd(new ItemInstance("stone", 5));
        inventory.TryAdd(new ItemInstance("dirt", 3));

        IReadOnlyList<ItemInstance> items = inventory.GetAll();

        items.Should().HaveCount(2);
    }

    [Fact]
    public void SlotChanged_RaisedOnAdd()
    {
        ItemDefinition stoneDef = CreateMaterial("stone");
        ItemRegistry registry = CreateRegistry(stoneDef);
        Inventory inventory = new Inventory(3, registry);

        int eventCount = 0;
        inventory.SlotChanged += (index, oldItem, newItem) => eventCount++;

        inventory.TryAdd(new ItemInstance("stone", 5));

        eventCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void InventoryChanged_RaisedOnModification()
    {
        ItemDefinition stoneDef = CreateMaterial("stone");
        ItemRegistry registry = CreateRegistry(stoneDef);
        Inventory inventory = new Inventory(3, registry);

        int eventCount = 0;
        inventory.InventoryChanged += () => eventCount++;

        inventory.TryAdd(new ItemInstance("stone", 5));

        eventCount.Should().BeGreaterThan(0);
    }
}
