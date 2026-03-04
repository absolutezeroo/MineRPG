using MineRPG.World.Biomes;

namespace MineRPG.World.Generation.Surface.Rules;

/// <summary>
/// Applies special ceiling blocks inside caves for specific underground biomes.
/// Used for dripstone on cave ceilings, glow lichen, moss, and roots.
/// </summary>
public sealed class CeilingRule : ISurfaceRule
{
    private readonly BiomeCategory _targetCategory;
    private readonly ushort _ceilingBlockId;

    /// <summary>
    /// Creates a ceiling rule for cave biomes.
    /// </summary>
    /// <param name="targetCategory">Biome category that triggers this rule.</param>
    /// <param name="ceilingBlockId">Block ID for the ceiling material.</param>
    public CeilingRule(BiomeCategory targetCategory, ushort ceilingBlockId)
    {
        _targetCategory = targetCategory;
        _ceilingBlockId = ceilingBlockId;
    }

    /// <inheritdoc />
    public ushort? TryApply(in SurfaceContext context)
    {
        if (!context.IsCeiling)
        {
            return null;
        }

        if (context.Biome == null || context.Biome.Category != _targetCategory)
        {
            return null;
        }

        return _ceilingBlockId;
    }
}
