using FluentAssertions;

using MineRPG.World.Biomes;
using MineRPG.World.Biomes.Climate;
using MineRPG.World.Generation;
using MineRPG.World.Generation.Surface;
using MineRPG.World.Generation.Surface.Rules;

namespace MineRPG.Tests.World;

public sealed class SurfaceRuleTests
{
    private static BiomeDefinition CreateTestBiome()
    {
        return new BiomeDefinition
        {
            Id = "plains",
            BiomeType = BiomeType.Plains,
            SurfaceBlock = 3,
            SubSurfaceBlock = 2,
            StoneBlock = 1,
            SubSurfaceDepth = 4,
        };
    }

    [Fact]
    public void BiomeSurfaceRule_AtSurface_ReturnsSurfaceBlock()
    {
        // Arrange
        BiomeSurfaceRule rule = new BiomeSurfaceRule();
        BiomeDefinition biome = CreateTestBiome();
        SurfaceContext context = new SurfaceContext
        {
            IsSurface = true,
            Biome = biome,
            DepthBelowSurface = 0,
            SurfaceY = 64,
            WorldY = 64,
            SeaLevel = 62,
        };

        // Act
        ushort? result = rule.TryApply(in context);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public void BiomeSurfaceRule_BelowSurface_ReturnsSubSurfaceBlock()
    {
        // Arrange
        BiomeSurfaceRule rule = new BiomeSurfaceRule();
        BiomeDefinition biome = CreateTestBiome();
        SurfaceContext context = new SurfaceContext
        {
            IsSurface = false,
            Biome = biome,
            DepthBelowSurface = 2,
            SurfaceY = 64,
            WorldY = 62,
            SeaLevel = 62,
        };

        // Act
        ushort? result = rule.TryApply(in context);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void DepthBasedRule_BelowThreshold_ReturnsDeepBlock()
    {
        // Arrange
        ushort deepslateId = 50;
        ushort stoneId = 1;
        DepthBasedRule rule = new DepthBasedRule(10, deepslateId, stoneId);
        SurfaceContext context = new SurfaceContext
        {
            WorldY = 5,
            DepthBelowSurface = 59,
        };

        // Act
        ushort? result = rule.TryApply(in context);

        // Assert
        result.Should().Be(deepslateId);
    }

    [Fact]
    public void DepthBasedRule_AboveThreshold_ReturnsNull()
    {
        // Arrange
        DepthBasedRule rule = new DepthBasedRule(10, 50, 1);
        SurfaceContext context = new SurfaceContext
        {
            WorldY = 15,
            DepthBelowSurface = 49,
        };

        // Act
        ushort? result = rule.TryApply(in context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void SnowLineRule_AboveSnowLine_ReturnsSnowBlock()
    {
        // Arrange
        ushort snowBlockId = 80;
        SnowLineRule rule = new SnowLineRule(100, snowBlockId);
        SurfaceContext context = new SurfaceContext
        {
            IsSurface = true,
            WorldY = 120,
            Climate = new ClimateParameters(0f, 0f, 0f, -0.5f, 0f, 0f),
        };

        // Act
        ushort? result = rule.TryApply(in context);

        // Assert
        result.Should().Be(snowBlockId);
    }

    [Fact]
    public void SurfaceRuleChain_FirstMatchWins()
    {
        // Arrange
        ushort stoneId = 1;
        DepthBasedRule deepRule = new DepthBasedRule(10, 50, stoneId);
        BiomeSurfaceRule biomeRule = new BiomeSurfaceRule();
        SurfaceRuleChain chain = new SurfaceRuleChain(
            new ISurfaceRule[] { deepRule, biomeRule }, stoneId);

        BiomeDefinition biome = CreateTestBiome();
        SurfaceContext context = new SurfaceContext
        {
            WorldY = 5,
            DepthBelowSurface = 59,
            IsSurface = false,
            Biome = biome,
        };

        // Act
        ushort result = chain.Evaluate(in context);

        // Assert - deep rule should fire first
        result.Should().Be(50);
    }

    [Fact]
    public void SurfaceRuleChain_NoMatch_ReturnsDefault()
    {
        // Arrange
        ushort stoneId = 1;
        SurfaceRuleChain chain = new SurfaceRuleChain(
            new ISurfaceRule[] { new SnowLineRule(200, 80) },
            stoneId);

        SurfaceContext context = new SurfaceContext
        {
            WorldY = 50,
            IsSurface = false,
            Climate = new ClimateParameters(0f, 0f, 0f, 0.5f, 0f, 0f),
        };

        // Act
        ushort result = chain.Evaluate(in context);

        // Assert
        result.Should().Be(stoneId);
    }

    [Fact]
    public void CeilingRule_OnCeiling_WithMatchingCategory_ReturnsBlock()
    {
        // Arrange
        ushort ceilingBlockId = 60;
        CeilingRule rule = new CeilingRule(BiomeCategory.Cave, ceilingBlockId);
        BiomeDefinition biome = new BiomeDefinition
        {
            Id = "dripstone",
            BiomeType = BiomeType.Plains,
            Category = BiomeCategory.Cave,
        };
        SurfaceContext context = new SurfaceContext
        {
            IsCeiling = true,
            Biome = biome,
        };

        // Act
        ushort? result = rule.TryApply(in context);

        // Assert
        result.Should().Be(ceilingBlockId);
    }

    [Fact]
    public void CeilingRule_NotOnCeiling_ReturnsNull()
    {
        // Arrange
        CeilingRule rule = new CeilingRule(BiomeCategory.Cave, 60);
        SurfaceContext context = new SurfaceContext
        {
            IsCeiling = false,
        };

        // Act
        ushort? result = rule.TryApply(in context);

        // Assert
        result.Should().BeNull();
    }
}
