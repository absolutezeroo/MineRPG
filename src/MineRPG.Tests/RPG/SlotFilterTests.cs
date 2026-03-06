using FluentAssertions;

using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.Tests.RPG;

public sealed class SlotFilterTests
{
    [Fact]
    public void AcceptAll_AcceptsAnyItem()
    {
        ItemDefinition def = new ItemDefinition
        {
            Id = "stone",
            Category = ItemCategory.Material,
        };

        SlotFilter.AcceptAll.Accepts(def).Should().BeTrue();
    }

    [Fact]
    public void AllowedCategories_AcceptsMatchingCategory()
    {
        SlotFilter filter = new SlotFilter
        {
            AllowedCategories = new[] { ItemCategory.Tool, ItemCategory.Weapon },
        };

        ItemDefinition tool = new ItemDefinition { Id = "pickaxe", Category = ItemCategory.Tool };
        ItemDefinition material = new ItemDefinition { Id = "stone", Category = ItemCategory.Material };

        filter.Accepts(tool).Should().BeTrue();
        filter.Accepts(material).Should().BeFalse();
    }

    [Fact]
    public void AllowedItemIds_AcceptsMatchingId()
    {
        SlotFilter filter = new SlotFilter
        {
            AllowedItemIds = new[] { "coal", "charcoal" },
        };

        ItemDefinition coal = new ItemDefinition { Id = "coal" };
        ItemDefinition stone = new ItemDefinition { Id = "stone" };

        filter.Accepts(coal).Should().BeTrue();
        filter.Accepts(stone).Should().BeFalse();
    }

    [Fact]
    public void AllowedTags_AcceptsItemWithMatchingTag()
    {
        SlotFilter filter = new SlotFilter
        {
            AllowedTags = new[] { "fuel" },
        };

        ItemDefinition coal = new ItemDefinition { Id = "coal", Tags = new[] { "fuel", "crafting" } };
        ItemDefinition stone = new ItemDefinition { Id = "stone", Tags = new[] { "building" } };

        filter.Accepts(coal).Should().BeTrue();
        filter.Accepts(stone).Should().BeFalse();
    }

    [Fact]
    public void RequiredArmorSlot_AcceptsCorrectSlot()
    {
        SlotFilter filter = new SlotFilter
        {
            RequiredArmorSlot = ArmorSlotType.Helmet,
        };

        ItemDefinition helmet = new ItemDefinition
        {
            Id = "iron_helmet",
            Armor = new ArmorProperties { Slot = ArmorSlotType.Helmet },
        };

        ItemDefinition boots = new ItemDefinition
        {
            Id = "iron_boots",
            Armor = new ArmorProperties { Slot = ArmorSlotType.Boots },
        };

        ItemDefinition sword = new ItemDefinition
        {
            Id = "sword",
            Category = ItemCategory.Weapon,
        };

        filter.Accepts(helmet).Should().BeTrue();
        filter.Accepts(boots).Should().BeFalse();
        filter.Accepts(sword).Should().BeFalse();
    }

    [Fact]
    public void Accepts_NullDefinition_ReturnsFalse()
    {
        SlotFilter.AcceptAll.Accepts(null!).Should().BeFalse();
    }
}
