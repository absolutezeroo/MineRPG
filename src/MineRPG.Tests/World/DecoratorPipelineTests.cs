using System;
using System.Collections.Generic;

using FluentAssertions;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Generation;
using MineRPG.World.Generation.Decorators;

namespace MineRPG.Tests.World;

public sealed class DecoratorPipelineTests
{
    [Fact]
    public void DecorateChunk_CallsAllDecorators()
    {
        // Arrange
        int callCount = 0;
        TestDecorator decorator1 = new TestDecorator(() => callCount++);
        TestDecorator decorator2 = new TestDecorator(() => callCount++);

        DecoratorPipeline pipeline = new DecoratorPipeline(
            new List<IDecorator> { decorator1, decorator2 });

        ChunkData data = new ChunkData(new ChunkCoord(0, 0));
        BiomeDefinition[] biomeMap = new BiomeDefinition[256];
        int[] heightMap = new int[256];

        // Act
        pipeline.DecorateChunk(data, biomeMap, heightMap, 0, 0, 42);

        // Assert
        callCount.Should().Be(2);
    }

    [Fact]
    public void DecorateChunk_IsDeterministic()
    {
        // Arrange
        List<int> seeds1 = new List<int>();
        List<int> seeds2 = new List<int>();

        SeedCapturingDecorator decorator1 = new SeedCapturingDecorator(seeds1);
        SeedCapturingDecorator decorator2 = new SeedCapturingDecorator(seeds2);

        DecoratorPipeline pipeline1 = new DecoratorPipeline(
            new List<IDecorator> { decorator1 });
        DecoratorPipeline pipeline2 = new DecoratorPipeline(
            new List<IDecorator> { decorator2 });

        ChunkData data = new ChunkData(new ChunkCoord(0, 0));
        BiomeDefinition[] biomeMap = new BiomeDefinition[256];
        int[] heightMap = new int[256];

        // Act
        pipeline1.DecorateChunk(data, biomeMap, heightMap, 10, 20, 42);
        pipeline2.DecorateChunk(data, biomeMap, heightMap, 10, 20, 42);

        // Assert — same seed produces same random value
        seeds1.Should().Equal(seeds2);
    }

    private sealed class TestDecorator : IDecorator
    {
        private readonly Action _onDecorate;

        public TestDecorator(Action onDecorate)
        {
            _onDecorate = onDecorate;
        }

        public void Decorate(
            ChunkData data,
            BiomeDefinition[] biomeMap,
            int[] heightMap,
            int chunkWorldX,
            int chunkWorldZ,
            Random random)
        {
            _onDecorate();
        }
    }

    private sealed class SeedCapturingDecorator : IDecorator
    {
        private readonly List<int> _capturedValues;

        public SeedCapturingDecorator(List<int> capturedValues)
        {
            _capturedValues = capturedValues;
        }

        public void Decorate(
            ChunkData data,
            BiomeDefinition[] biomeMap,
            int[] heightMap,
            int chunkWorldX,
            int chunkWorldZ,
            Random random)
        {
            _capturedValues.Add(random.Next());
        }
    }
}
