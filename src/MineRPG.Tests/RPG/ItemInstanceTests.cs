using System;

using FluentAssertions;

using MineRPG.RPG.Items;

namespace MineRPG.Tests.RPG;

public sealed class ItemInstanceTests
{
    [Fact]
    public void Constructor_WithValidId_SetsProperties()
    {
        ItemInstance instance = new ItemInstance("stone", 10);

        instance.DefinitionId.Should().Be("stone");
        instance.Count.Should().Be(10);
    }

    [Fact]
    public void Constructor_WithNullId_ThrowsArgumentException()
    {
        Action act = () => new ItemInstance(null!, 1);

        act.Should().Throw<ArgumentException>().WithParameterName("definitionId");
    }

    [Fact]
    public void Constructor_WithEmptyId_ThrowsArgumentException()
    {
        Action act = () => new ItemInstance("", 1);

        act.Should().Throw<ArgumentException>().WithParameterName("definitionId");
    }

    [Fact]
    public void Constructor_WithZeroCount_ThrowsArgumentOutOfRangeException()
    {
        Action act = () => new ItemInstance("stone", 0);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("count");
    }

    [Fact]
    public void Constructor_DefaultCount_IsOne()
    {
        ItemInstance instance = new ItemInstance("dirt");

        instance.Count.Should().Be(1);
    }

    [Fact]
    public void Constructor_DefaultDurability_IsMinusOne()
    {
        ItemInstance instance = new ItemInstance("dirt");

        instance.CurrentDurability.Should().Be(-1);
        instance.HasDurability.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithDurability_TracksDurability()
    {
        ItemInstance instance = new ItemInstance("iron_pickaxe", 1, 250);

        instance.HasDurability.Should().BeTrue();
        instance.CurrentDurability.Should().Be(250);
        instance.IsBroken.Should().BeFalse();
    }

    [Fact]
    public void DamageDurability_ReducesDurability()
    {
        ItemInstance instance = new ItemInstance("iron_pickaxe", 1, 250);

        instance.DamageDurability(10);

        instance.CurrentDurability.Should().Be(240);
    }

    [Fact]
    public void DamageDurability_ToZero_IsBroken()
    {
        ItemInstance instance = new ItemInstance("iron_pickaxe", 1, 5);

        instance.DamageDurability(10);

        instance.CurrentDurability.Should().Be(0);
        instance.IsBroken.Should().BeTrue();
    }

    [Fact]
    public void DamageDurability_WithoutDurability_DoesNothing()
    {
        ItemInstance instance = new ItemInstance("stone", 10);

        instance.DamageDurability(5);

        instance.CurrentDurability.Should().Be(-1);
    }

    [Fact]
    public void RepairDurability_RestoresDurability()
    {
        ItemInstance instance = new ItemInstance("iron_pickaxe", 1, 100);

        instance.RepairDurability(50, 250);

        instance.CurrentDurability.Should().Be(150);
    }

    [Fact]
    public void RepairDurability_CapsAtMax()
    {
        ItemInstance instance = new ItemInstance("iron_pickaxe", 1, 240);

        instance.RepairDurability(50, 250);

        instance.CurrentDurability.Should().Be(250);
    }

    [Fact]
    public void CanStackWith_SameId_NoDurability_ReturnsTrue()
    {
        ItemInstance instance1 = new ItemInstance("stone", 10);
        ItemInstance instance2 = new ItemInstance("stone", 5);

        instance1.CanStackWith(instance2).Should().BeTrue();
    }

    [Fact]
    public void CanStackWith_DifferentId_ReturnsFalse()
    {
        ItemInstance instance1 = new ItemInstance("stone", 10);
        ItemInstance instance2 = new ItemInstance("dirt", 5);

        instance1.CanStackWith(instance2).Should().BeFalse();
    }

    [Fact]
    public void CanStackWith_HasDurability_ReturnsFalse()
    {
        ItemInstance instance1 = new ItemInstance("iron_pickaxe", 1, 250);
        ItemInstance instance2 = new ItemInstance("iron_pickaxe", 1, 250);

        instance1.CanStackWith(instance2).Should().BeFalse();
    }

    [Fact]
    public void CanStackWith_Null_ReturnsFalse()
    {
        ItemInstance instance = new ItemInstance("stone", 10);

        instance.CanStackWith(null!).Should().BeFalse();
    }

    [Fact]
    public void Split_CreatesNewInstance()
    {
        ItemInstance instance = new ItemInstance("stone", 10);

        ItemInstance split = instance.Split(3);

        instance.Count.Should().Be(7);
        split.Count.Should().Be(3);
        split.DefinitionId.Should().Be("stone");
    }

    [Fact]
    public void Split_WithTooManyItems_Throws()
    {
        ItemInstance instance = new ItemInstance("stone", 10);

        Action act = () => instance.Split(10);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Merge_CombinesStacks()
    {
        ItemInstance instance1 = new ItemInstance("stone", 10);
        ItemInstance instance2 = new ItemInstance("stone", 5);

        int overflow = instance1.Merge(instance2, 64);

        instance1.Count.Should().Be(15);
        overflow.Should().Be(0);
    }

    [Fact]
    public void Merge_WithOverflow_ReturnsRemainder()
    {
        ItemInstance instance1 = new ItemInstance("stone", 60);
        ItemInstance instance2 = new ItemInstance("stone", 10);

        int overflow = instance1.Merge(instance2, 64);

        instance1.Count.Should().Be(64);
        overflow.Should().Be(6);
        instance2.Count.Should().Be(6);
    }

    [Fact]
    public void Merge_DifferentItems_ReturnsFullCount()
    {
        ItemInstance instance1 = new ItemInstance("stone", 10);
        ItemInstance instance2 = new ItemInstance("dirt", 5);

        int overflow = instance1.Merge(instance2, 64);

        instance1.Count.Should().Be(10);
        overflow.Should().Be(5);
    }

    [Fact]
    public void AddEnchantment_AddsToList()
    {
        ItemInstance instance = new ItemInstance("diamond_sword", 1, 1561);
        Enchantment enchantment = new Enchantment { EnchantmentId = "sharpness", Level = 3 };

        instance.AddEnchantment(enchantment);

        instance.Enchantments.Should().HaveCount(1);
        instance.Enchantments[0].EnchantmentId.Should().Be("sharpness");
        instance.Enchantments[0].Level.Should().Be(3);
    }

    [Fact]
    public void CanStackWith_DifferentEnchantments_ReturnsFalse()
    {
        ItemInstance instance1 = new ItemInstance("stone", 10);
        instance1.AddEnchantment(new Enchantment { EnchantmentId = "fortune", Level = 1 });

        ItemInstance instance2 = new ItemInstance("stone", 5);

        instance1.CanStackWith(instance2).Should().BeFalse();
    }

    [Fact]
    public void CanStackWith_DifferentCustomData_ReturnsFalse()
    {
        ItemInstance instance1 = new ItemInstance("stone", 10);
        instance1.CustomData["renamed"] = "My Stone";

        ItemInstance instance2 = new ItemInstance("stone", 5);

        instance1.CanStackWith(instance2).Should().BeFalse();
    }
}
