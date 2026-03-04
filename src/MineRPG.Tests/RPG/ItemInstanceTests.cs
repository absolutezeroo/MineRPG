using System;

using FluentAssertions;

using MineRPG.RPG.Items;

namespace MineRPG.Tests.RPG;

public sealed class ItemInstanceTests
{
    [Fact]
    public void Constructor_WithValidDefinition_SetsProperties()
    {
        // Arrange
        ItemDefinition def = new ItemDefinition { Id = 1, Name = "Stone", MaxStack = 64 };

        // Act
        ItemInstance instance = new ItemInstance(def, 10);

        // Assert
        instance.Definition.Should().BeSameAs(def);
        instance.Quantity.Should().Be(10);
    }

    [Fact]
    public void Constructor_WithNullDefinition_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new ItemInstance(null!, 1);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("definition");
    }

    [Fact]
    public void Constructor_DefaultQuantity_IsOne()
    {
        ItemDefinition def = new ItemDefinition { Id = 1, Name = "Dirt" };
        ItemInstance instance = new ItemInstance(def);

        instance.Quantity.Should().Be(1);
    }

    [Fact]
    public void Quantity_CanBeModified()
    {
        ItemDefinition def = new ItemDefinition { Id = 1, Name = "Wood", MaxStack = 64 };
        ItemInstance instance = new ItemInstance(def, 5);

        instance.Quantity = 32;

        instance.Quantity.Should().Be(32);
    }
}
