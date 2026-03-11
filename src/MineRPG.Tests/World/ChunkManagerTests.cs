using System.Collections.Generic;

using FluentAssertions;

using NSubstitute;

using MineRPG.Core.Events;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.Tests.World;

public sealed class ChunkManagerTests
{
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly ILogger _logger = Substitute.For<ILogger>();

    private ChunkManager CreateManager() => new ChunkManager(_eventBus, _logger);

    [Fact]
    public void GetCoordsInRange_Distance1_Returns9Coords()
    {
        ChunkManager manager = CreateManager();
        ChunkCoord center = new(0, 0);
        List<ChunkCoord> result = new();

        manager.GetCoordsInRange(center, 1, result);

        // 3x3 = 9 coords
        result.Should().HaveCount(9);
    }

    [Fact]
    public void GetCoordsInRange_NoDuplicates()
    {
        ChunkManager manager = CreateManager();
        ChunkCoord center = new(5, 5);
        List<ChunkCoord> result = new();

        manager.GetCoordsInRange(center, 2, result);

        // 5x5 = 25 coords, no duplicates
        result.Should().HaveCount(25);
        result.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GetCoordsInRange_SortedByDistance()
    {
        ChunkManager manager = CreateManager();
        ChunkCoord center = new(0, 0);

        IReadOnlyList<ChunkCoord> result = manager.GetCoordsInRange(center, 3);

        // First element should be center (distance 0)
        result[0].Should().Be(center);

        // Verify sorted by increasing distance
        for (int i = 1; i < result.Count; i++)
        {
            int distPrev = result[i - 1].ChebyshevDistance(center);
            int distCurr = result[i].ChebyshevDistance(center);
            distCurr.Should().BeGreaterThanOrEqualTo(distPrev);
        }
    }

    [Fact]
    public void GetOrCreate_CreatesNewEntry()
    {
        ChunkManager manager = CreateManager();
        ChunkCoord coord = new(1, 2);

        ChunkEntry entry = manager.GetOrCreate(coord);

        entry.Should().NotBeNull();
        entry.Coord.Should().Be(coord);
        manager.Count.Should().Be(1);
    }

    [Fact]
    public void GetOrCreate_ReturnsSameEntry()
    {
        ChunkManager manager = CreateManager();
        ChunkCoord coord = new(1, 2);

        ChunkEntry first = manager.GetOrCreate(coord);
        ChunkEntry second = manager.GetOrCreate(coord);

        first.Should().BeSameAs(second);
        manager.Count.Should().Be(1);
    }

    [Fact]
    public void TryGet_ExistingChunk_ReturnsTrue()
    {
        ChunkManager manager = CreateManager();
        ChunkCoord coord = new(3, 4);
        manager.GetOrCreate(coord);

        bool found = manager.TryGet(coord, out ChunkEntry? entry);

        found.Should().BeTrue();
        entry.Should().NotBeNull();
        entry!.Coord.Should().Be(coord);
    }

    [Fact]
    public void TryGet_MissingChunk_ReturnsFalse()
    {
        ChunkManager manager = CreateManager();

        bool found = manager.TryGet(new ChunkCoord(99, 99), out ChunkEntry? entry);

        found.Should().BeFalse();
    }

    [Fact]
    public void Remove_ExistingChunk_DecreasesCount()
    {
        ChunkManager manager = CreateManager();
        ChunkCoord coord = new(1, 1);
        manager.GetOrCreate(coord);
        manager.Count.Should().Be(1);

        manager.Remove(coord);

        manager.Count.Should().Be(0);
        manager.TryGet(coord, out _).Should().BeFalse();
    }

    [Fact]
    public void GetNeighborData_WithGeneratedNeighbors_ReturnsData()
    {
        ChunkManager manager = CreateManager();
        ChunkCoord center = new(5, 5);

        // Create center and neighbors in Generated state
        ChunkEntry centerEntry = manager.GetOrCreate(center);
        centerEntry.SetState(ChunkState.Generated);

        ChunkEntry eastEntry = manager.GetOrCreate(center.East);
        eastEntry.SetState(ChunkState.Generated);

        ChunkEntry westEntry = manager.GetOrCreate(center.West);
        westEntry.SetState(ChunkState.Generated);

        ChunkData?[] neighbors = manager.GetNeighborData(center);

        neighbors.Should().HaveCount(4);
        neighbors[0].Should().NotBeNull("East neighbor is Generated");
        neighbors[1].Should().NotBeNull("West neighbor is Generated");
        neighbors[2].Should().BeNull("South neighbor doesn't exist");
        neighbors[3].Should().BeNull("North neighbor doesn't exist");
    }

    [Fact]
    public void GetNeighborData_AcceptsRelightPending()
    {
        ChunkManager manager = CreateManager();
        ChunkCoord center = new(0, 0);

        ChunkEntry eastEntry = manager.GetOrCreate(center.East);
        eastEntry.SetState(ChunkState.RelightPending);

        ChunkData?[] neighbors = manager.GetNeighborData(center);

        neighbors[0].Should().NotBeNull("RelightPending has valid voxel data");
    }

    [Fact]
    public void GetNeighborData_RejectsGenerating()
    {
        ChunkManager manager = CreateManager();
        ChunkCoord center = new(0, 0);

        ChunkEntry eastEntry = manager.GetOrCreate(center.East);
        eastEntry.SetState(ChunkState.Generating);

        ChunkData?[] neighbors = manager.GetNeighborData(center);

        neighbors[0].Should().BeNull("Generating does not have valid voxel data");
    }

    [Fact]
    public void GetNeighborData_ZeroAllocOverload_FillsBuffer()
    {
        ChunkManager manager = CreateManager();
        ChunkCoord center = new(0, 0);
        ChunkData?[] buffer = new ChunkData?[4];

        ChunkEntry eastEntry = manager.GetOrCreate(center.East);
        eastEntry.SetState(ChunkState.Ready);

        ChunkData?[] result = manager.GetNeighborData(center, buffer);

        result.Should().BeSameAs(buffer);
        result[0].Should().NotBeNull();
    }
}
