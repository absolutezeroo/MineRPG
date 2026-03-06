using FluentAssertions;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.Tests.World;

public sealed class ChunkPriorityCalculatorTests
{
    [Fact]
    public void ComputePriority_PlayerChunk_ReturnsImmediate()
    {
        ChunkCoord player = new(0, 0);
        ChunkCoord target = new(0, 0);

        int priority = ChunkPriorityCalculator.ComputePriority(target, player, 0f, -1f);

        priority.Should().Be(ChunkPriorityCalculator.PriorityImmediate);
    }

    [Fact]
    public void ComputePriority_AdjacentChunk_ReturnsImmediate()
    {
        ChunkCoord player = new(0, 0);
        ChunkCoord target = new(1, 0);

        int priority = ChunkPriorityCalculator.ComputePriority(target, player, 0f, -1f);

        priority.Should().BeLessThanOrEqualTo(ChunkPriorityCalculator.PriorityImmediate + 1);
    }

    [Fact]
    public void ComputePriority_InFront_HasHigherPriorityThanBehind()
    {
        ChunkCoord player = new(0, 0);

        // Player looking north (Z-)
        float forwardX = 0f;
        float forwardZ = -1f;

        ChunkCoord chunkInFront = new(0, -5);
        ChunkCoord chunkBehind = new(0, 5);

        int frontPriority = ChunkPriorityCalculator.ComputePriority(
            chunkInFront, player, forwardX, forwardZ);
        int behindPriority = ChunkPriorityCalculator.ComputePriority(
            chunkBehind, player, forwardX, forwardZ);

        frontPriority.Should().BeLessThan(behindPriority,
            "chunks in front should have higher priority (lower number)");
    }

    [Fact]
    public void ComputePriority_CloserChunk_HasHigherPriority()
    {
        ChunkCoord player = new(0, 0);
        float forwardX = 0f;
        float forwardZ = -1f;

        ChunkCoord closeChunk = new(0, -3);
        ChunkCoord farChunk = new(0, -10);

        int closePriority = ChunkPriorityCalculator.ComputePriority(
            closeChunk, player, forwardX, forwardZ);
        int farPriority = ChunkPriorityCalculator.ComputePriority(
            farChunk, player, forwardX, forwardZ);

        closePriority.Should().BeLessThan(farPriority,
            "closer chunks should have higher priority");
    }

    [Fact]
    public void ComputePriority_NearButBehind_HasReasonablePriority()
    {
        ChunkCoord player = new(0, 0);
        float forwardX = 0f;
        float forwardZ = -1f;

        ChunkCoord nearBehind = new(0, 3);

        int priority = ChunkPriorityCalculator.ComputePriority(
            nearBehind, player, forwardX, forwardZ);

        priority.Should().BeGreaterThanOrEqualTo(ChunkPriorityCalculator.PriorityNearOutOfFrustum);
        priority.Should().BeLessThan(ChunkPriorityCalculator.PriorityMidOutOfFrustum);
    }

    [Fact]
    public void ComputePriority_DistantBehind_HasLowestPriority()
    {
        ChunkCoord player = new(0, 0);
        float forwardX = 0f;
        float forwardZ = -1f;

        ChunkCoord distantBehind = new(0, 20);

        int priority = ChunkPriorityCalculator.ComputePriority(
            distantBehind, player, forwardX, forwardZ);

        priority.Should().BeGreaterThanOrEqualTo(ChunkPriorityCalculator.PriorityFarBehind);
    }
}
