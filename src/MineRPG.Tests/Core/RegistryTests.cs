using System;
using System.Collections.Generic;

using FluentAssertions;

using MineRPG.Core.Registry;

namespace MineRPG.Tests.Core;

public sealed class RegistryTests
{
    private sealed class TestItem
    {
        public int Id { get; init; }
        public string Name { get; init; } = "";
    }

    private readonly Registry<int, TestItem> _registry = new();

    [Fact]
    public void Register_AndGet_ReturnsItem()
    {
        // Arrange
        TestItem item = new TestItem { Id = 1, Name = "Stone" };

        // Act
        _registry.Register(1, item);
        TestItem result = _registry.Get(1);

        // Assert
        result.Should().BeSameAs(item);
    }

    [Fact]
    public void Register_DuplicateKey_ThrowsInvalidOperation()
    {
        // Arrange
        _registry.Register(1, new TestItem { Id = 1, Name = "Stone" });

        // Act
        Action act = () => _registry.Register(1, new TestItem { Id = 1, Name = "Dirt" });

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public void Get_UnknownKey_ThrowsKeyNotFound()
    {
        // Act
        Action act = () => _registry.Get(999);

        // Assert
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    public void TryGet_ExistingKey_ReturnsTrueAndValue()
    {
        // Arrange
        TestItem item = new TestItem { Id = 1, Name = "Stone" };
        _registry.Register(1, item);

        // Act
        bool isFound = _registry.TryGet(1, out TestItem? result);

        // Assert
        isFound.Should().BeTrue();
        result.Should().BeSameAs(item);
    }

    [Fact]
    public void TryGet_UnknownKey_ReturnsFalse()
    {
        // Act
        bool isFound = _registry.TryGet(42, out _);

        // Assert
        isFound.Should().BeFalse();
    }

    [Fact]
    public void GetAll_ReturnsItemsInInsertionOrder()
    {
        // Arrange
        TestItem[] items = new[]
        {
            new TestItem { Id = 3, Name = "C" },
            new TestItem { Id = 1, Name = "A" },
            new TestItem { Id = 2, Name = "B" },
        };

        foreach (TestItem item in items)
        {
            _registry.Register(item.Id, item);
        }

        // Act
        IReadOnlyList<TestItem> all = _registry.GetAll();

        // Assert
        all.Should().HaveCount(3);
        all[0].Name.Should().Be("C");
        all[1].Name.Should().Be("A");
        all[2].Name.Should().Be("B");
    }

    [Fact]
    public void Contains_RegisteredKey_ReturnsTrue()
    {
        // Arrange
        _registry.Register(5, new TestItem { Id = 5, Name = "Wood" });

        // Act & Assert
        _registry.Contains(5).Should().BeTrue();
        _registry.Contains(99).Should().BeFalse();
    }

    [Fact]
    public void Count_ReflectsRegisteredItems()
    {
        // Act & Assert
        _registry.Count.Should().Be(0);
        _registry.Register(1, new TestItem { Id = 1 });
        _registry.Count.Should().Be(1);
        _registry.Register(2, new TestItem { Id = 2 });
        _registry.Count.Should().Be(2);
    }
}
