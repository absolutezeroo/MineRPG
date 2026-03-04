using System;
using System.Runtime.CompilerServices;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.World.Generation;

/// <summary>
/// Owns all 2D and 3D noise channels for multi-layer terrain generation.
/// Computes blended per-column surface height and per-voxel cave densities.
///
/// Three macro channels (continentalness, erosion, peaks/valleys) are mapped
/// through global splines and combined to produce terrain height -- inspired
/// by Minecraft 1.18+ density-offset approach.
///
/// All noise sampling is stateless per-call. Thread-safe.
/// </summary>
public sealed class TerrainSampler
{
    private const int SeaLevel = 62;
    private const int MinClampY = 1;

    // Noise frequency and FBM parameters
    private const float ContinentalnessFrequency = 0.0015f;
    private const int ContinentalnessOctaves = 4;
    private const float ContinentalnessLacunarity = 2.0f;
    private const float ContinentalnessPersistence = 0.5f;

    private const float ErosionFrequency = 0.003f;
    private const int ErosionOctaves = 3;
    private const float ErosionLacunarity = 2.2f;
    private const float ErosionPersistence = 0.45f;

    private const float PeaksValleysFrequency = 0.005f;
    private const int PeaksValleysOctaves = 5;
    private const float PeaksValleysLacunarity = 2.0f;
    private const float PeaksValleysPersistence = 0.5f;

    // Cave noise parameters
    private const int CheeseCaveOctaves = 2;
    private const float CheeseCaveFrequency = 0.018f;
    private const float CheeseCaveLacunarity = 2.0f;
    private const float CheeseCavePersistence = 0.5f;
    private const float CheeseCaveThreshold = 0.45f;
    private const float CaveDensityCarve = 2f;

    private const float SpaghettiScale = 0.025f;
    private const float SpaghettiThreshold = 0.15f;

    private const float NoodleScale = 0.04f;
    private const float NoodleThreshold = 0.08f;
    private const float NoodleDensityCarve = 1f;

    private const int DepthSuppressionOffset = 8;
    private const float DepthSuppressionRange = 16f;
    private const float OceanSuppressionMultiplier = 2f;
    private const float SolidDensity = 1f;

    // Noise seed masks
    private const int ContinentalnessSeedMask = 0x11111111;
    private const int ErosionSeedMask = 0x22222222;
    private const int PeaksValleysSeedMask = 0x33333333;
    private const int CheeseCaveSeedMask = 0x44444444;
    private const int SpaghettiASeedMask = 0x55555555;
    private const int SpaghettiB_SeedMask = 0x66666666;
    private const int NoodleASeedMask = 0x77777777;

    private static readonly int NoodleBSeedMask = unchecked((int)0x88888888);

    // 2D terrain shape channels
    private readonly FastNoise _continentalnessNoise;
    private readonly FastNoise _erosionNoise;
    private readonly FastNoise _peaksValleysNoise;

    // 3D cave channels
    private readonly FastNoise _cheeseCaveNoise;
    private readonly FastNoise _spaghettiNoiseA;
    private readonly FastNoise _spaghettiNoiseB;
    private readonly FastNoise _noodleNoiseA;
    private readonly FastNoise _noodleNoiseB;

    private readonly BiomeSelector _biomeSelector;

    // Global splines mapping noise to terrain parameters
    private readonly HeightSpline _continentalnessSpline = new(
    [
        new SplinePoint(-1.0f, -50f),
        new SplinePoint(-0.5f, -25f),
        new SplinePoint(-0.1f, 0f),
        new SplinePoint(0.0f, 2f),
        new SplinePoint(0.3f, 8f),
        new SplinePoint(0.6f, 12f),
        new SplinePoint(1.0f, 30f),
    ]);

    private readonly HeightSpline _erosionSpline = new(
    [
        new SplinePoint(-1.0f, 1.0f),
        new SplinePoint(-0.3f, 0.9f),
        new SplinePoint(0.0f, 0.7f),
        new SplinePoint(0.5f, 0.4f),
        new SplinePoint(1.0f, 0.2f),
    ]);

    private readonly HeightSpline _peaksValleysSpline = new(
    [
        new SplinePoint(-1.0f, -20f),
        new SplinePoint(-0.5f, -8f),
        new SplinePoint(0.0f, 0f),
        new SplinePoint(0.3f, 10f),
        new SplinePoint(0.6f, 25f),
        new SplinePoint(0.9f, 45f),
        new SplinePoint(1.0f, 60f),
    ]);

    /// <summary>
    /// Creates a terrain sampler with the given biome selector and world seed.
    /// </summary>
    /// <param name="biomeSelector">Biome selector for blending terrain heights.</param>
    /// <param name="seed">World seed for noise generation.</param>
    public TerrainSampler(BiomeSelector biomeSelector, int seed)
    {
        _biomeSelector = biomeSelector;
        _continentalnessNoise = new FastNoise(seed ^ ContinentalnessSeedMask);
        _erosionNoise = new FastNoise(seed ^ ErosionSeedMask);
        _peaksValleysNoise = new FastNoise(seed ^ PeaksValleysSeedMask);
        _cheeseCaveNoise = new FastNoise(seed ^ CheeseCaveSeedMask);
        _spaghettiNoiseA = new FastNoise(seed ^ SpaghettiASeedMask);
        _spaghettiNoiseB = new FastNoise(seed ^ SpaghettiB_SeedMask);
        _noodleNoiseA = new FastNoise(seed ^ NoodleASeedMask);
        _noodleNoiseB = new FastNoise(seed ^ NoodleBSeedMask);
    }

    /// <summary>
    /// Compute all column-level (X,Z) data. Called once per column per chunk.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>Precomputed terrain column data.</returns>
    public TerrainColumn SampleColumn(int worldX, int worldZ)
    {
        // Sample the three macro noise channels
        float continental = _continentalnessNoise.FractionalBrownianMotion2D(
            worldX, worldZ,
            octaves: ContinentalnessOctaves,
            frequency: ContinentalnessFrequency,
            lacunarity: ContinentalnessLacunarity,
            persistence: ContinentalnessPersistence);

        float erosion = _erosionNoise.FractionalBrownianMotion2D(
            worldX, worldZ,
            octaves: ErosionOctaves,
            frequency: ErosionFrequency,
            lacunarity: ErosionLacunarity,
            persistence: ErosionPersistence);

        float peaksValleys = _peaksValleysNoise.FractionalBrownianMotion2D(
            worldX, worldZ,
            octaves: PeaksValleysOctaves,
            frequency: PeaksValleysFrequency,
            lacunarity: PeaksValleysLacunarity,
            persistence: PeaksValleysPersistence);

        // Map through global splines
        float continentalOffset = _continentalnessSpline.Evaluate(continental);
        float erosionFactor = _erosionSpline.Evaluate(erosion);
        float peaksValleysOffset = _peaksValleysSpline.Evaluate(peaksValleys);

        // Combine: erosion modulates the peaks/valleys amplitude
        float rawHeight = SeaLevel + continentalOffset + peaksValleysOffset * erosionFactor;

        // Biome blending
        (BiomeDefinition primaryBiome, BiomeDefinition secondaryBiome, float blendWeight) =
            _biomeSelector.SelectWeighted(worldX, worldZ);

        // Biome-local height offset via per-biome splines
        float biomeOffsetA = primaryBiome.HeightSpline.Evaluate(peaksValleys);
        float biomeOffsetB = secondaryBiome.HeightSpline.Evaluate(peaksValleys);
        float blendedBiomeOffset = Lerp(biomeOffsetA, biomeOffsetB, blendWeight);

        int finalHeight = (int)MathF.Round(rawHeight + blendedBiomeOffset);
        finalHeight = Math.Clamp(finalHeight, MinClampY, ChunkData.SizeY - 2);

        return new TerrainColumn
        {
            SurfaceY = finalHeight,
            SubSurfaceDepth = primaryBiome.SubSurfaceDepth,
            PrimaryBiome = primaryBiome,
            SecondaryBiome = secondaryBiome,
            BlendWeight = blendWeight,
            Continentalness = continental,
        };
    }

    /// <summary>
    /// Returns cave density at a specific voxel. Negative values should be carved.
    /// Combines cheese (large caverns), spaghetti (worm tunnels), and noodle (thin tunnels).
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldY">World Y coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <param name="surfaceY">Surface height at this column.</param>
    /// <param name="continentalness">Continentalness noise value for ocean suppression.</param>
    /// <returns>Cave density value. Negative means carved.</returns>
    public float SampleCaveDensity(int worldX, int worldY, int worldZ, int surfaceY, float continentalness)
    {
        float density = SolidDensity;

        // Cheese caves: large open caverns
        float cheese = _cheeseCaveNoise.FractionalBrownianMotion3D(
            worldX, worldY, worldZ,
            octaves: CheeseCaveOctaves,
            frequency: CheeseCaveFrequency,
            lacunarity: CheeseCaveLacunarity,
            persistence: CheeseCavePersistence);

        if (cheese > CheeseCaveThreshold)
        {
            density -= CaveDensityCarve;
        }

        // Spaghetti caves: worm-like tunnels (two orthogonal noise channels)
        float spaghettiA = _spaghettiNoiseA.Sample3D(
            worldX * SpaghettiScale, worldY * SpaghettiScale, worldZ * SpaghettiScale);
        float spaghettiB = _spaghettiNoiseB.Sample3D(
            worldX * SpaghettiScale, worldY * SpaghettiScale, worldZ * SpaghettiScale);
        float spaghettiTunnel = MathF.Sqrt(spaghettiA * spaghettiA + spaghettiB * spaghettiB);

        if (spaghettiTunnel < SpaghettiThreshold)
        {
            density -= CaveDensityCarve;
        }

        // Noodle caves: thinner tunnels using independent noise channels
        float noodleA = _noodleNoiseA.Sample3D(
            worldX * NoodleScale, worldY * NoodleScale, worldZ * NoodleScale);
        float noodleB = _noodleNoiseB.Sample3D(
            worldX * NoodleScale, worldY * NoodleScale, worldZ * NoodleScale);
        float noodleTunnel = MathF.Sqrt(noodleA * noodleA + noodleB * noodleB);

        if (noodleTunnel < NoodleThreshold)
        {
            density -= NoodleDensityCarve;
        }

        // Depth suppression: caves rarer near surface (top 24 blocks of underground)
        float depthFactor = Math.Clamp(
            (surfaceY - worldY - DepthSuppressionOffset) / DepthSuppressionRange, 0f, 1f);

        // Ocean suppression: no caves under deep ocean
        float oceanSuppression = Math.Clamp(
            continentalness * OceanSuppressionMultiplier + SolidDensity, 0f, 1f);

        // Only apply carving if suppression allows it
        if (density < SolidDensity)
        {
            density = SolidDensity + (density - SolidDensity) * depthFactor * oceanSuppression;
        }

        return density;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
