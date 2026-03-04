using FluentAssertions;
using MineRPG.Core.Pooling;

namespace MineRPG.Tests.Core;

public sealed class ObjectPoolTests
{
    private sealed class TestObject
    {
        public int Value { get; set; }
    }

    [Fact]
    public void Rent_WhenEmpty_CreatesNewInstance()
    {
        // Arrange
        var pool = new ObjectPool<TestObject>(() => new TestObject { Value = 42 });

        // Act
        var item = pool.Rent();

        // Assert
        item.Should().NotBeNull();
        item.Value.Should().Be(42);
    }

    [Fact]
    public void Return_AndRent_ReusesInstance()
    {
        // Arrange
        var pool = new ObjectPool<TestObject>(() => new TestObject());
        var original = pool.Rent();
        original.Value = 99;

        // Act
        pool.Return(original);
        var reused = pool.Rent();

        // Assert
        reused.Should().BeSameAs(original);
        reused.Value.Should().Be(99);
    }

    [Fact]
    public void Return_WithReset_ResetsBeforeStoring()
    {
        // Arrange
        var pool = new ObjectPool<TestObject>(
            () => new TestObject(),
            obj => obj.Value = 0);

        var item = pool.Rent();
        item.Value = 42;

        // Act
        pool.Return(item);
        var reused = pool.Rent();

        // Assert
        reused.Value.Should().Be(0);
    }

    [Fact]
    public void IdleCount_TracksPooledItems()
    {
        // Arrange
        var pool = new ObjectPool<TestObject>(() => new TestObject());

        // Act & Assert
        pool.IdleCount.Should().Be(0);

        var item1 = pool.Rent();
        var item2 = pool.Rent();
        pool.IdleCount.Should().Be(0);

        pool.Return(item1);
        pool.IdleCount.Should().Be(1);

        pool.Return(item2);
        pool.IdleCount.Should().Be(2);

        pool.Rent();
        pool.IdleCount.Should().Be(1);
    }

    [Fact]
    public void Constructor_NullFactory_ThrowsArgumentNull()
    {
        // Act
        var act = () => new ObjectPool<TestObject>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ZeroMaxCapacity_ThrowsArgumentOutOfRange()
    {
        // Act
        var act = () => new ObjectPool<TestObject>(() => new TestObject(), maxCapacity: 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NegativeMaxCapacity_ThrowsArgumentOutOfRange()
    {
        // Act
        var act = () => new ObjectPool<TestObject>(() => new TestObject(), maxCapacity: -1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Return_WhenAtMaxCapacity_DropsItem()
    {
        // Arrange
        var pool = new ObjectPool<TestObject>(() => new TestObject(), maxCapacity: 2);
        var a = pool.Rent();
        var b = pool.Rent();
        var c = pool.Rent();

        pool.Return(a);
        pool.Return(b);
        pool.IdleCount.Should().Be(2);

        // Act — pool is at capacity, this should be dropped
        pool.Return(c);

        // Assert
        pool.IdleCount.Should().Be(2);
    }

    [Fact]
    public void MaxCapacity_ReturnsConfiguredValue()
    {
        // Arrange
        var pool = new ObjectPool<TestObject>(() => new TestObject(), maxCapacity: 10);

        // Assert
        pool.MaxCapacity.Should().Be(10);
    }
}
