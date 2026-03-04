using System.Runtime.CompilerServices;

using Newtonsoft.Json;

namespace MineRPG.World.Biomes.Climate;

/// <summary>
/// Defines the 6-dimensional climate target for a biome.
/// Each parameter has a min/max range. The biome selector picks the biome
/// whose target is closest (in Euclidean distance) to the sampled climate.
/// </summary>
public readonly struct BiomeClimateTarget
{
    /// <summary>Continentalness range: ocean (-1) to inland (1).</summary>
    [JsonProperty("continentalness")]
    public ParameterRange Continentalness { get; init; }

    /// <summary>Erosion range: rugged (-1) to flat (1).</summary>
    [JsonProperty("erosion")]
    public ParameterRange Erosion { get; init; }

    /// <summary>Peaks and valleys range: valley (-1) to peak (1).</summary>
    [JsonProperty("peaks_and_valleys")]
    public ParameterRange PeaksAndValleys { get; init; }

    /// <summary>Temperature range: frozen (-1) to scorching (1).</summary>
    [JsonProperty("temperature")]
    public ParameterRange Temperature { get; init; }

    /// <summary>Humidity range: arid (-1) to tropical (1).</summary>
    [JsonProperty("humidity")]
    public ParameterRange Humidity { get; init; }

    /// <summary>Depth range: surface (0) to deep underground (1).</summary>
    [JsonProperty("depth")]
    public ParameterRange Depth { get; init; }

    /// <summary>
    /// Computes the squared Euclidean distance from the given climate parameters
    /// to this target in 6D space. Uses range distance (0 if inside the range).
    /// </summary>
    /// <param name="parameters">The sampled climate parameters.</param>
    /// <returns>The squared distance in 6D climate space.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float SquaredDistanceTo(in ClimateParameters parameters)
    {
        float dc = Continentalness.DistanceTo(parameters.Continentalness);
        float de = Erosion.DistanceTo(parameters.Erosion);
        float dp = PeaksAndValleys.DistanceTo(parameters.PeaksAndValleys);
        float dt = Temperature.DistanceTo(parameters.Temperature);
        float dh = Humidity.DistanceTo(parameters.Humidity);
        float dd = Depth.DistanceTo(parameters.Depth);

        return dc * dc + de * de + dp * dp + dt * dt + dh * dh + dd * dd;
    }
}
