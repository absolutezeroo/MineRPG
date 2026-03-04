namespace MineRPG.World.Generation.Ores;

/// <summary>
/// Describes how ore spawn probability varies with height.
/// </summary>
public enum OreDistribution
{
    /// <summary>Equal probability at all heights within range.</summary>
    Uniform = 0,

    /// <summary>Peak probability at center, decreasing linearly to edges.</summary>
    Triangle = 1,

    /// <summary>Minimum probability at center, increasing toward edges.</summary>
    InvertedTriangle = 2,
}
