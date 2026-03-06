using FluentAssertions;

using MineRPG.RPG.Equipment;
using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.Tests.RPG;

public sealed class EquipmentManagerTests
{
    private static ItemRegistry CreateArmorRegistry()
    {
        ItemRegistry registry = new ItemRegistry();

        registry.Register(new ItemDefinition
        {
            Id = "iron_helmet",
            Category = ItemCategory.Armor,
            MaxStackSize = 1,
            Armor = new ArmorProperties
            {
                Slot = ArmorSlotType.Helmet,
                Defense = 2.0f,
                Toughness = 0f,
                Weight = 2.0f,
                Resistances = new[] { "fire" },
            },
        });

        registry.Register(new ItemDefinition
        {
            Id = "iron_chestplate",
            Category = ItemCategory.Armor,
            MaxStackSize = 1,
            Armor = new ArmorProperties
            {
                Slot = ArmorSlotType.Chestplate,
                Defense = 6.0f,
                Toughness = 1.0f,
                Weight = 5.0f,
            },
        });

        registry.Register(new ItemDefinition
        {
            Id = "iron_leggings",
            Category = ItemCategory.Armor,
            MaxStackSize = 1,
            Armor = new ArmorProperties
            {
                Slot = ArmorSlotType.Leggings,
                Defense = 5.0f,
                Weight = 4.0f,
            },
        });

        registry.Register(new ItemDefinition
        {
            Id = "iron_boots",
            Category = ItemCategory.Armor,
            MaxStackSize = 1,
            Armor = new ArmorProperties
            {
                Slot = ArmorSlotType.Boots,
                Defense = 2.0f,
                Weight = 2.0f,
            },
        });

        registry.Freeze();
        return registry;
    }

    private static EquipmentManager CreateManager(
        ItemRegistry registry,
        IReadOnlyList<SetBonusDefinition>? sets = null)
    {
        List<SlotFilter> armorFilters = new()
        {
            new SlotFilter { RequiredArmorSlot = ArmorSlotType.Helmet },
            new SlotFilter { RequiredArmorSlot = ArmorSlotType.Chestplate },
            new SlotFilter { RequiredArmorSlot = ArmorSlotType.Leggings },
            new SlotFilter { RequiredArmorSlot = ArmorSlotType.Boots },
        };

        Inventory armorInventory = new Inventory(armorFilters, registry);
        return new EquipmentManager(armorInventory, registry, sets ?? []);
    }

    [Fact]
    public void Equip_SetsItemInSlot()
    {
        ItemRegistry registry = CreateArmorRegistry();
        EquipmentManager manager = CreateManager(registry);
        ItemInstance helmet = new ItemInstance("iron_helmet", 1);

        ItemInstance? previous = manager.Equip(ArmorSlotType.Helmet, helmet);

        previous.Should().BeNull();
        manager.TotalDefense.Should().Be(2.0f);
    }

    [Fact]
    public void Equip_ReturnsOldItem()
    {
        ItemRegistry registry = CreateArmorRegistry();
        EquipmentManager manager = CreateManager(registry);
        ItemInstance helmet1 = new ItemInstance("iron_helmet", 1);
        ItemInstance helmet2 = new ItemInstance("iron_helmet", 1);

        manager.Equip(ArmorSlotType.Helmet, helmet1);
        ItemInstance? previous = manager.Equip(ArmorSlotType.Helmet, helmet2);

        previous.Should().BeSameAs(helmet1);
    }

    [Fact]
    public void Unequip_RemovesItemFromSlot()
    {
        ItemRegistry registry = CreateArmorRegistry();
        EquipmentManager manager = CreateManager(registry);

        manager.Equip(ArmorSlotType.Helmet, new ItemInstance("iron_helmet", 1));
        ItemInstance? removed = manager.Unequip(ArmorSlotType.Helmet);

        removed.Should().NotBeNull();
        manager.TotalDefense.Should().Be(0f);
    }

    [Fact]
    public void TotalDefense_SumsAllEquipped()
    {
        ItemRegistry registry = CreateArmorRegistry();
        EquipmentManager manager = CreateManager(registry);

        manager.Equip(ArmorSlotType.Helmet, new ItemInstance("iron_helmet", 1));
        manager.Equip(ArmorSlotType.Chestplate, new ItemInstance("iron_chestplate", 1));

        manager.TotalDefense.Should().Be(8.0f);
    }

    [Fact]
    public void TotalWeight_SumsAllEquipped()
    {
        ItemRegistry registry = CreateArmorRegistry();
        EquipmentManager manager = CreateManager(registry);

        manager.Equip(ArmorSlotType.Helmet, new ItemInstance("iron_helmet", 1));
        manager.Equip(ArmorSlotType.Chestplate, new ItemInstance("iron_chestplate", 1));

        manager.TotalWeight.Should().Be(7.0f);
    }

    [Fact]
    public void ActiveResistances_CollectsFromAllPieces()
    {
        ItemRegistry registry = CreateArmorRegistry();
        EquipmentManager manager = CreateManager(registry);

        manager.Equip(ArmorSlotType.Helmet, new ItemInstance("iron_helmet", 1));

        manager.ActiveResistances.Should().Contain("fire");
    }

    [Fact]
    public void CanEquip_WithCorrectSlot_ReturnsTrue()
    {
        ItemRegistry registry = CreateArmorRegistry();
        EquipmentManager manager = CreateManager(registry);

        bool canEquip = manager.CanEquip(
            ArmorSlotType.Helmet,
            registry.Get("iron_helmet"));

        canEquip.Should().BeTrue();
    }

    [Fact]
    public void CanEquip_WithWrongSlot_ReturnsFalse()
    {
        ItemRegistry registry = CreateArmorRegistry();
        EquipmentManager manager = CreateManager(registry);

        bool canEquip = manager.CanEquip(
            ArmorSlotType.Boots,
            registry.Get("iron_helmet"));

        canEquip.Should().BeFalse();
    }

    [Fact]
    public void SetBonus_ActivatesAtRequiredPieces()
    {
        ItemRegistry registry = CreateArmorRegistry();

        SetBonusDefinition ironSet = new SetBonusDefinition
        {
            SetId = "iron_set",
            DisplayName = "Iron Set",
            Pieces = new[] { "iron_helmet", "iron_chestplate", "iron_leggings", "iron_boots" },
            Bonuses = new[]
            {
                new SetBonus
                {
                    RequiredPieces = 2,
                    Description = "+10% Mining Speed",
                },
                new SetBonus
                {
                    RequiredPieces = 4,
                    Description = "+5 Defense",
                },
            },
        };

        EquipmentManager manager = CreateManager(registry, new[] { ironSet });

        manager.Equip(ArmorSlotType.Helmet, new ItemInstance("iron_helmet", 1));
        manager.Equip(ArmorSlotType.Chestplate, new ItemInstance("iron_chestplate", 1));

        manager.ActiveSetId.Should().Be("iron_set");
        manager.ActiveSetBonuses.Should().HaveCount(1);
        manager.ActiveSetBonuses[0].RequiredPieces.Should().Be(2);
    }
}
