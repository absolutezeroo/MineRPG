using System;

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
        ObjectPool<TestObject> pool = new ObjectPool<TestObject>(() => new TestObject { Value = 42 });

        // Act
        TestObject item = pool.Rent();

        // Assert
        item.Should().NotBeNull();
        item.Value.Should().Be(42);
    }

    [Fact]
    public void Return_AndRent_ReusesInstance()
    {
        // Arrange
        ObjectPool<TestObject> pool = new ObjectPool<TestObject>(() => new TestObject());
        TestObject original = pool.Rent();
        original.Value = 99;

        // Act
        pool.Return(original);
        TestObject reused = pool.Rent();

        // Assert
        reused.Should().BeSameAs(original);
        reused.Value.Should().Be(99);
    }

    [Fact]
    public void Return_WithReset_ResetsBeforeStoring()
    {
        // Arrange
        ObjectPool<TestObject> pool = new ObjectPool<TestObject>(
            () => new TestObject(),
            obj => obj.Value = 0);

        TestObject item = pool.Rent();
        item.Value = 42;

        // Act
        pool.Return(item);
        TestObject reused = pool.Rent();

        // Assert
        reused.Value.Should().Be(0);
    }

    [Fact]
    public void IdleCount_TracksPooledItems()
    {
        // Arrange
        ObjectPool<TestObject> pool = new ObjectPool<TestObject>(() => new TestObject());

        // Act & Assert
        pool.IdleCount.Should().Be(0);

        TestObject item1 = pool.Rent();
        TestObject item2 = pool.Rent();
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
        Action act = () => new ObjectPool<TestObject>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ZeroMaxCapacity_ThrowsArgumentOutOfRange()
    {
        // Act
        Action act = () => new ObjectPool<TestObject>(() => new TestObject(), maxCapacity: 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NegativeMaxCapacity_ThrowsArgumentOutOfRange()
    {
        // Act
        Action act = () => new ObjectPool<TestObject>(() => new TestObject(), maxCapacity: -1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Return_WhenAtMaxCapacity_DropsItem()
    {
        // Arrange
        ObjectPool<TestObject> pool = new ObjectPool<TestObject>(() => new TestObject(), maxCapacity: 2);
        TestObject a = pool.Rent();
        TestObject b = pool.Rent();
        TestObject c = pool.Rent();

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
        ObjectPool<TestObject> pool = new ObjectPool<TestObject>(() => new TestObject(), maxCapacity: 10);

        // Assert
        pool.MaxCapacity.Should().Be(10);
    }

    [Fact]
    public void PreAllocate_FillsPoolWithInstances()
    {
        // Arrange
        ObjectPool<TestObject> pool = new ObjectPool<TestObject>(() => new TestObject { Value = 7 });

        // Act
        pool.PreAllocate(5);

        // Assert
        pool.IdleCount.Should().Be(5);

        TestObject item = pool.Rent();
        item.Value.Should().Be(7);
        pool.IdleCount.Should().Be(4);
    }

    [Fact]
    public void PreAllocate_RespectsMaxCapacity()
    {
        // Arrange
        ObjectPool<TestObject> pool = new ObjectPool<TestObject>(() => new TestObject(), maxCapacity: 3);

        // Act
        pool.PreAllocate(10);

        // Assert
        pool.IdleCount.Should().Be(3);
    }

    [Fact]
    public void PreAllocate_NegativeCount_ThrowsArgumentOutOfRange()
    {
        // Arrange
        ObjectPool<TestObject> pool = new ObjectPool<TestObject>(() => new TestObject());

        // Act
        Action act = () => pool.PreAllocate(-1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PreAllocate_ZeroCount_DoesNothing()
    {
        // Arrange
        ObjectPool<TestObject> pool = new ObjectPool<TestObject>(() => new TestObject());

        // Act
        pool.PreAllocate(0);

        // Assert
        pool.IdleCount.Should().Be(0);
    }
}
