namespace MineRPG.World.Generation.Surface.Rules;

/// <summary>
/// Applies the biome's standard surface, filler, and underwater blocks.
/// This is the primary rule for most terrain columns.
/// </summary>
public sealed class BiomeSurfaceRule : ISurfaceRule
{
    /// <inheritdoc />
    public ushort? TryApply(in SurfaceContext context)
    {
        if (context.Biome == null)
        {
            return null;
        }

        // Underwater surface
        if (context.IsSurface && context.IsUnderwater)
        {
            ushort underwaterBlock = context.Biome.UnderwaterBlock;
            return underwaterBlock != 0 ? underwaterBlock : context.Biome.SubSurfaceBlock;
        }

        // Top surface block
        if (context.IsSurface)
        {
            return context.Biome.SurfaceBlock;
        }

        // Filler / sub-surface layer
        if (context.DepthBelowSurface > 0 && context.DepthBelowSurface <= context.Biome.SubSurfaceDepth)
        {
            return context.Biome.SubSurfaceBlock;
        }

        return null;
    }
}
