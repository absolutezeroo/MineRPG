using FluentAssertions;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;
using MineRPG.World.Meshing;

namespace MineRPG.Tests.World;

public sealed class ChunkDownsamplerTests
{
    [Fact]
    public void Downsample_EmptyChunk_AllOutputIsAir()
    {
        ChunkData chunk = new(ChunkCoord.Zero);
        int outputSize = ChunkDownsampler.GetOutputSize(2);
        ushort[] output = new ushort[outputSize];

        ChunkDownsampler.Downsample(chunk, 2, output, out int sizeX, out int sizeY, out int sizeZ);

        sizeX.Should().Be(8);
        sizeY.Should().Be(128);
        sizeZ.Should().Be(8);

        for (int i = 0; i < outputSize; i++)
        {
            output[i].Should().Be(0, $"output[{i}] should be air for empty chunk");
        }
    }

    [Fact]
    public void Downsample_Factor2_ReducesDimensions()
    {
        ChunkData chunk = new(ChunkCoord.Zero);
        int outputSize = ChunkDownsampler.GetOutputSize(2);
        ushort[] output = new ushort[outputSize];

        ChunkDownsampler.Downsample(chunk, 2, output, out int sizeX, out int sizeY, out int sizeZ);

        sizeX.Should().Be(ChunkData.SizeX / 2);
        sizeY.Should().Be(ChunkData.SizeY / 2);
        sizeZ.Should().Be(ChunkData.SizeZ / 2);
    }

    [Fact]
    public void Downsample_Factor4_ReducesDimensions()
    {
        ChunkData chunk = new(ChunkCoord.Zero);
        int outputSize = ChunkDownsampler.GetOutputSize(4);
        ushort[] output = new ushort[outputSize];

        ChunkDownsampler.Downsample(chunk, 4, output, out int sizeX, out int sizeY, out int sizeZ);

        sizeX.Should().Be(ChunkData.SizeX / 4);
        sizeY.Should().Be(ChunkData.SizeY / 4);
        sizeZ.Should().Be(ChunkData.SizeZ / 4);
    }

    [Fact]
    public void Downsample_UniformBlock_OutputMatchesBlock()
    {
        ChunkData chunk = new(ChunkCoord.Zero);
        ushort stoneId = 1;

        // Fill a 2x2x2 region with stone at the origin
        for (int y = 0; y < 2; y++)
        {
            for (int z = 0; z < 2; z++)
            {
                for (int x = 0; x < 2; x++)
                {
                    chunk.SetBlock(x, y, z, stoneId);
                }
            }
        }

        int outputSize = ChunkDownsampler.GetOutputSize(2);
        ushort[] output = new ushort[outputSize];

        ChunkDownsampler.Downsample(chunk, 2, output, out int sizeX, out int sizeY, out int sizeZ);

        // The mega-block at (0,0,0) should be stone
        output[0].Should().Be(stoneId);
    }

    [Fact]
    public void Downsample_MixedBlocks_MajorityWins()
    {
        ChunkData chunk = new(ChunkCoord.Zero);
        ushort stoneId = 1;
        ushort dirtId = 2;

        // Fill 6 of 8 blocks with stone, 2 with dirt
        chunk.SetBlock(0, 0, 0, stoneId);
        chunk.SetBlock(1, 0, 0, stoneId);
        chunk.SetBlock(0, 0, 1, stoneId);
        chunk.SetBlock(1, 0, 1, stoneId);
        chunk.SetBlock(0, 1, 0, stoneId);
        chunk.SetBlock(1, 1, 0, stoneId);
        chunk.SetBlock(0, 1, 1, dirtId);
        chunk.SetBlock(1, 1, 1, dirtId);

        int outputSize = ChunkDownsampler.GetOutputSize(2);
        ushort[] output = new ushort[outputSize];

        ChunkDownsampler.Downsample(chunk, 2, output, out _, out _, out _);

        output[0].Should().Be(stoneId, "stone is the majority (6 vs 2)");
    }

    [Fact]
    public void GetOutputSize_Factor2_ReturnsCorrectSize()
    {
        int size = ChunkDownsampler.GetOutputSize(2);

        int expected = (ChunkData.SizeX / 2) * (ChunkData.SizeY / 2) * (ChunkData.SizeZ / 2);
        size.Should().Be(expected);
    }

    [Fact]
    public void GetOutputSize_Factor4_ReturnsCorrectSize()
    {
        int size = ChunkDownsampler.GetOutputSize(4);

        int expected = (ChunkData.SizeX / 4) * (ChunkData.SizeY / 4) * (ChunkData.SizeZ / 4);
        size.Should().Be(expected);
    }
}
