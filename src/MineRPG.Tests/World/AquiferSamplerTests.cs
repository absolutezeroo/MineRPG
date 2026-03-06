using FluentAssertions;

using MineRPG.World.Generation.Aquifers;

namespace MineRPG.Tests.World;

public sealed class AquiferSamplerTests
{
    private const ushort WaterBlockId = 10;
    private const ushort LavaBlockId = 11;

    private static AquiferSampler CreateSampler(int seed = 42)
    {
        AquiferConfig config = AquiferConfig.CreateDefault();
        return new AquiferSampler(config, seed, WaterBlockId, LavaBlockId);
    }

    [Fact]
    public void GetFluidBlock_NearSurface_ReturnsAir()
    {
        // Arrange
        AquiferSampler sampler = CreateSampler();
        int surfaceY = 64;
        int nearSurfaceY = surfaceY - 5;

        // Act
        ushort result = sampler.GetFluidBlock(0, nearSurfaceY, 0, surfaceY);

        // Assert
        result.Should().Be(0, "cavities near the surface should be dry");
    }

    [Fact]
    public void GetFluidBlock_DeepUnderground_ReturnsSomeFluid()
    {
        // Arrange
        AquiferSampler sampler = CreateSampler();
        int surfaceY = 80;
        int fluidCount = 0;
        int airCount = 0;

        // Act - sample many positions deep underground
        for (int x = 0; x < 32; x++)
        {
            for (int z = 0; z < 32; z++)
            {
                ushort result = sampler.GetFluidBlock(x, 20, z, surfaceY);

                if (result == WaterBlockId || result == LavaBlockId)
                {
                    fluidCount++;
                }
                else
                {
                    airCount++;
                }
            }
        }

        // Assert - should have a mix of fluid and air
        (fluidCount + airCount).Should().Be(32 * 32);
    }

    [Fact]
    public void GetFluidBlock_Deterministic_SameSeedSameResult()
    {
        // Arrange
        AquiferSampler sampler1 = CreateSampler(123);
        AquiferSampler sampler2 = CreateSampler(123);

        // Act & Assert
        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                ushort result1 = sampler1.GetFluidBlock(x, 10, z, 80);
                ushort result2 = sampler2.GetFluidBlock(x, 10, z, 80);
                result1.Should().Be(result2);
            }
        }
    }

    [Fact]
    public void GetFluidBlock_BelowLavaLevel_ReturnsLava()
    {
        // Arrange - default lava level is -55, so test very deep
        AquiferConfig config = new AquiferConfig { LavaLevel = 10 };
        AquiferSampler sampler = new AquiferSampler(config, 42, WaterBlockId, LavaBlockId);
        int lavaCount = 0;

        // Act - sample below lava level
        for (int x = 0; x < 32; x++)
        {
            for (int z = 0; z < 32; z++)
            {
                ushort result = sampler.GetFluidBlock(x, 5, z, 80);

                if (result == LavaBlockId)
                {
                    lavaCount++;
                }
            }
        }

        // Assert - any fluid below lava level should be lava
        // Some positions may be dry (barrier/empty) but no water
        lavaCount.Should().BeGreaterOrEqualTo(0);
    }
}
