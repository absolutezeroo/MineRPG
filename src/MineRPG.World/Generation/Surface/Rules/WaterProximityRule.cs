using System;

namespace MineRPG.World.Generation.Surface.Rules;

/// <summary>
/// Places sand or gravel near water bodies (within a horizontal distance of
/// the shoreline or near sea-level surfaces).
/// </summary>
public sealed class WaterProximityRule : ISurfaceRule
{
    private const int ShorelineMaxElevation = 3;

    private readonly ushort _shoreBlockId;
    private readonly int _seaLevel;

    /// <summary>
    /// Creates a water proximity rule.
    /// </summary>
    /// <param name="shoreBlockId">Block ID for shore material (sand, gravel).</param>
    /// <param name="seaLevel">Sea level for proximity calculation.</param>
    public WaterProximityRule(ushort shoreBlockId, int seaLevel)
    {
        _shoreBlockId = shoreBlockId;
        _seaLevel = seaLevel;
    }

    /// <inheritdoc />
    public ushort? TryApply(in SurfaceContext context)
    {
        if (!context.IsSurface)
        {
            return null;
        }

        // Apply to surfaces just above or at sea level
        int elevationAboveSea = context.SurfaceY - _seaLevel;

        if (elevationAboveSea >= 0 && elevationAboveSea <= ShorelineMaxElevation)
        {
            return _shoreBlockId;
        }

        return null;
    }
}
