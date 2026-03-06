using System;

using MineRPG.World.Blocks;

namespace MineRPG.World.Mining;

/// <summary>
/// Pure stateless calculator for block mining time.
/// Hybrid formula: Minecraft-style base (hardness * multiplier, wrong-tool penalty)
/// with an RPG stat modifier that scales the final time.
/// </summary>
public static class MiningCalculator
{
    /// <summary>Seconds of mining per unit of block hardness.</summary>
    private const float BaseTimePerHardnessUnit = 1.5f;

    /// <summary>Penalty multiplier when the wrong tool type or insufficient tier is used.</summary>
    private const float WrongToolPenalty = 3.33f;

    /// <summary>Minimum mining time in seconds. Prevents instant breaking.</summary>
    private const float MinMineTime = 0.05f;

    /// <summary>
    /// Computes the total mining time in seconds for the given block and tool parameters.
    /// Returns <see cref="float.MaxValue"/> for indestructible blocks (Hardness &lt; 0).
    /// Follows Minecraft-style mechanics: bare hands can break any non-indestructible block.
    /// The wrong-tool penalty only applies to blocks that require a tool (RequiredHarvestLevel &gt; 0)
    /// when the player lacks the correct tool type. Tool speed bonus only applies when
    /// the equipped tool type matches the block's preferred type.
    /// </summary>
    /// <param name="block">The block definition to mine.</param>
    /// <param name="equippedToolType">The type string of the equipped tool, or empty for bare hands.</param>
    /// <param name="equippedToolTier">The tier of the equipped tool (0 = bare hand).</param>
    /// <param name="toolSpeedMultiplier">Speed multiplier from the tool definition (1.0 = bare hands).</param>
    /// <param name="playerMiningSpeedModifier">RPG stat modifier (1.0 = no bonus).</param>
    /// <returns>Time in seconds to break the block.</returns>
    public static float ComputeMineTime(
        BlockDefinition block,
        string equippedToolType,
        int equippedToolTier,
        float toolSpeedMultiplier,
        float playerMiningSpeedModifier)
    {
        if (block.Hardness < 0f)
        {
            return float.MaxValue;
        }

        float baseTime = block.Hardness * BaseTimePerHardnessUnit;

        bool hasMatchingToolType = HasMatchingToolType(block, equippedToolType);

        // Wrong-tool penalty: only when the block requires a specific tool
        // (RequiredHarvestLevel > 0) and the player does not have the matching type.
        // Blocks with RequiredHarvestLevel == 0 (dirt, wood, sand) can always be
        // mined without penalty - the preferred tool just adds a speed bonus.
        if (block.RequiredHarvestLevel > 0 && !hasMatchingToolType)
        {
            baseTime *= WrongToolPenalty;
        }

        // Tool speed bonus only applies when the tool type matches the
        // block's preferred type (e.g., shovel on dirt, pickaxe on stone).
        float effectiveToolSpeed = hasMatchingToolType ? toolSpeedMultiplier : 1f;

        float combinedMultiplier = effectiveToolSpeed * playerMiningSpeedModifier;

        if (combinedMultiplier <= 0f)
        {
            combinedMultiplier = 1f;
        }

        float finalTime = baseTime / combinedMultiplier;

        if (finalTime < MinMineTime)
        {
            return MinMineTime;
        }

        return finalTime;
    }

    /// <summary>
    /// Returns true if the equipped tool satisfies the block's requirements for
    /// efficient mining and item drops. Blocks with no RequiredToolType or with
    /// RequiredHarvestLevel of 0 always return true (bare hands are sufficient for drops).
    /// </summary>
    /// <param name="block">The block definition to check.</param>
    /// <param name="equippedToolType">The type string of the equipped tool.</param>
    /// <param name="equippedToolTier">The tier of the equipped tool.</param>
    /// <returns>True if the tool is correct for this block (drops will be produced).</returns>
    public static bool IsCorrectTool(
        BlockDefinition block,
        string equippedToolType,
        int equippedToolTier)
    {
        if (string.IsNullOrEmpty(block.RequiredToolType))
        {
            return true;
        }

        // Blocks that don't require a minimum tool tier can be harvested
        // with anything - bare hands, wrong tool, etc.
        if (block.RequiredHarvestLevel <= 0)
        {
            return true;
        }

        return string.Equals(equippedToolType, block.RequiredToolType, StringComparison.OrdinalIgnoreCase)
            && equippedToolTier >= block.RequiredHarvestLevel;
    }

    /// <summary>
    /// Returns true if the equipped tool type matches the block's preferred tool type.
    /// Used to determine whether the tool's speed bonus applies.
    /// </summary>
    /// <param name="block">The block definition to check.</param>
    /// <param name="equippedToolType">The type string of the equipped tool.</param>
    /// <returns>True if the tool type matches the block's preferred type.</returns>
    public static bool HasMatchingToolType(
        BlockDefinition block,
        string equippedToolType)
    {
        if (string.IsNullOrEmpty(block.RequiredToolType))
        {
            return false;
        }

        return string.Equals(equippedToolType, block.RequiredToolType, StringComparison.OrdinalIgnoreCase);
    }
}
