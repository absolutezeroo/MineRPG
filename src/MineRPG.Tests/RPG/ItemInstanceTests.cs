using FluentAssertions;
using MineRPG.RPG.Items;

namespace MineRPG.Tests.RPG;

public sealed class ItemInstanceTests
{
    [Fact]
    public void Constructor_WithValidDefinition_SetsProperties()
    {
        // Arrange
        var def = new ItemDefinition { Id = 1, Name = "Stone", MaxStack = 64 };

        // Act
        var instance = new ItemInstance(def, 10);

        // Assert
        instance.Definition.Should().BeSameAs(def);
        instance.Quantity.Should().Be(10);
    }

    [Fact]
    public void Constructor_WithNullDefinition_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ItemInstance(null!, 1);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("definition");
    }

    [Fact]
    public void Constructor_DefaultQuantity_IsOne()
    {
        var def = new ItemDefinition { Id = 1, Name = "Dirt" };
        var instance = new ItemInstance(def);

        instance.Quantity.Should().Be(1);
    }

    [Fact]
    public void Quantity_CanBeModified()
    {
        var def = new ItemDefinition { Id = 1, Name = "Wood", MaxStack = 64 };
        var instance = new ItemInstance(def, 5);

        instance.Quantity = 32;

        instance.Quantity.Should().Be(32);
    }
}
