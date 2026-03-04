using System;

namespace MineRPG.World.Generation.Surface.Rules;

/// <summary>
/// Replaces the surface block with snow above a temperature-modulated altitude.
/// The snow line rises with higher temperatures and lowers with colder ones.
/// </summary>
public sealed class SnowLineRule : ISurfaceRule
{
    private const float TemperatureToHeightScale = 30f;

    private readonly int _baseSnowLine;
    private readonly ushort _snowBlockId;

    /// <summary>
    /// Creates a snow line rule.
    /// </summary>
    /// <param name="baseSnowLine">Base Y level for the snow line at neutral temperature.</param>
    /// <param name="snowBlockId">Block ID for snow.</param>
    public SnowLineRule(int baseSnowLine, ushort snowBlockId)
    {
        _baseSnowLine = baseSnowLine;
        _snowBlockId = snowBlockId;
    }

    /// <inheritdoc />
    public ushort? TryApply(in SurfaceContext context)
    {
        if (!context.IsSurface || context.IsUnderwater)
        {
            return null;
        }

        // Snow line is modulated by temperature: cold = lower snow line, hot = higher
        float adjustedSnowLine = _baseSnowLine + context.Climate.Temperature * TemperatureToHeightScale;

        if (context.WorldY >= (int)adjustedSnowLine)
        {
            return _snowBlockId;
        }

        return null;
    }
}
