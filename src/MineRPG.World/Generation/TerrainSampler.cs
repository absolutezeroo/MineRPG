using System;
using System.Runtime.CompilerServices;

using MineRPG.Core.Math;
using MineRPG.World.Biomes.Climate;
using MineRPG.World.Chunks;

namespace MineRPG.World.Generation;

/// <summary>
/// Owns all noise channels for multi-layer terrain generation.
/// Computes blended per-column surface height and per-voxel cave densities.
/// Delegates climate sampling to <see cref="ClimateSampler"/> and terrain height
/// computation to <see cref="TerrainShaper"/>.
/// Thread-safe: all state is readonly after construction.
/// </summary>
public sealed class TerrainSampler
{
    private const int SeaLevel = 62;
    private const int MinClampY = 1;

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

    private const int CheeseCaveSeedMask = 0x44444444;
    private const int SpaghettiASeedMask = 0x55555555;
    private const int SpaghettiB_SeedMask = 0x66666666;
    private const int NoodleASeedMask = 0x77777777;

    private static readonly int NoodleBSeedMask = unchecked((int)0x88888888);

    private readonly FastNoise _cheeseCaveNoise;
    private readonly FastNoise _spaghettiNoiseA;
    private readonly FastNoise _spaghettiNoiseB;
    private readonly FastNoise _noodleNoiseA;
    private readonly FastNoise _noodleNoiseB;

    private readonly ClimateSampler _climateSampler;
    private readonly BiomeSelector _biomeSelector;
    private readonly TerrainShaper _terrainShaper;

    /// <summary>
    /// Creates a terrain sampler with full 6D climate support.
    /// </summary>
    /// <param name="climateSampler">Climate sampler for noise channels.</param>
    /// <param name="biomeSelector">Biome selector for 6D selection.</param>
    /// <param name="terrainShaper">Terrain shaper for spline-based height.</param>
    /// <param name="seed">World seed for cave noise.</param>
    public TerrainSampler(
        ClimateSampler climateSampler,
        BiomeSelector biomeSelector,
        TerrainShaper terrainShaper,
        int seed)
    {
        _climateSampler = climateSampler;
        _biomeSelector = biomeSelector;
        _terrainShaper = terrainShaper;

        _cheeseCaveNoise = new FastNoise(seed ^ CheeseCaveSeedMask);
        _spaghettiNoiseA = new FastNoise(seed ^ SpaghettiASeedMask);
        _spaghettiNoiseB = new FastNoise(seed ^ SpaghettiB_SeedMask);
        _noodleNoiseA = new FastNoise(seed ^ NoodleASeedMask);
        _noodleNoiseB = new FastNoise(seed ^ NoodleBSeedMask);
    }

    /// <summary>
    /// Legacy constructor for backwards compatibility with existing code.
    /// Creates internal climate sampler and terrain shaper.
    /// </summary>
    /// <param name="biomeSelector">Biome selector.</param>
    /// <param name="seed">World seed.</param>
    public TerrainSampler(BiomeSelector biomeSelector, int seed)
    {
        ClimateNoiseConfig config = ClimateNoiseConfig.CreateDefault();
        _climateSampler = new ClimateSampler(config, seed);
        _biomeSelector = biomeSelector;
        _terrainShaper = TerrainShaper.CreateDefault();

        _cheeseCaveNoise = new FastNoise(seed ^ CheeseCaveSeedMask);
        _spaghettiNoiseA = new FastNoise(seed ^ SpaghettiASeedMask);
        _spaghettiNoiseB = new FastNoise(seed ^ SpaghettiB_SeedMask);
        _noodleNoiseA = new FastNoise(seed ^ NoodleASeedMask);
        _noodleNoiseB = new FastNoise(seed ^ NoodleBSeedMask);
    }

    /// <summary>
    /// Compute all column-level (X,Z) data using 6D climate parameters.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>Precomputed terrain column data.</returns>
    public TerrainColumn SampleColumn(int worldX, int worldZ)
    {
        ClimateParameters climate = _climateSampler.SampleSurface(worldX, worldZ);

        (BiomeDefinition primaryBiome, BiomeDefinition secondaryBiome, float blendWeight) =
            _biomeSelector.SelectWeighted(in climate);

        int finalHeight = _terrainShaper.GetBlendedHeight(
            in climate, primaryBiome, secondaryBiome, blendWeight);

        return new TerrainColumn
        {
            SurfaceY = finalHeight,
            SubSurfaceDepth = primaryBiome.SubSurfaceDepth,
            PrimaryBiome = primaryBiome,
            SecondaryBiome = secondaryBiome,
            BlendWeight = blendWeight,
            Continentalness = climate.Continentalness,
            Climate = climate,
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

        float spaghettiA = _spaghettiNoiseA.Sample3D(
            worldX * SpaghettiScale, worldY * SpaghettiScale, worldZ * SpaghettiScale);
        float spaghettiB = _spaghettiNoiseB.Sample3D(
            worldX * SpaghettiScale, worldY * SpaghettiScale, worldZ * SpaghettiScale);
        float spaghettiTunnel = MathF.Sqrt(spaghettiA * spaghettiA + spaghettiB * spaghettiB);

        if (spaghettiTunnel < SpaghettiThreshold)
        {
            density -= CaveDensityCarve;
        }

        float noodleA = _noodleNoiseA.Sample3D(
            worldX * NoodleScale, worldY * NoodleScale, worldZ * NoodleScale);
        float noodleB = _noodleNoiseB.Sample3D(
            worldX * NoodleScale, worldY * NoodleScale, worldZ * NoodleScale);
        float noodleTunnel = MathF.Sqrt(noodleA * noodleA + noodleB * noodleB);

        if (noodleTunnel < NoodleThreshold)
        {
            density -= NoodleDensityCarve;
        }

        float depthFactor = Math.Clamp(
            (surfaceY - worldY - DepthSuppressionOffset) / DepthSuppressionRange, 0f, 1f);

        float oceanSuppression = Math.Clamp(
            continentalness * OceanSuppressionMultiplier + SolidDensity, 0f, 1f);

        if (density < SolidDensity)
        {
            density = SolidDensity + (density - SolidDensity) * depthFactor * oceanSuppression;
        }

        return density;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
