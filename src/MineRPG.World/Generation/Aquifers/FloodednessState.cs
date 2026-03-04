namespace MineRPG.World.Generation.Aquifers;

/// <summary>
/// Describes the flooding state of an underground cavity.
/// </summary>
public enum FloodednessState
{
    /// <summary>Aquifer system does not apply (e.g., above surface).</summary>
    Disabled = 0,

    /// <summary>Cavity is flooded with water or lava.</summary>
    Flooded = 1,

    /// <summary>Cavity is dry (air).</summary>
    Empty = 2,

    /// <summary>Flooding is determined by noise at this position.</summary>
    Randomized = 3,
}
