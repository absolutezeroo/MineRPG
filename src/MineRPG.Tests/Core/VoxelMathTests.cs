using FluentAssertions;

using MineRPG.Core.Math;

namespace MineRPG.Tests.Core;

public sealed class VoxelMathTests
{
    private const int SizeX = 16;
    private const int SizeZ = 16;

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 0, 0, 1)]
    [InlineData(0, 0, 1, 16)]
    [InlineData(0, 1, 0, 256)]
    [InlineData(15, 0, 15, 255)]
    public void GetIndex_ReturnsCorrectFlatIndex(int x, int y, int z, int expected)
    {
        VoxelMath.GetIndex(x, y, z, SizeX, SizeZ).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 1, 0, 0)]
    [InlineData(16, 0, 1, 0)]
    [InlineData(256, 0, 0, 1)]
    public void GetPosition_ReturnsCorrectCoordinates(int index, int expectedX, int expectedZ, int expectedY)
    {
        (int x, int y, int z) = VoxelMath.GetPosition(index, SizeX, SizeZ);
        x.Should().Be(expectedX);
        y.Should().Be(expectedY);
        z.Should().Be(expectedZ);
    }

    [Fact]
    public void GetIndex_AndGetPosition_AreInverses()
    {
        for (int x = 0; x < SizeX; x++)
        {
            for (int z = 0; z < SizeZ; z++)
            {
                for (int y = 0; y < 4; y++)
                {
                    int index = VoxelMath.GetIndex(x, y, z, SizeX, SizeZ);
                    (int rx, int ry, int rz) = VoxelMath.GetPosition(index, SizeX, SizeZ);
                    rx.Should().Be(x);
                    ry.Should().Be(y);
                    rz.Should().Be(z);
                }
            }
        }
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(15, 15, 0, 0)]
    [InlineData(16, 0, 1, 0)]
    [InlineData(31, 15, 1, 0)]
    [InlineData(-1, -16, -1, -1)]
    [InlineData(-16, -16, -1, -1)]
    [InlineData(-17, -16, -2, -1)]
    public void WorldToChunk_HandlesNegativeCoordinates(int worldX, int worldZ, int expectedCX, int expectedCZ)
    {
        (int cx, int cz) = VoxelMath.WorldToChunk(worldX, worldZ, SizeX, SizeZ);
        cx.Should().Be(expectedCX);
        cz.Should().Be(expectedCZ);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(5, 5, 5, 5)]
    [InlineData(16, 16, 0, 0)]
    [InlineData(-1, -1, 15, 15)]
    [InlineData(-16, -16, 0, 0)]
    public void WorldToLocal_ReturnsPositiveLocalCoordinates(int worldX, int worldZ, int expectedLX, int expectedLZ)
    {
        (int lx, int lz) = VoxelMath.WorldToLocal(worldX, worldZ, SizeX, SizeZ);
        lx.Should().Be(expectedLX);
        lz.Should().Be(expectedLZ);
    }

    [Fact]
    public void FaceDirections_HasSixEntries()
    {
        VoxelMath.FaceDirections.Should().HaveCount(6);
    }

    [Theory]
    [InlineData(0f, 10f, 0.5f, 5f)]
    [InlineData(0f, 10f, 0f, 0f)]
    [InlineData(0f, 10f, 1f, 10f)]
    public void Lerp_InterpolatesCorrectly(float a, float b, float t, float expected)
    {
        VoxelMath.Lerp(a, b, t).Should().BeApproximately(expected, 0.001f);
    }

    [Theory]
    [InlineData(5f, 0f, 10f, 5f)]
    [InlineData(-5f, 0f, 10f, 0f)]
    [InlineData(15f, 0f, 10f, 10f)]
    public void Clamp_ClampsCorrectly(float value, float min, float max, float expected)
    {
        VoxelMath.Clamp(value, min, max).Should().BeApproximately(expected, 0.001f);
    }
}
