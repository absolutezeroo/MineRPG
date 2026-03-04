using System;

using MineRPG.World.Noise;

namespace MineRPG.World.Biomes.Climate;

/// <summary>
/// Samples the 6 climate parameters at any world position using independent noise channels.
/// Continentalness, Erosion, and Weirdness (transformed to PeaksAndValleys) are 2D (x, z).
/// Temperature and Humidity are 2D (x, z).
/// Depth is derived from the Y position relative to the surface.
/// Thread-safe: all state is readonly after construction.
/// </summary>
public sealed class ClimateSampler
{
    private const float WeirdnessToFoldMultiplier = 3f;
    private const float WeirdnessToFoldOffset = 2f;

    private readonly FractalNoiseSampler _continentalnessNoise;
    private readonly FractalNoiseSampler _erosionNoise;
    private readonly FractalNoiseSampler _weirdnessNoise;
    private readonly FractalNoiseSampler _temperatureNoise;
    private readonly FractalNoiseSampler _humidityNoise;

    /// <summary>
    /// Creates a climate sampler from the given noise configuration and world seed.
    /// </summary>
    /// <param name="config">Noise settings for all climate channels.</param>
    /// <param name="worldSeed">The world seed.</param>
    public ClimateSampler(ClimateNoiseConfig config, int worldSeed)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        _continentalnessNoise = new FractalNoiseSampler(config.Continentalness, worldSeed);
        _erosionNoise = new FractalNoiseSampler(config.Erosion, worldSeed);
        _weirdnessNoise = new FractalNoiseSampler(config.Weirdness, worldSeed);
        _temperatureNoise = new FractalNoiseSampler(config.Temperature, worldSeed);
        _humidityNoise = new FractalNoiseSampler(config.Humidity, worldSeed);
    }

    /// <summary>
    /// Samples all 5 horizontal climate parameters at a world (x, z) position.
    /// Depth is set to 0 (surface). Use <see cref="SampleFull"/> for underground biomes.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>Climate parameters with depth = 0.</returns>
    public ClimateParameters SampleSurface(int worldX, int worldZ)
    {
        float continentalness = _continentalnessNoise.Sample2D(worldX, worldZ);
        float erosion = _erosionNoise.Sample2D(worldX, worldZ);
        float weirdness = _weirdnessNoise.Sample2D(worldX, worldZ);
        float temperature = _temperatureNoise.Sample2D(worldX, worldZ);
        float humidity = _humidityNoise.Sample2D(worldX, worldZ);

        // Transform weirdness to peaks-and-valleys using the Minecraft formula:
        // PV = 1 - |3 * |weirdness| - 2|
        float peaksAndValleys = WeirdnessToPeaksAndValleys(weirdness);

        return new ClimateParameters(
            continentalness,
            erosion,
            peaksAndValleys,
            temperature,
            humidity,
            depth: 0f);
    }

    /// <summary>
    /// Samples all 6 climate parameters including depth for underground biome selection.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldY">World Y coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <param name="surfaceY">The surface height at this column.</param>
    /// <returns>Full climate parameters including depth.</returns>
    public ClimateParameters SampleFull(int worldX, int worldY, int worldZ, int surfaceY)
    {
        ClimateParameters surface = SampleSurface(worldX, worldZ);

        // Depth increases from 0 at surface to 1 at deep underground
        float depth = ComputeDepth(worldY, surfaceY);

        return new ClimateParameters(
            surface.Continentalness,
            surface.Erosion,
            surface.PeaksAndValleys,
            surface.Temperature,
            surface.Humidity,
            depth);
    }

    /// <summary>
    /// Samples only the continentalness value at a position (for fast terrain queries).
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>Raw continentalness noise value in [-1, 1].</returns>
    public float SampleContinentalness(int worldX, int worldZ)
    {
        return _continentalnessNoise.Sample2D(worldX, worldZ);
    }

    /// <summary>
    /// Transforms weirdness noise to peaks-and-valleys using the Minecraft 1.18+ formula.
    /// PV = 1 - |3 * |weirdness| - 2|
    /// This creates a folded distribution: both extreme and near-zero weirdness values
    /// produce low PV (valleys), while intermediate values produce high PV (peaks).
    /// </summary>
    /// <param name="weirdness">Raw weirdness noise value in [-1, 1].</param>
    /// <returns>Peaks and valleys value in approximately [-1, 1].</returns>
    public static float WeirdnessToPeaksAndValleys(float weirdness)
    {
        float absWeirdness = MathF.Abs(weirdness);
        float folded = WeirdnessToFoldMultiplier * absWeirdness - WeirdnessToFoldOffset;
        return 1f - MathF.Abs(folded);
    }

    private static float ComputeDepth(int worldY, int surfaceY)
    {
        if (worldY >= surfaceY)
        {
            return 0f;
        }

        // Normalize depth: 0 at surface, 1 at Y=0 or below
        float rawDepth = (float)(surfaceY - worldY) / surfaceY;
        return Math.Clamp(rawDepth, 0f, 1f);
    }
}
