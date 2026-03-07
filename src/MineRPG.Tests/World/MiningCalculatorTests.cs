using FluentAssertions;

using MineRPG.World.Blocks;
using MineRPG.World.Mining;

namespace MineRPG.Tests.World;

public sealed class MiningCalculatorTests
{
    private static BlockDefinition CreateBlock(
        float hardness = 5f,
        string? requiredToolType = null,
        int requiredHarvestLevel = 0)
    {
        return new BlockDefinition
        {
            Id = "minerpg:test_block",
            DisplayName = "TestBlock",
            Flags = BlockFlags.Solid,
            Hardness = hardness,
            RequiredToolType = requiredToolType,
            RequiredHarvestLevel = requiredHarvestLevel,
        };
    }

    // ── ComputeMineTime ─────────────────────────────────────────────

    [Fact]
    public void ComputeMineTime_IndestructibleBlock_ReturnsMaxValue()
    {
        BlockDefinition block = CreateBlock(hardness: -1f);

        float result = MiningCalculator.ComputeMineTime(block, "", 0, 1f, 1f);

        result.Should().Be(float.MaxValue);
    }

    [Fact]
    public void ComputeMineTime_BareHandsNoRequirement_ReturnsBaseTime()
    {
        // hardness 2.0, no required tool -> baseTime = 2.0 * 1.5 = 3.0
        BlockDefinition block = CreateBlock(hardness: 2f);

        float result = MiningCalculator.ComputeMineTime(block, "", 0, 1f, 1f);

        result.Should().BeApproximately(3f, 0.001f);
    }

    [Fact]
    public void ComputeMineTime_BareHandsOnSoftBlock_NoPenalty()
    {
        // Dirt-like block: preferred tool is shovel but minTier = 0
        // Bare hands should mine at base speed without penalty
        // baseTime = 1.0 * 1.5 = 1.5, no penalty, no speed bonus
        BlockDefinition block = CreateBlock(hardness: 1f, requiredToolType: "shovel", requiredHarvestLevel: 0);

        float result = MiningCalculator.ComputeMineTime(block, "", 0, 1f, 1f);

        result.Should().BeApproximately(1.5f, 0.001f);
    }

    [Fact]
    public void ComputeMineTime_WrongToolOnSoftBlock_NoPenalty()
    {
        // Dirt-like block with pickaxe: no penalty (minTier = 0),
        // but no speed bonus either (pickaxe != shovel)
        // baseTime = 1.0 * 1.5 = 1.5
        BlockDefinition block = CreateBlock(hardness: 1f, requiredToolType: "shovel", requiredHarvestLevel: 0);

        float result = MiningCalculator.ComputeMineTime(block, "pickaxe", 2, 4f, 1f);

        result.Should().BeApproximately(1.5f, 0.001f);
    }

    [Fact]
    public void ComputeMineTime_CorrectToolOnSoftBlock_GivesSpeedBonus()
    {
        // Dirt-like block with shovel (speed 2.0): no penalty, speed bonus applies
        // baseTime = 1.0 * 1.5 = 1.5, final = 1.5 / 2.0 = 0.75
        BlockDefinition block = CreateBlock(hardness: 1f, requiredToolType: "shovel", requiredHarvestLevel: 0);

        float result = MiningCalculator.ComputeMineTime(block, "shovel", 1, 2f, 1f);

        result.Should().BeApproximately(0.75f, 0.001f);
    }

    [Fact]
    public void ComputeMineTime_WrongToolType_AppliesPenalty()
    {
        // Stone-like block (minTier=1): requires pickaxe, using axe -> penalty
        // baseTime = 2.0 * 1.5 * 3.33 = 9.99, no speed bonus (wrong type)
        BlockDefinition block = CreateBlock(hardness: 2f, requiredToolType: "pickaxe", requiredHarvestLevel: 1);

        float result = MiningCalculator.ComputeMineTime(block, "axe", 1, 1f, 1f);

        result.Should().BeApproximately(2f * 1.5f * 3.33f, 0.01f);
    }

    [Fact]
    public void ComputeMineTime_BareHandsOnHardBlock_AppliesPenalty()
    {
        // Stone-like block (minTier=1): bare hands -> penalty
        // baseTime = 2.0 * 1.5 * 3.33 = 9.99
        BlockDefinition block = CreateBlock(hardness: 2f, requiredToolType: "pickaxe", requiredHarvestLevel: 1);

        float result = MiningCalculator.ComputeMineTime(block, "", 0, 1f, 1f);

        result.Should().BeApproximately(2f * 1.5f * 3.33f, 0.01f);
    }

    [Fact]
    public void ComputeMineTime_CorrectToolCorrectTier_NoMalus()
    {
        // hardness 5.0, requires pickaxe tier 1, using pickaxe tier 2 with speed 4.0
        // baseTime = 5.0 * 1.5 = 7.5, final = 7.5 / 4.0 = 1.875
        BlockDefinition block = CreateBlock(hardness: 5f, requiredToolType: "pickaxe", requiredHarvestLevel: 1);

        float result = MiningCalculator.ComputeMineTime(block, "pickaxe", 2, 4f, 1f);

        result.Should().BeApproximately(1.875f, 0.001f);
    }

    [Fact]
    public void ComputeMineTime_MatchingTypeTierTooLow_SpeedBonusNoPenalty()
    {
        // Matching tool type but tier too low: no penalty (type matches),
        // speed bonus still applies. No drops (IsCorrectTool = false).
        // hardness 3.0, pickaxe tier 1 on minTier 3 block, speed 2.0
        // baseTime = 3.0 * 1.5 = 4.5, final = 4.5 / 2.0 = 2.25
        BlockDefinition block = CreateBlock(hardness: 3f, requiredToolType: "pickaxe", requiredHarvestLevel: 3);

        float result = MiningCalculator.ComputeMineTime(block, "pickaxe", 1, 2f, 1f);

        result.Should().BeApproximately(2.25f, 0.001f);
    }

    [Fact]
    public void ComputeMineTime_PlayerModifierScalesTime()
    {
        // hardness 2.0, no tool requirement, player modifier 2.0
        // baseTime = 3.0, final = 3.0 / (1.0 * 2.0) = 1.5
        BlockDefinition block = CreateBlock(hardness: 2f);

        float result = MiningCalculator.ComputeMineTime(block, "", 0, 1f, 2f);

        result.Should().BeApproximately(1.5f, 0.001f);
    }

    [Fact]
    public void ComputeMineTime_VeryFastCombination_ClampsToMinimum()
    {
        // hardness 0.01, matching tool with speed 100 -> would be < 0.05
        BlockDefinition block = CreateBlock(hardness: 0.01f, requiredToolType: "pickaxe");

        float result = MiningCalculator.ComputeMineTime(block, "pickaxe", 1, 100f, 1f);

        result.Should().Be(0.05f);
    }

    [Fact]
    public void ComputeMineTime_ZeroSpeedMultiplier_FallsBackToOne()
    {
        BlockDefinition block = CreateBlock(hardness: 2f);

        float result = MiningCalculator.ComputeMineTime(block, "", 0, 0f, 1f);

        result.Should().BeApproximately(3f, 0.001f);
    }

    // ── IsCorrectTool ───────────────────────────────────────────────

    [Fact]
    public void IsCorrectTool_NoRequirement_ReturnsTrue()
    {
        BlockDefinition block = CreateBlock();

        bool result = MiningCalculator.IsCorrectTool(block, "", 0);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsCorrectTool_PreferredToolNoMinTier_AlwaysTrue()
    {
        // Dirt-like: has preferred tool but minTier = 0 -> always true for drops
        BlockDefinition block = CreateBlock(requiredToolType: "shovel", requiredHarvestLevel: 0);

        bool bareHands = MiningCalculator.IsCorrectTool(block, "", 0);
        bool wrongTool = MiningCalculator.IsCorrectTool(block, "pickaxe", 3);
        bool correctTool = MiningCalculator.IsCorrectTool(block, "shovel", 1);

        bareHands.Should().BeTrue();
        wrongTool.Should().BeTrue();
        correctTool.Should().BeTrue();
    }

    [Fact]
    public void IsCorrectTool_CorrectTypeAndTier_ReturnsTrue()
    {
        BlockDefinition block = CreateBlock(requiredToolType: "pickaxe", requiredHarvestLevel: 2);

        bool result = MiningCalculator.IsCorrectTool(block, "pickaxe", 3);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsCorrectTool_WrongType_ReturnsFalse()
    {
        BlockDefinition block = CreateBlock(requiredToolType: "pickaxe", requiredHarvestLevel: 1);

        bool result = MiningCalculator.IsCorrectTool(block, "axe", 4);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsCorrectTool_TierTooLow_ReturnsFalse()
    {
        BlockDefinition block = CreateBlock(requiredToolType: "pickaxe", requiredHarvestLevel: 3);

        bool result = MiningCalculator.IsCorrectTool(block, "pickaxe", 2);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsCorrectTool_CaseInsensitive_ReturnsTrue()
    {
        BlockDefinition block = CreateBlock(requiredToolType: "Pickaxe", requiredHarvestLevel: 1);

        bool result = MiningCalculator.IsCorrectTool(block, "pickaxe", 1);

        result.Should().BeTrue();
    }

    // ── HasMatchingToolType ─────────────────────────────────────────

    [Fact]
    public void HasMatchingToolType_NoPreferredTool_ReturnsFalse()
    {
        BlockDefinition block = CreateBlock();

        bool result = MiningCalculator.HasMatchingToolType(block, "pickaxe");

        result.Should().BeFalse();
    }

    [Fact]
    public void HasMatchingToolType_MatchingType_ReturnsTrue()
    {
        BlockDefinition block = CreateBlock(requiredToolType: "pickaxe");

        bool result = MiningCalculator.HasMatchingToolType(block, "pickaxe");

        result.Should().BeTrue();
    }

    [Fact]
    public void HasMatchingToolType_WrongType_ReturnsFalse()
    {
        BlockDefinition block = CreateBlock(requiredToolType: "pickaxe");

        bool result = MiningCalculator.HasMatchingToolType(block, "axe");

        result.Should().BeFalse();
    }

    [Fact]
    public void HasMatchingToolType_BareHands_ReturnsFalse()
    {
        BlockDefinition block = CreateBlock(requiredToolType: "shovel");

        bool result = MiningCalculator.HasMatchingToolType(block, "");

        result.Should().BeFalse();
    }
}
