namespace MineRPG.World.Generation.Surface;

/// <summary>
/// A rule that determines the block to place at a surface or near-surface position.
/// Returns null if this rule does not apply, allowing the chain to continue.
/// </summary>
public interface ISurfaceRule
{
    /// <summary>
    /// Attempts to determine the block ID for the given context.
    /// </summary>
    /// <param name="context">The surface context with position, biome, and terrain data.</param>
    /// <returns>A block ID if this rule applies, or null to pass to the next rule.</returns>
    public ushort? TryApply(in SurfaceContext context);
}
