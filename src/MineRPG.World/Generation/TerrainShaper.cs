using System;
using System.Runtime.CompilerServices;

using MineRPG.World.Biomes;
using MineRPG.World.Biomes.Climate;
using MineRPG.World.Chunks;

namespace MineRPG.World.Generation;

/// <summary>
/// Converts climate parameters into terrain height using spline curves.
/// The three terrain parameters (Continentalness, Erosion, PeaksAndValleys)
/// are mapped through configurable splines and combined to produce a final height.
/// Thread-safe: all state is readonly after construction.
/// </summary>
public sealed class TerrainShaper : ITerrainHeightProvider
{
    private const int SeaLevel = 62;
    private const int MinClampY = 1;

    private readonly HeightSpline _continentalnessSpline;
    private readonly HeightSpline _erosionSpline;
    private readonly HeightSpline _peaksValleysSpline;

    /// <summary>
    /// Creates a terrain shaper with the given spline curves.
    /// </summary>
    /// <param name="continentalnessSpline">Maps continentalness to base height offset.</param>
    /// <param name="erosionSpline">Maps erosion to relief scale factor.</param>
    /// <param name="peaksValleysSpline">Maps peaks/valleys to height offset.</param>
    public TerrainShaper(
        HeightSpline continentalnessSpline,
        HeightSpline erosionSpline,
        HeightSpline peaksValleysSpline)
    {
        _continentalnessSpline = continentalnessSpline
            ?? throw new ArgumentNullException(nameof(continentalnessSpline));
        _erosionSpline = erosionSpline
            ?? throw new ArgumentNullException(nameof(erosionSpline));
        _peaksValleysSpline = peaksValleysSpline
            ?? throw new ArgumentNullException(nameof(peaksValleysSpline));
    }

    /// <summary>
    /// Creates a terrain shaper with default spline curves matching Minecraft 1.18+ terrain.
    /// </summary>
    /// <returns>A terrain shaper with default splines.</returns>
    public static TerrainShaper CreateDefault()
    {
        // Continentalness: determines ocean depth and base land elevation.
        // Inland areas get only a small height boost (0-15 blocks above sea level).
        // Mountain heights come from PeaksAndValleys × erosionFactor, not from this.
        HeightSpline continentalness = new(
        [
            new SplinePoint(-1.0f, -60f),
            new SplinePoint(-0.55f, -35f),
            new SplinePoint(-0.3f, -12f),
            new SplinePoint(-0.2f, -4f),
            new SplinePoint(-0.12f, 0f),
            new SplinePoint(-0.05f, 2f),
            new SplinePoint(0.05f, 4f),
            new SplinePoint(0.2f, 6f),
            new SplinePoint(0.4f, 8f),
            new SplinePoint(0.65f, 10f),
            new SplinePoint(0.85f, 12f),
            new SplinePoint(1.0f, 15f),
        ]);

        // Erosion: scales the PeaksAndValleys contribution.
        // High erosion (plains, deserts) nearly zeroes out PV → flat terrain.
        // Low erosion (mountains) amplifies PV → dramatic height variation.
        HeightSpline erosion = new(
        [
            new SplinePoint(-1.0f, 2.0f),
            new SplinePoint(-0.5f, 1.5f),
            new SplinePoint(-0.2f, 1.0f),
            new SplinePoint(0.0f, 0.6f),
            new SplinePoint(0.3f, 0.3f),
            new SplinePoint(0.5f, 0.15f),
            new SplinePoint(0.7f, 0.08f),
            new SplinePoint(1.0f, 0.04f),
        ]);

        // PeaksAndValleys: local height variation scaled by erosion factor.
        // At low erosion (mountains): full range applied → -20 to +60 blocks.
        // At high erosion (plains): nearly zeroed → ±1-2 blocks.
        HeightSpline peaksValleys = new(
        [
            new SplinePoint(-1.0f, -20f),
            new SplinePoint(-0.5f, -8f),
            new SplinePoint(0.0f, 0f),
            new SplinePoint(0.5f, 20f),
            new SplinePoint(1.0f, 60f),
        ]);

        return new TerrainShaper(continentalness, erosion, peaksValleys);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetHeight(in ClimateParameters parameters)
    {
        float continentalHeight = _continentalnessSpline.Evaluate(parameters.Continentalness);
        float erosionFactor = _erosionSpline.Evaluate(parameters.Erosion);
        float pvOffset = _peaksValleysSpline.Evaluate(parameters.PeaksAndValleys);

        float rawHeight = SeaLevel + continentalHeight + pvOffset * erosionFactor;
        return Math.Clamp(rawHeight, MinClampY, ChunkData.SizeY - 2);
    }

    /// <summary>
    /// Computes the terrain height for a column including biome-local height offsets.
    /// </summary>
    /// <param name="parameters">The sampled climate parameters.</param>
    /// <param name="primaryBiome">Primary biome at this position.</param>
    /// <param name="secondaryBiome">Secondary biome for blending.</param>
    /// <param name="blendWeight">Blend weight between primary and secondary.</param>
    /// <returns>Final terrain height as integer Y.</returns>
    public int GetBlendedHeight(
        in ClimateParameters parameters,
        BiomeDefinition primaryBiome,
        BiomeDefinition secondaryBiome,
        float blendWeight)
    {
        float baseHeight = GetHeight(in parameters);

        float biomeOffsetA = primaryBiome.HeightSpline.Evaluate(parameters.PeaksAndValleys);
        float biomeOffsetB = secondaryBiome.HeightSpline.Evaluate(parameters.PeaksAndValleys);
        float blendedBiomeOffset = Lerp(biomeOffsetA, biomeOffsetB, blendWeight);

        int finalHeight = (int)MathF.Round(baseHeight + blendedBiomeOffset);
        return Math.Clamp(finalHeight, MinClampY, ChunkData.SizeY - 2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
