using System;
using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using MineRPG.Core.Extensions;

namespace MineRPG.Tests.Core;

public sealed class CollectionExtensionsTests
{
    [Fact]
    public void PopLast_WithNonEmptyList_RemovesAndReturnsLastElement()
    {
        // Arrange
        List<int> list = new List<int> { 10, 20, 30 };

        // Act
        int result = list.PopLast();

        // Assert
        result.Should().Be(30);
        list.Should().HaveCount(2);
        list.Should().Equal(10, 20);
    }

    [Fact]
    public void PopLast_WithEmptyList_ThrowsInvalidOperationException()
    {
        // Arrange
        List<int> list = new List<int>();

        // Act
        Action act = () => list.PopLast();

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Shuffle_WithSeededRandom_ProducesDeterministicResult()
    {
        // Arrange
        List<int> list1 = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        List<int> list2 = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // Act
        list1.Shuffle(new Random(42));
        list2.Shuffle(new Random(42));

        // Assert
        list1.Should().Equal(list2);
    }

    [Fact]
    public void Shuffle_WithSingleElement_DoesNotThrow()
    {
        // Arrange
        List<int> list = new List<int> { 42 };

        // Act
        Action act = () => list.Shuffle(new Random(0));

        // Assert
        act.Should().NotThrow();
        list.Should().ContainSingle().Which.Should().Be(42);
    }

    [Fact]
    public void WeightedRandom_WithEqualWeights_ReturnsAnyItem()
    {
        // Arrange
        List<string> items = new List<string> { "a", "b", "c" };
        Random rng = new Random(42);

        // Act
        string? result = items.WeightedRandom(_ => 1f, rng);

        // Assert
        result.Should().NotBeNull();
        items.Should().Contain(result!);
    }

    [Fact]
    public void WeightedRandom_WithSingleDominantWeight_ReturnsDominantItem()
    {
        // Arrange
        List<string> items = new List<string> { "rare", "common" };

        // Act - run many times, dominant item (weight 1000) should always win
        List<string?> results = Enumerable.Range(0, 100)
            .Select(seed => items.WeightedRandom(
                i => i == "common" ? 1000f : 0.001f,
                new Random(seed)))
            .ToList();

        // Assert
        results.Should().AllBe("common");
    }

    [Fact]
    public void WeightedRandom_WithEmptyList_ReturnsDefault()
    {
        // Arrange
        List<string> items = new List<string>();

        // Act
        string? result = items.WeightedRandom(_ => 1f, new Random(0));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void WeightedRandom_WithZeroTotalWeight_ThrowsInvalidOperationException()
    {
        // Arrange
        List<string> items = new List<string> { "a" };

        // Act
        Action act = () => items.WeightedRandom(_ => 0f, new Random(0));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*total weight*positive*");
    }

    [Fact]
    public void TryGetFirst_WithNonEmptySpan_ReturnsTrueAndFirstElement()
    {
        // Arrange
        ReadOnlySpan<int> span = stackalloc int[] { 42, 99 };

        // Act
        bool isFound = span.TryGetFirst(out int result);

        // Assert
        isFound.Should().BeTrue();
        result.Should().Be(42);
    }

    [Fact]
    public void TryGetFirst_WithEmptySpan_ReturnsFalse()
    {
        // Arrange
        ReadOnlySpan<int> span = ReadOnlySpan<int>.Empty;

        // Act
        bool isFound = span.TryGetFirst(out _);

        // Assert
        isFound.Should().BeFalse();
    }
}
