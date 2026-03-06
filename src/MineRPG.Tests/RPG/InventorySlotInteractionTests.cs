using FluentAssertions;

using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.Tests.RPG;

public sealed class InventorySlotInteractionTests
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

    // -----------------------------------------------------------------------
    // Left-click tests
    // -----------------------------------------------------------------------

    [Fact]
    public void LeftClick_EmptyCursorEmptySlot_DoesNothing()
    {
        ItemRegistry registry = CreateRegistry(CreateMaterial("stone"));
        Inventory inventory = new Inventory(9, registry);
        CursorItemHolder cursor = new CursorItemHolder();

        InventorySlotInteraction.HandleLeftClick(inventory, 0, cursor, registry);

        cursor.IsEmpty.Should().BeTrue();
        inventory.GetSlot(0).Should().BeNull();
    }

    [Fact]
    public void LeftClick_EmptyCursorFilledSlot_PicksUpItem()
    {
        ItemRegistry registry = CreateRegistry(CreateMaterial("stone"));
        Inventory inventory = new Inventory(9, registry);
        inventory.TryAdd(new ItemInstance("stone", 10));
        CursorItemHolder cursor = new CursorItemHolder();

        InventorySlotInteraction.HandleLeftClick(inventory, 0, cursor, registry);

        cursor.IsEmpty.Should().BeFalse();
        cursor.HeldItem!.DefinitionId.Should().Be("stone");
        cursor.HeldItem.Count.Should().Be(10);
        inventory.GetSlot(0).Should().BeNull();
    }

    [Fact]
    public void LeftClick_FilledCursorEmptySlot_PlacesItem()
    {
        ItemRegistry registry = CreateRegistry(CreateMaterial("stone"));
        Inventory inventory = new Inventory(9, registry);
        CursorItemHolder cursor = new CursorItemHolder();
        cursor.SetItem(new ItemInstance("stone", 5));

        InventorySlotInteraction.HandleLeftClick(inventory, 0, cursor, registry);

        cursor.IsEmpty.Should().BeTrue();
        inventory.GetSlot(0).Should().NotBeNull();
        inventory.GetSlot(0)!.Count.Should().Be(5);
    }

    [Fact]
    public void LeftClick_CompatibleItems_MergesStacks()
    {
        ItemRegistry registry = CreateRegistry(CreateMaterial("stone"));
        Inventory inventory = new Inventory(9, registry);
        inventory.AddItemAt(0, new ItemInstance("stone", 30));
        CursorItemHolder cursor = new CursorItemHolder();
        cursor.SetItem(new ItemInstance("stone", 20));

        InventorySlotInteraction.HandleLeftClick(inventory, 0, cursor, registry);

        inventory.GetSlot(0)!.Count.Should().Be(50);
        cursor.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void LeftClick_CompatibleItemsOverflow_LeavesRemainderOnCursor()
    {
        ItemRegistry registry = CreateRegistry(CreateMaterial("stone"));
        Inventory inventory = new Inventory(9, registry);
        inventory.AddItemAt(0, new ItemInstance("stone", 50));
        CursorItemHolder cursor = new CursorItemHolder();
        cursor.SetItem(new ItemInstance("stone", 20));

        InventorySlotInteraction.HandleLeftClick(inventory, 0, cursor, registry);

        inventory.GetSlot(0)!.Count.Should().Be(64);
        cursor.HeldItem!.Count.Should().Be(6);
    }

    [Fact]
    public void LeftClick_IncompatibleItems_Swaps()
    {
        ItemDefinition stoneDef = CreateMaterial("stone");
        ItemDefinition dirtDef = CreateMaterial("dirt");
        ItemRegistry registry = CreateRegistry(stoneDef, dirtDef);
        Inventory inventory = new Inventory(9, registry);
        inventory.AddItemAt(0, new ItemInstance("stone", 10));
        CursorItemHolder cursor = new CursorItemHolder();
        cursor.SetItem(new ItemInstance("dirt", 5));

        InventorySlotInteraction.HandleLeftClick(inventory, 0, cursor, registry);

        inventory.GetSlot(0)!.DefinitionId.Should().Be("dirt");
        inventory.GetSlot(0)!.Count.Should().Be(5);
        cursor.HeldItem!.DefinitionId.Should().Be("stone");
        cursor.HeldItem.Count.Should().Be(10);
    }

    // -----------------------------------------------------------------------
    // Right-click tests
    // -----------------------------------------------------------------------

    [Fact]
    public void RightClick_EmptyCursorFilledSlot_PicksUpHalf()
    {
        ItemRegistry registry = CreateRegistry(CreateMaterial("stone"));
        Inventory inventory = new Inventory(9, registry);
        inventory.AddItemAt(0, new ItemInstance("stone", 10));
        CursorItemHolder cursor = new CursorItemHolder();

        InventorySlotInteraction.HandleRightClick(inventory, 0, cursor, registry);

        cursor.HeldItem!.Count.Should().Be(5);
        inventory.GetSlot(0)!.Count.Should().Be(5);
    }

    [Fact]
    public void RightClick_EmptyCursorFilledSlotOddCount_RoundsUpPickUp()
    {
        ItemRegistry registry = CreateRegistry(CreateMaterial("stone"));
        Inventory inventory = new Inventory(9, registry);
        inventory.AddItemAt(0, new ItemInstance("stone", 7));
        CursorItemHolder cursor = new CursorItemHolder();

        InventorySlotInteraction.HandleRightClick(inventory, 0, cursor, registry);

        cursor.HeldItem!.Count.Should().Be(4);
        inventory.GetSlot(0)!.Count.Should().Be(3);
    }

    [Fact]
    public void RightClick_FilledCursorEmptySlot_PlacesOne()
    {
        ItemRegistry registry = CreateRegistry(CreateMaterial("stone"));
        Inventory inventory = new Inventory(9, registry);
        CursorItemHolder cursor = new CursorItemHolder();
        cursor.SetItem(new ItemInstance("stone", 10));

        InventorySlotInteraction.HandleRightClick(inventory, 0, cursor, registry);

        inventory.GetSlot(0)!.Count.Should().Be(1);
        cursor.HeldItem!.Count.Should().Be(9);
    }

    [Fact]
    public void RightClick_FilledCursorCompatibleSlot_PlacesOneOnTop()
    {
        ItemRegistry registry = CreateRegistry(CreateMaterial("stone"));
        Inventory inventory = new Inventory(9, registry);
        inventory.AddItemAt(0, new ItemInstance("stone", 5));
        CursorItemHolder cursor = new CursorItemHolder();
        cursor.SetItem(new ItemInstance("stone", 10));

        InventorySlotInteraction.HandleRightClick(inventory, 0, cursor, registry);

        inventory.GetSlot(0)!.Count.Should().Be(6);
        cursor.HeldItem!.Count.Should().Be(9);
    }

    [Fact]
    public void RightClick_FilledCursorLastItem_ClearsWhenEmpty()
    {
        ItemRegistry registry = CreateRegistry(CreateMaterial("stone"));
        Inventory inventory = new Inventory(9, registry);
        CursorItemHolder cursor = new CursorItemHolder();
        cursor.SetItem(new ItemInstance("stone", 1));

        InventorySlotInteraction.HandleRightClick(inventory, 0, cursor, registry);

        inventory.GetSlot(0)!.Count.Should().Be(1);
        cursor.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void RightClick_FilledCursorFullSlotCompatible_DoesNothing()
    {
        ItemRegistry registry = CreateRegistry(CreateMaterial("stone"));
        Inventory inventory = new Inventory(9, registry);
        inventory.AddItemAt(0, new ItemInstance("stone", 64));
        CursorItemHolder cursor = new CursorItemHolder();
        cursor.SetItem(new ItemInstance("stone", 10));

        InventorySlotInteraction.HandleRightClick(inventory, 0, cursor, registry);

        inventory.GetSlot(0)!.Count.Should().Be(64);
        cursor.HeldItem!.Count.Should().Be(10);
    }

    // -----------------------------------------------------------------------
    // Shift-click tests
    // -----------------------------------------------------------------------

    [Fact]
    public void ShiftClick_HotbarToMain_MovesItem()
    {
        ItemRegistry registry = CreateRegistry(CreateMaterial("stone"));
        PlayerInventory playerInventory = new PlayerInventory(registry);
        playerInventory.Hotbar.AddItemAt(0, new ItemInstance("stone", 10));

        InventorySlotInteraction.HandleShiftClick(
            playerInventory, playerInventory.Hotbar, 0, registry);

        playerInventory.Hotbar.GetSlot(0).Should().BeNull();
        playerInventory.Main.CountItem("stone").Should().Be(10);
    }

    [Fact]
    public void ShiftClick_MainToHotbar_MovesItem()
    {
        ItemRegistry registry = CreateRegistry(CreateMaterial("stone"));
        PlayerInventory playerInventory = new PlayerInventory(registry);
        playerInventory.Main.AddItemAt(0, new ItemInstance("stone", 10));

        InventorySlotInteraction.HandleShiftClick(
            playerInventory, playerInventory.Main, 0, registry);

        playerInventory.Main.GetSlot(0).Should().BeNull();
        playerInventory.Hotbar.CountItem("stone").Should().Be(10);
    }

    [Fact]
    public void ShiftClick_EmptySlot_DoesNothing()
    {
        ItemRegistry registry = CreateRegistry(CreateMaterial("stone"));
        PlayerInventory playerInventory = new PlayerInventory(registry);

        InventorySlotInteraction.HandleShiftClick(
            playerInventory, playerInventory.Hotbar, 0, registry);

        playerInventory.Hotbar.GetSlot(0).Should().BeNull();
        playerInventory.Main.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void ShiftClick_DestinationFull_ReturnsToSource()
    {
        ItemDefinition stoneDef = CreateMaterial("stone");
        ItemDefinition dirtDef = CreateMaterial("dirt");
        ItemRegistry registry = CreateRegistry(stoneDef, dirtDef);
        PlayerInventory playerInventory = new PlayerInventory(registry);

        // Fill all main slots with dirt
        for (int i = 0; i < PlayerInventory.MainSlotCount; i++)
        {
            playerInventory.Main.AddItemAt(i, new ItemInstance("dirt", 64));
        }

        // Put stone in hotbar
        playerInventory.Hotbar.AddItemAt(0, new ItemInstance("stone", 10));

        InventorySlotInteraction.HandleShiftClick(
            playerInventory, playerInventory.Hotbar, 0, registry);

        // Stone should remain in hotbar since main is full
        playerInventory.Hotbar.GetSlot(0).Should().NotBeNull();
        playerInventory.Hotbar.GetSlot(0)!.DefinitionId.Should().Be("stone");
        playerInventory.Hotbar.GetSlot(0)!.Count.Should().Be(10);
    }
}
