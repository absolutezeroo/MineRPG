using System;

using MineRPG.World.Noise;

namespace MineRPG.World.Generation;

/// <summary>
/// Computes 3D density values for terrain overhangs, arches, and complex formations.
/// A position is solid when density &gt; 0 and air when density &lt;= 0.
/// The base density is (surfaceHeight - y), modulated by 3D noise for overhangs.
/// Thread-safe: all state is readonly after construction.
/// </summary>
public sealed class DensityFunction
{
    private const float OverhangFrequency = 0.02f;
    private const int OverhangOctaves = 3;
    private const float OverhangLacunarity = 2.0f;
    private const float OverhangGain = 0.5f;
    private const float DefaultOverhangStrength = 12f;
    private const int OverhangBandAbove = 20;
    private const int OverhangBandBelow = 20;
    private const float MinErosionForOverhangs = -0.3f;

    private readonly FractalNoiseSampler _overhangNoise;
    private readonly float _overhangStrength;

    /// <summary>
    /// Creates a density function with the given noise seed.
    /// </summary>
    /// <param name="worldSeed">World seed for overhang noise.</param>
    /// <param name="overhangStrength">Amplitude of the overhang effect.</param>
    public DensityFunction(int worldSeed, float overhangStrength = DefaultOverhangStrength)
    {
        NoiseSettings settings = new NoiseSettings
        {
            SeedOffset = 80000,
            Frequency = OverhangFrequency,
            Octaves = OverhangOctaves,
            Lacunarity = OverhangLacunarity,
            Gain = OverhangGain,
        };
        _overhangNoise = new FractalNoiseSampler(settings, worldSeed);
        _overhangStrength = overhangStrength;
    }

    /// <summary>
    /// Computes the density at a 3D position. Positive = solid, non-positive = air.
    /// Overhang noise is only applied within a band around the surface for performance.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldY">World Y coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <param name="surfaceY">Surface height at this column.</param>
    /// <param name="erosion">Erosion value at this column (low = more overhangs).</param>
    /// <returns>Density value. Positive = solid, non-positive = air.</returns>
    public float GetDensity(int worldX, int worldY, int worldZ, int surfaceY, float erosion)
    {
        float baseDensity = surfaceY - worldY;

        // Only apply overhang noise within a band around the surface
        int distanceToSurface = Math.Abs(worldY - surfaceY);

        if (distanceToSurface > OverhangBandAbove + OverhangBandBelow)
        {
            return baseDensity;
        }

        // Overhang strength is modulated by erosion — low erosion = more overhangs on mountains
        float erosionFactor = Math.Clamp(
            (MinErosionForOverhangs - erosion) / (MinErosionForOverhangs + 1f), 0f, 1f);

        if (erosionFactor <= 0f)
        {
            return baseDensity;
        }

        float noise = _overhangNoise.Sample3D(worldX, worldY, worldZ);
        float bandFactor = 1f - (float)distanceToSurface / (OverhangBandAbove + OverhangBandBelow);

        return baseDensity + noise * _overhangStrength * erosionFactor * bandFactor;
    }
}
