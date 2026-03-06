using FluentAssertions;

using MineRPG.World.Spatial;

namespace MineRPG.Tests.World;

public sealed class ChunkVisibilityMatrixTests
{
    [Fact]
    public void AllVisible_CanSeeThrough_AllDirections()
    {
        ChunkVisibilityMatrix matrix = ChunkVisibilityMatrix.AllVisible;

        for (int entry = 0; entry < ChunkVisibilityMatrix.FaceCount; entry++)
        {
            for (int exit = 0; exit < ChunkVisibilityMatrix.FaceCount; exit++)
            {
                matrix.CanSeeThrough(entry, exit).Should().BeTrue(
                    $"AllVisible should allow passage from face {entry} to {exit}");
            }
        }
    }

    [Fact]
    public void Opaque_CanSeeThrough_NoDirections()
    {
        ChunkVisibilityMatrix matrix = ChunkVisibilityMatrix.Opaque;

        for (int entry = 0; entry < ChunkVisibilityMatrix.FaceCount; entry++)
        {
            for (int exit = 0; exit < ChunkVisibilityMatrix.FaceCount; exit++)
            {
                matrix.CanSeeThrough(entry, exit).Should().BeFalse(
                    $"Opaque should block all passage from face {entry} to {exit}");
            }
        }
    }

    [Fact]
    public void IsFullyOpaque_WhenAllBitsClear_ReturnsTrue()
    {
        ChunkVisibilityMatrix matrix = new(0);

        matrix.IsFullyOpaque.Should().BeTrue();
    }

    [Fact]
    public void IsFullyOpaque_WhenAnyBitSet_ReturnsFalse()
    {
        ChunkVisibilityMatrix matrix = new(1);

        matrix.IsFullyOpaque.Should().BeFalse();
    }

    [Fact]
    public void IsFullyTransparent_WhenAllBitsSet_ReturnsTrue()
    {
        ChunkVisibilityMatrix matrix = ChunkVisibilityMatrix.AllVisible;

        matrix.IsFullyTransparent.Should().BeTrue();
    }

    [Fact]
    public void OppositeFace_NorthSouth_AreOpposites()
    {
        ChunkVisibilityMatrix.OppositeFace(ChunkVisibilityMatrix.FaceNorth)
            .Should().Be(ChunkVisibilityMatrix.FaceSouth);

        ChunkVisibilityMatrix.OppositeFace(ChunkVisibilityMatrix.FaceSouth)
            .Should().Be(ChunkVisibilityMatrix.FaceNorth);
    }

    [Fact]
    public void OppositeFace_EastWest_AreOpposites()
    {
        ChunkVisibilityMatrix.OppositeFace(ChunkVisibilityMatrix.FaceEast)
            .Should().Be(ChunkVisibilityMatrix.FaceWest);

        ChunkVisibilityMatrix.OppositeFace(ChunkVisibilityMatrix.FaceWest)
            .Should().Be(ChunkVisibilityMatrix.FaceEast);
    }

    [Fact]
    public void CustomBits_CanSeeThrough_ReflectsCorrectBit()
    {
        // Set only the bit for North→South passage (entry=0, exit=1)
        // Bit index = 0 * 4 + 1 = 1
        ushort bits = 1 << 1;
        ChunkVisibilityMatrix matrix = new(bits);

        matrix.CanSeeThrough(ChunkVisibilityMatrix.FaceNorth, ChunkVisibilityMatrix.FaceSouth)
            .Should().BeTrue();

        matrix.CanSeeThrough(ChunkVisibilityMatrix.FaceSouth, ChunkVisibilityMatrix.FaceNorth)
            .Should().BeFalse("only N→S is set, not S→N");

        matrix.CanSeeThrough(ChunkVisibilityMatrix.FaceEast, ChunkVisibilityMatrix.FaceWest)
            .Should().BeFalse("only N→S is set, not E→W");
    }

    [Fact]
    public void RawBits_RoundTrips()
    {
        ushort expected = 0xABCD;
        ChunkVisibilityMatrix matrix = new(expected);

        matrix.RawBits.Should().Be(expected);
    }
}
