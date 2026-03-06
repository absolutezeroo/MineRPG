using FluentAssertions;

using MineRPG.World.Meshing;

namespace MineRPG.Tests.World;

public sealed class LodPolicyTests
{
    [Theory]
    [InlineData(0, LodLevel.Lod0)]
    [InlineData(8, LodLevel.Lod0)]
    [InlineData(15, LodLevel.Lod0)]
    [InlineData(16, LodLevel.Lod0)]
    [InlineData(17, LodLevel.Lod1)]
    [InlineData(25, LodLevel.Lod1)]
    [InlineData(32, LodLevel.Lod1)]
    [InlineData(33, LodLevel.Lod2)]
    [InlineData(64, LodLevel.Lod2)]
    public void GetInitialLod_ReturnsExpectedLevel(int distance, LodLevel expected)
    {
        LodLevel result = LodPolicy.GetInitialLod(distance);

        result.Should().Be(expected);
    }

    [Fact]
    public void GetLodWithHysteresis_Lod0_StaysAtLod0BelowThreshold()
    {
        LodLevel result = LodPolicy.GetLodWithHysteresis(16, LodLevel.Lod0);

        result.Should().Be(LodLevel.Lod0, "distance 16 < Lod0ToLod1Distance (17)");
    }

    [Fact]
    public void GetLodWithHysteresis_Lod0_SwitchesToLod1AtThreshold()
    {
        LodLevel result = LodPolicy.GetLodWithHysteresis(17, LodLevel.Lod0);

        result.Should().Be(LodLevel.Lod1);
    }

    [Fact]
    public void GetLodWithHysteresis_Lod1_StaysAtLod1InHysteresisRange()
    {
        // Between Lod1ToLod0Distance(15) and Lod0ToLod1Distance(17) — hysteresis zone
        LodLevel result = LodPolicy.GetLodWithHysteresis(16, LodLevel.Lod1);

        result.Should().Be(LodLevel.Lod1, "distance 16 is in the hysteresis zone");
    }

    [Fact]
    public void GetLodWithHysteresis_Lod1_DropsToLod0BelowThreshold()
    {
        LodLevel result = LodPolicy.GetLodWithHysteresis(14, LodLevel.Lod1);

        result.Should().Be(LodLevel.Lod0);
    }

    [Fact]
    public void GetLodWithHysteresis_Lod2_StaysAtLod2InHysteresisRange()
    {
        LodLevel result = LodPolicy.GetLodWithHysteresis(32, LodLevel.Lod2);

        result.Should().Be(LodLevel.Lod2, "distance 32 is in hysteresis zone for LOD2");
    }

    [Fact]
    public void GetLodWithHysteresis_Lod2_DropsToLod1BelowThreshold()
    {
        LodLevel result = LodPolicy.GetLodWithHysteresis(30, LodLevel.Lod2);

        result.Should().Be(LodLevel.Lod1);
    }

    [Theory]
    [InlineData(LodLevel.Lod0, 1)]
    [InlineData(LodLevel.Lod1, 2)]
    [InlineData(LodLevel.Lod2, 4)]
    public void GetDownsampleFactor_ReturnsCorrectFactor(LodLevel lod, int expectedFactor)
    {
        int factor = LodPolicy.GetDownsampleFactor(lod);

        factor.Should().Be(expectedFactor);
    }
}
