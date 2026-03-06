namespace MineRPG.Game.Bootstrap.Gameplay;

/// <summary>
/// Lightweight DTO describing the currently equipped tool for mining calculations.
/// Decouples <see cref="BlockInteractionService"/> from the RPG item system.
/// </summary>
public sealed class ToolDefinition
{
    /// <summary>The tool type string (e.g. "pickaxe", "axe", "shovel").</summary>
    public string ToolType { get; init; } = "";

    /// <summary>The tool tier determining which blocks can be harvested.</summary>
    public int ToolTier { get; init; }

    /// <summary>Mining speed multiplier provided by this tool.</summary>
    public float SpeedMultiplier { get; init; } = 1f;
}
