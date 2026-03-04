using System.Runtime.CompilerServices;
using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.World.Generation;

/// <summary>
/// Owns all 2D and 3D noise channels for multi-layer terrain generation.
/// Computes blended per-column surface height and per-voxel cave densities.
///
/// Three macro channels (continentalness, erosion, peaks/valleys) are mapped
/// through global splines and combined to produce terrain height — inspired
/// by Minecraft 1.18+ density-offset approach.
///
/// All noise sampling is stateless per-call. Thread-safe.
/// </summary>
public sealed class TerrainSampler(BiomeSelector biomeSelector, int seed)
{
    private const int SeaLevel = 62;

    // 2D terrain shape channels
    private readonly FastNoise _continentalnessNoise = new(seed ^ 0x11111111);
    private readonly FastNoise _erosionNoise = new(seed ^ 0x22222222);
    private readonly FastNoise _peaksValleysNoise = new(seed ^ 0x33333333);

    // 3D cave channels
    private readonly FastNoise _cheeseCaveNoise = new(seed ^ 0x44444444);
    private readonly FastNoise _spaghettiNoiseA = new(seed ^ 0x55555555);
    private readonly FastNoise _spaghettiNoiseB = new(seed ^ 0x66666666);
    private readonly FastNoise _noodleNoiseA = new(seed ^ 0x77777777);
    private readonly FastNoise _noodleNoiseB = new(seed ^ unchecked((int)0x88888888));

    // Global splines mapping noise → terrain parameters
    private readonly HeightSpline _continentalnessSpline = new(
    [
        new SplinePoint(-1.0f, -50f), // deep ocean
        new SplinePoint(-0.5f, -25f), // coastal ocean
        new SplinePoint(-0.1f, 0f),   // shore
        new SplinePoint(0.0f, 2f),    // beach
        new SplinePoint(0.3f, 8f),    // lowlands
        new SplinePoint(0.6f, 12f),   // midlands
        new SplinePoint(1.0f, 30f),   // inland plateau
    ]);
    private readonly HeightSpline _erosionSpline = new(
    [
        new SplinePoint(-1.0f, 1.0f),   // uneroded (mountains preserved)
        new SplinePoint(-0.3f, 0.9f),
        new SplinePoint(0.0f, 0.7f), // moderate erosion
        new SplinePoint(0.5f, 0.4f), // quite eroded
        new SplinePoint(1.0f, 0.2f), // extremely eroded (flat mesa)
    ]);
    private readonly HeightSpline _peaksValleysSpline = new(
    [
        new SplinePoint(-1.0f, -20f), // deep valley
        new SplinePoint(-0.5f, -8f),  // valley edge
        new SplinePoint(0.0f, 0f),    // neutral
        new SplinePoint(0.3f, 10f),   // gentle hills
        new SplinePoint(0.6f, 25f),   // hills
        new SplinePoint(0.9f, 45f),   // mountains
        new SplinePoint(1.0f, 60f),   // mountain peaks
    ]);

    // deep ocean
    // coastal ocean
    // shore
    // beach
    // lowlands
    // midlands
    // inland plateau
    // uneroded (mountains preserved)
    // moderate erosion
    // quite eroded
    // extremely eroded (flat mesa)
    // deep valley
    // valley edge
    // neutral
    // gentle hills
    // hills
    // mountains
    // mountain peaks

    /// <summary>
    /// Compute all column-level (X,Z) data. Called once per column per chunk.
    /// </summary>
    public TerrainColumn SampleColumn(int worldX, int worldZ)
    {
        // Sample the three macro noise channels
        var continental = _continentalnessNoise.FractionalBrownianMotion2D(
            worldX, worldZ, octaves: 4, frequency: 0.0015f, lacunarity: 2.0f, persistence: 0.5f);

        var erosion = _erosionNoise.FractionalBrownianMotion2D(
            worldX, worldZ, octaves: 3, frequency: 0.003f, lacunarity: 2.2f, persistence: 0.45f);

        var pv = _peaksValleysNoise.FractionalBrownianMotion2D(
            worldX, worldZ, octaves: 5, frequency: 0.005f, lacunarity: 2.0f, persistence: 0.5f);

        // Map through global splines
        var continentalOffset = _continentalnessSpline.Evaluate(continental);
        var erosionFactor = _erosionSpline.Evaluate(erosion);
        var pvOffset = _peaksValleysSpline.Evaluate(pv);

        // Combine: erosion modulates the peaks/valleys amplitude
        var rawHeight = SeaLevel + continentalOffset + pvOffset * erosionFactor;

        // Biome blending
        var (primaryBiome, secondaryBiome, blendWeight) = biomeSelector.SelectWeighted(worldX, worldZ);

        // Biome-local height offset via per-biome splines
        var biomeOffsetA = primaryBiome.HeightSpline.Evaluate(pv);
        var biomeOffsetB = secondaryBiome.HeightSpline.Evaluate(pv);
        var blendedBiomeOffset = Lerp(biomeOffsetA, biomeOffsetB, blendWeight);

        var finalHeight = (int)MathF.Round(rawHeight + blendedBiomeOffset);
        finalHeight = Math.Clamp(finalHeight, 1, ChunkData.SizeY - 2);

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
    public float SampleCaveDensity(int worldX, int worldY, int worldZ, int surfaceY, float continentalness)
    {
        var density = 1f; // start solid

        // Cheese caves: large open caverns
        var cheese = _cheeseCaveNoise.FractionalBrownianMotion3D(
            worldX, worldY, worldZ,
            octaves: 2, frequency: 0.018f, lacunarity: 2.0f, persistence: 0.5f);

        if (cheese > 0.45f)
            density -= 2f;

        // Spaghetti caves: worm-like tunnels (two orthogonal noise channels)
        var spaghA = _spaghettiNoiseA.Sample3D(worldX * 0.025f, worldY * 0.025f, worldZ * 0.025f);
        var spaghB = _spaghettiNoiseB.Sample3D(worldX * 0.025f, worldY * 0.025f, worldZ * 0.025f);
        var spaghTunnel = MathF.Sqrt(spaghA * spaghA + spaghB * spaghB);

        if (spaghTunnel < 0.15f)
            density -= 2f;

        // Noodle caves: thinner tunnels using independent noise channels
        var noodleA = _noodleNoiseA.Sample3D(worldX * 0.04f, worldY * 0.04f, worldZ * 0.04f);
        var noodleB = _noodleNoiseB.Sample3D(worldX * 0.04f, worldY * 0.04f, worldZ * 0.04f);
        var noodleTunnel = MathF.Sqrt(noodleA * noodleA + noodleB * noodleB);

        if (noodleTunnel < 0.08f)
            density -= 1f;

        // Depth suppression: caves rarer near surface (top 24 blocks of underground)
        var depthFactor = Math.Clamp((surfaceY - worldY - 8) / 16f, 0f, 1f);

        // Ocean suppression: no caves under deep ocean
        var oceanSuppression = Math.Clamp(continentalness * 2f + 1f, 0f, 1f);

        // Only apply carving if suppression allows it
        if (density < 1f)
            density = 1f + (density - 1f) * depthFactor * oceanSuppression;

        return density;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
