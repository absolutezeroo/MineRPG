namespace MineRPG.World.Biomes;

/// <summary>
/// Classification of biomes by their geographical role.
/// Used for terrain generation parameter selection and biome grouping.
/// </summary>
public enum BiomeCategory
{
    /// <summary>Deep ocean and ocean biomes (low continentalness).</summary>
    Ocean,

    /// <summary>Coastal biomes: beaches, shores (transition zone).</summary>
    Coast,

    /// <summary>Inland biomes at moderate elevation: plains, forests, deserts.</summary>
    Middle,

    /// <summary>High-elevation plateaus and mesas.</summary>
    Plateau,

    /// <summary>Mountain peaks and slopes.</summary>
    Peak,

    /// <summary>Underground biomes (high depth parameter).</summary>
    Cave,

    /// <summary>River and waterway biomes.</summary>
    River,

    /// <summary>Special biomes with unique generation rules.</summary>
    Special,
}
