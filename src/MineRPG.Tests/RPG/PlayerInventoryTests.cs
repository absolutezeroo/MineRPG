using FluentAssertions;

using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.Tests.RPG;

public sealed class PlayerInventoryTests
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

    [Fact]
    public void AddItem_GoesToHotbarFirst()
    {
        ItemDefinition stoneDef = new ItemDefinition
        {
            Id = "stone",
            MaxStackSize = 64,
            Category = ItemCategory.Material,
        };

        ItemRegistry registry = CreateRegistry(stoneDef);
        PlayerInventory playerInv = new PlayerInventory(registry);

        int remaining = playerInv.AddItem(new ItemInstance("stone", 10));

        remaining.Should().Be(0);
        playerInv.Hotbar.GetSlot(0).Should().NotBeNull();
        playerInv.Hotbar.GetSlot(0)!.DefinitionId.Should().Be("stone");
    }

    [Fact]
    public void AddItem_OverflowsToMain()
    {
        ItemDefinition stoneDef = new ItemDefinition
        {
            Id = "stone",
            MaxStackSize = 64,
            Category = ItemCategory.Material,
        };

        ItemRegistry registry = CreateRegistry(stoneDef);
        PlayerInventory playerInv = new PlayerInventory(registry);

        // Fill hotbar
        for (int i = 0; i < PlayerInventory.HotbarSlotCount; i++)
        {
            playerInv.AddItem(new ItemInstance("stone", 64));
        }

        int remaining = playerInv.AddItem(new ItemInstance("stone", 10));

        remaining.Should().Be(0);
        playerInv.Main.GetSlot(0).Should().NotBeNull();
    }

    [Fact]
    public void HasItem_SearchesAllSections()
    {
        ItemDefinition stoneDef = new ItemDefinition
        {
            Id = "stone",
            MaxStackSize = 64,
            Category = ItemCategory.Material,
        };

        ItemRegistry registry = CreateRegistry(stoneDef);
        PlayerInventory playerInv = new PlayerInventory(registry);

        playerInv.AddItem(new ItemInstance("stone", 10));

        playerInv.HasItem("stone", 10).Should().BeTrue();
        playerInv.HasItem("stone", 11).Should().BeFalse();
    }

    [Fact]
    public void CountItem_CountsAcrossSections()
    {
        ItemDefinition stoneDef = new ItemDefinition
        {
            Id = "stone",
            MaxStackSize = 64,
            Category = ItemCategory.Material,
        };

        ItemRegistry registry = CreateRegistry(stoneDef);
        PlayerInventory playerInv = new PlayerInventory(registry);

        playerInv.AddItem(new ItemInstance("stone", 10));

        playerInv.CountItem("stone").Should().Be(10);
    }

    [Fact]
    public void RemoveItem_RemovesFromInventory()
    {
        ItemDefinition stoneDef = new ItemDefinition
        {
            Id = "stone",
            MaxStackSize = 64,
            Category = ItemCategory.Material,
        };

        ItemRegistry registry = CreateRegistry(stoneDef);
        PlayerInventory playerInv = new PlayerInventory(registry);

        playerInv.AddItem(new ItemInstance("stone", 20));
        int removed = playerInv.RemoveItem("stone", 10);

        removed.Should().Be(10);
        playerInv.CountItem("stone").Should().Be(10);
    }

    [Fact]
    public void SelectedItem_ReturnsHotbarItem()
    {
        ItemDefinition pickaxeDef = new ItemDefinition
        {
            Id = "iron_pickaxe",
            MaxStackSize = 1,
            Category = ItemCategory.Tool,
            HasDurability = true,
            MaxDurability = 250,
        };

        ItemRegistry registry = CreateRegistry(pickaxeDef);
        PlayerInventory playerInv = new PlayerInventory(registry);

        playerInv.AddItem(new ItemInstance("iron_pickaxe", 1, 250));
        playerInv.SelectedHotbarIndex = 0;

        playerInv.SelectedItem.Should().NotBeNull();
        playerInv.SelectedItem!.DefinitionId.Should().Be("iron_pickaxe");
    }

    [Fact]
    public void GetTotalDefense_CalculatesFromArmor()
    {
        ItemDefinition helmetDef = new ItemDefinition
        {
            Id = "iron_helmet",
            MaxStackSize = 1,
            Category = ItemCategory.Armor,
            Armor = new ArmorProperties { Slot = ArmorSlotType.Helmet, Defense = 2.0f },
        };

        ItemDefinition chestDef = new ItemDefinition
        {
            Id = "iron_chestplate",
            MaxStackSize = 1,
            Category = ItemCategory.Armor,
            Armor = new ArmorProperties { Slot = ArmorSlotType.Chestplate, Defense = 6.0f },
        };

        ItemRegistry registry = CreateRegistry(helmetDef, chestDef);
        PlayerInventory playerInv = new PlayerInventory(registry);

        playerInv.Armor.AddItemAt(0, new ItemInstance("iron_helmet", 1));
        playerInv.Armor.AddItemAt(1, new ItemInstance("iron_chestplate", 1));

        playerInv.GetTotalDefense().Should().Be(8.0f);
    }
}
