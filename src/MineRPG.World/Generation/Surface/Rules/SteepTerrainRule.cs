namespace MineRPG.World.Generation.Surface.Rules;

/// <summary>
/// Exposes bare rock on steep slopes where soil cannot hold.
/// Applies when the slope gradient exceeds a configurable threshold.
/// </summary>
public sealed class SteepTerrainRule : ISurfaceRule
{
    private readonly int _slopeThreshold;
    private readonly ushort _rockBlockId;

    /// <summary>
    /// Creates a steep terrain rule.
    /// </summary>
    /// <param name="slopeThreshold">Minimum height difference to adjacent columns to trigger.</param>
    /// <param name="rockBlockId">Block ID for exposed rock.</param>
    public SteepTerrainRule(int slopeThreshold, ushort rockBlockId)
    {
        _slopeThreshold = slopeThreshold;
        _rockBlockId = rockBlockId;
    }

    /// <inheritdoc />
    public ushort? TryApply(in SurfaceContext context)
    {
        if (!context.IsSurface && context.DepthBelowSurface > 0)
        {
            return null;
        }

        if (context.SlopeGradient >= _slopeThreshold)
        {
            return _rockBlockId;
        }

        return null;
    }
}
