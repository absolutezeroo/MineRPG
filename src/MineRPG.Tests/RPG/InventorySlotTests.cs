using FluentAssertions;

using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.Tests.RPG;

public sealed class InventorySlotTests
{
    [Fact]
    public void NewSlot_IsEmpty()
    {
        InventorySlot slot = new InventorySlot();

        slot.IsEmpty.Should().BeTrue();
        slot.Item.Should().BeNull();
    }

    [Fact]
    public void Place_InEmptySlot_SetsItem()
    {
        InventorySlot slot = new InventorySlot();
        ItemInstance item = new ItemInstance("stone", 10);

        ItemInstance? surplus = slot.Place(item, 64);

        surplus.Should().BeNull();
        slot.IsEmpty.Should().BeFalse();
        slot.Item!.Count.Should().Be(10);
    }

    [Fact]
    public void Take_RemovesItems()
    {
        InventorySlot slot = new InventorySlot();
        slot.Place(new ItemInstance("stone", 10), 64);

        ItemInstance? taken = slot.Take(5);

        taken.Should().NotBeNull();
        taken!.Count.Should().Be(5);
        slot.Item!.Count.Should().Be(5);
    }

    [Fact]
    public void Take_AllItems_ClearsSlot()
    {
        InventorySlot slot = new InventorySlot();
        slot.Place(new ItemInstance("stone", 10), 64);

        ItemInstance? taken = slot.Take(10);

        taken.Should().NotBeNull();
        taken!.Count.Should().Be(10);
        slot.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void TakeAll_ClearsSlot()
    {
        InventorySlot slot = new InventorySlot();
        slot.Place(new ItemInstance("stone", 10), 64);

        ItemInstance? taken = slot.TakeAll();

        taken.Should().NotBeNull();
        taken!.Count.Should().Be(10);
        slot.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Swap_ExchangesItem()
    {
        InventorySlot slot = new InventorySlot();
        ItemInstance first = new ItemInstance("stone", 10);
        slot.Place(first, 64);

        ItemInstance second = new ItemInstance("dirt", 5);
        ItemInstance? previous = slot.Swap(second);

        previous.Should().BeSameAs(first);
        slot.Item.Should().BeSameAs(second);
    }

    [Fact]
    public void CanAccept_WithAcceptAllFilter_ReturnsTrue()
    {
        InventorySlot slot = new InventorySlot();
        ItemInstance item = new ItemInstance("stone", 1);
        ItemDefinition def = new ItemDefinition { Id = "stone", Category = ItemCategory.Material };

        slot.CanAccept(item, def).Should().BeTrue();
    }

    [Fact]
    public void CanAccept_WithCategoryFilter_RejectsWrongCategory()
    {
        SlotFilter filter = new SlotFilter
        {
            AllowedCategories = new[] { ItemCategory.Armor },
        };

        InventorySlot slot = new InventorySlot(filter);
        ItemInstance item = new ItemInstance("stone", 1);
        ItemDefinition def = new ItemDefinition { Id = "stone", Category = ItemCategory.Material };

        slot.CanAccept(item, def).Should().BeFalse();
    }
}
