namespace MineRPG.World.Biomes.Climate;

/// <summary>
/// The 6 climate parameters sampled at a world position.
/// Used for biome selection and terrain generation.
/// Three parameters (Continentalness, Erosion, PeaksAndValleys) drive terrain shape.
/// Two parameters (Temperature, Humidity) drive biome climate.
/// One parameter (Depth) drives underground biome selection.
/// </summary>
public readonly struct ClimateParameters
{
    /// <summary>Ocean to inland axis. Negative = ocean, positive = continent interior.</summary>
    public float Continentalness { get; init; }

    /// <summary>Terrain roughness. Low = rugged mountains, high = flat plains.</summary>
    public float Erosion { get; init; }

    /// <summary>Peaks and valleys. Derived from weirdness noise. High = peaks, low = valleys.</summary>
    public float PeaksAndValleys { get; init; }

    /// <summary>Temperature axis. Low = cold/frozen, high = hot/desert.</summary>
    public float Temperature { get; init; }

    /// <summary>Humidity axis. Low = arid, high = tropical/jungle.</summary>
    public float Humidity { get; init; }

    /// <summary>Vertical depth. 0 = surface, 1 = deep underground.</summary>
    public float Depth { get; init; }

    /// <summary>
    /// Creates climate parameters with all six values.
    /// </summary>
    /// <param name="continentalness">Continentalness value in [-1, 1].</param>
    /// <param name="erosion">Erosion value in [-1, 1].</param>
    /// <param name="peaksAndValleys">Peaks and valleys value in [-1, 1].</param>
    /// <param name="temperature">Temperature value in [-1, 1].</param>
    /// <param name="humidity">Humidity value in [-1, 1].</param>
    /// <param name="depth">Depth value in [0, 1].</param>
    public ClimateParameters(
        float continentalness,
        float erosion,
        float peaksAndValleys,
        float temperature,
        float humidity,
        float depth)
    {
        Continentalness = continentalness;
        Erosion = erosion;
        PeaksAndValleys = peaksAndValleys;
        Temperature = temperature;
        Humidity = humidity;
        Depth = depth;
    }
}
