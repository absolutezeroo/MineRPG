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
        HeightSpline continentalness = new(
        [
            new SplinePoint(-1.0f, -60f),
            new SplinePoint(-0.55f, -35f),
            new SplinePoint(-0.3f, -12f),
            new SplinePoint(-0.2f, -4f),
            new SplinePoint(-0.12f, 0f),
            new SplinePoint(-0.05f, 4f),
            new SplinePoint(0.05f, 10f),
            new SplinePoint(0.2f, 22f),
            new SplinePoint(0.4f, 40f),
            new SplinePoint(0.65f, 70f),
            new SplinePoint(0.85f, 100f),
            new SplinePoint(1.0f, 120f),
        ]);

        HeightSpline erosion = new(
        [
            new SplinePoint(-1.0f, 1.5f),
            new SplinePoint(-0.5f, 1.2f),
            new SplinePoint(0.0f, 1.0f),
            new SplinePoint(0.5f, 0.5f),
            new SplinePoint(1.0f, 0.2f),
        ]);

        HeightSpline peaksValleys = new(
        [
            new SplinePoint(-1.0f, -40f),
            new SplinePoint(-0.5f, -10f),
            new SplinePoint(0.0f, 0f),
            new SplinePoint(0.5f, 15f),
            new SplinePoint(1.0f, 50f),
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
