namespace MineRPG.World.Generation.Surface.Rules;

/// <summary>
/// Replaces stone with an alternate block below a certain Y level.
/// Used for deepslate below Y=0 or similar depth-dependent substitutions.
/// </summary>
public sealed class DepthBasedRule : ISurfaceRule
{
    private readonly int _yThreshold;
    private readonly ushort _deepBlockId;
    private readonly ushort _stoneBlockId;

    /// <summary>
    /// Creates a depth-based rule.
    /// </summary>
    /// <param name="yThreshold">Y level at or below which the deep block is used.</param>
    /// <param name="deepBlockId">Block ID for the deep layer (e.g., deepslate).</param>
    /// <param name="stoneBlockId">Block ID for normal stone (only replaces this block).</param>
    public DepthBasedRule(int yThreshold, ushort deepBlockId, ushort stoneBlockId)
    {
        _yThreshold = yThreshold;
        _deepBlockId = deepBlockId;
        _stoneBlockId = stoneBlockId;
    }

    /// <inheritdoc />
    public ushort? TryApply(in SurfaceContext context)
    {
        // Only applies underground, below the filler layer
        if (context.IsSurface || context.DepthBelowSurface <= 0)
        {
            return null;
        }

        if (context.WorldY <= _yThreshold)
        {
            return _deepBlockId;
        }

        return null;
    }
}
