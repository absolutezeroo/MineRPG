using System;
using System.Runtime.CompilerServices;

namespace MineRPG.Core.Math;

/// <summary>
/// Standalone OpenSimplex2S (smooth) noise implementation.
/// Produces seamless, high-quality gradient noise for terrain generation.
///
/// Thread-safe: all state is readonly after construction.
/// </summary>
public sealed class FastNoise
{
    private const int PermSize = 2048;
    private const int PermMask = PermSize - 1;
    private const float NoiseScale2D = 47.0f;
    private const float NoiseScale3D = 32f;

    // OpenSimplex2S 2D core constants
    private const float Skew2D = 0.366025403784439f;   // (sqrt(3)-1)/2
    private const float Unskew2D = 0.211324865405187f;  // (3-sqrt(3))/6

    // OpenSimplex2S 3D core constants
    private const float Skew3D = 1f / 3f;
    private const float Unskew3D = 1f / 6f;

    private const float Attn2DBase = 0.5f;
    private const float Attn3DBase = 0.6f;

    private readonly short[] _perm;
    private readonly short[] _permGrad2;
    private readonly short[] _permGrad3;

    private static readonly float[] Grad2 =
    [
        0.130526192220052f, 0.99144486137381f,
        0.38268343236509f, 0.923879532511287f,
        0.608761429008721f, 0.793353340291235f,
        0.793353340291235f, 0.608761429008721f,
        0.923879532511287f, 0.38268343236509f,
        0.99144486137381f, 0.130526192220052f,
        0.99144486137381f, -0.130526192220052f,
        0.923879532511287f, -0.38268343236509f,
        0.793353340291235f, -0.608761429008721f,
        0.608761429008721f, -0.793353340291235f,
        0.38268343236509f, -0.923879532511287f,
        0.130526192220052f, -0.99144486137381f,
        -0.130526192220052f, -0.99144486137381f,
        -0.38268343236509f, -0.923879532511287f,
        -0.608761429008721f, -0.793353340291235f,
        -0.793353340291235f, -0.608761429008721f,
        -0.923879532511287f, -0.38268343236509f,
        -0.99144486137381f, -0.130526192220052f,
        -0.99144486137381f, 0.130526192220052f,
        -0.923879532511287f, 0.38268343236509f,
        -0.793353340291235f, 0.608761429008721f,
        -0.608761429008721f, 0.793353340291235f,
        -0.38268343236509f, 0.923879532511287f,
        -0.130526192220052f, 0.99144486137381f,
    ];

    private static readonly float[] Grad3 =
    [
        -2.22474487139f, -2.22474487139f, -1f,
        -2.22474487139f, -2.22474487139f, 1f,
        -3.0862664687972017f, -1.1721513422464978f, 0f,
        -1.1721513422464978f, -3.0862664687972017f, 0f,
        -2.22474487139f, -1f, -2.22474487139f,
        -2.22474487139f, 1f, -2.22474487139f,
        -1.1721513422464978f, 0f, -3.0862664687972017f,
        -3.0862664687972017f, 0f, -1.1721513422464978f,
        -2.22474487139f, -1f, 2.22474487139f,
        -2.22474487139f, 1f, 2.22474487139f,
        -3.0862664687972017f, 0f, 1.1721513422464978f,
        -1.1721513422464978f, 0f, 3.0862664687972017f,
        -2.22474487139f, 2.22474487139f, -1f,
        -2.22474487139f, 2.22474487139f, 1f,
        -1.1721513422464978f, 3.0862664687972017f, 0f,
        -3.0862664687972017f, 1.1721513422464978f, 0f,
        -1f, -2.22474487139f, -2.22474487139f,
        1f, -2.22474487139f, -2.22474487139f,
        0f, -3.0862664687972017f, -1.1721513422464978f,
        0f, -1.1721513422464978f, -3.0862664687972017f,
        -1f, -2.22474487139f, 2.22474487139f,
        1f, -2.22474487139f, 2.22474487139f,
        0f, -1.1721513422464978f, 3.0862664687972017f,
        0f, -3.0862664687972017f, 1.1721513422464978f,
        -1f, 2.22474487139f, -2.22474487139f,
        1f, 2.22474487139f, -2.22474487139f,
        0f, 1.1721513422464978f, -3.0862664687972017f,
        0f, 3.0862664687972017f, -1.1721513422464978f,
        -1f, 2.22474487139f, 2.22474487139f,
        1f, 2.22474487139f, 2.22474487139f,
        0f, 3.0862664687972017f, 1.1721513422464978f,
        0f, 1.1721513422464978f, 3.0862664687972017f,
        2.22474487139f, -2.22474487139f, -1f,
        2.22474487139f, -2.22474487139f, 1f,
        1.1721513422464978f, -3.0862664687972017f, 0f,
        3.0862664687972017f, -1.1721513422464978f, 0f,
        2.22474487139f, -1f, -2.22474487139f,
        2.22474487139f, 1f, -2.22474487139f,
        3.0862664687972017f, 0f, -1.1721513422464978f,
        1.1721513422464978f, 0f, -3.0862664687972017f,
        2.22474487139f, -1f, 2.22474487139f,
        2.22474487139f, 1f, 2.22474487139f,
        1.1721513422464978f, 0f, 3.0862664687972017f,
        3.0862664687972017f, 0f, 1.1721513422464978f,
        2.22474487139f, 2.22474487139f, -1f,
        2.22474487139f, 2.22474487139f, 1f,
        3.0862664687972017f, 1.1721513422464978f, 0f,
        1.1721513422464978f, 3.0862664687972017f, 0f,
    ];

    /// <summary>
    /// The seed used to initialize the permutation tables.
    /// </summary>
    public int Seed { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="FastNoise"/> with the given seed.
    /// </summary>
    /// <param name="seed">Seed for the permutation table shuffle.</param>
    public FastNoise(int seed = 0)
    {
        Seed = seed;
        _perm = new short[PermSize];
        _permGrad2 = new short[PermSize];
        _permGrad3 = new short[PermSize];

        short[] source = new short[PermSize];

        for (short i = 0; i < PermSize; i++)
        {
            source[i] = i;
        }

        // Knuth shuffle seeded by the given seed
        long shuffleSeed = seed;
        int grad2HalfLength = Grad2.Length / 2;
        int grad3ThirdLength = Grad3.Length / 3;

        for (int i = PermSize - 1; i >= 0; i--)
        {
            shuffleSeed = shuffleSeed * 6364136223846793005L + 1442695040888963407L;
            int randomIndex = (int)((shuffleSeed + 31) % (i + 1));

            if (randomIndex < 0)
            {
                randomIndex += i + 1;
            }

            _perm[i] = source[randomIndex];
            _permGrad2[i] = (short)(_perm[i] % grad2HalfLength);
            _permGrad3[i] = (short)(_perm[i] % grad3ThirdLength);
            source[randomIndex] = source[i];
        }
    }

    /// <summary>
    /// 2D OpenSimplex2S noise. Returns value in [-1, 1].
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <returns>Noise value in the range [-1, 1].</returns>
    public float Sample2D(float x, float y) => Noise2(x, y);

    /// <summary>
    /// 3D OpenSimplex2S noise. Returns value in [-1, 1].
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <returns>Noise value in the range [-1, 1].</returns>
    public float Sample3D(float x, float y, float z) => Noise3(x, y, z);

    /// <summary>
    /// Fractional Brownian Motion 2D — sums multiple octaves of noise
    /// for natural-looking terrain height maps.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="octaves">Number of noise octaves to sum.</param>
    /// <param name="frequency">Base frequency of the first octave.</param>
    /// <param name="lacunarity">Frequency multiplier per octave.</param>
    /// <param name="persistence">Amplitude multiplier per octave.</param>
    /// <returns>The combined noise value, normalized to approximately [-1, 1].</returns>
    public float FractionalBrownianMotion2D(
        float x,
        float z,
        int octaves,
        float frequency,
        float lacunarity,
        float persistence)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(octaves, 1);

        float result = 0f;
        float amplitude = 1f;
        float maxValue = 0f;
        float currentFrequency = frequency;

        for (int i = 0; i < octaves; i++)
        {
            result += Sample2D(x * currentFrequency, z * currentFrequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            currentFrequency *= lacunarity;
        }

        return result / maxValue;
    }

    /// <summary>
    /// Fractional Brownian Motion 3D — for cave density fields.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="octaves">Number of noise octaves to sum.</param>
    /// <param name="frequency">Base frequency of the first octave.</param>
    /// <param name="lacunarity">Frequency multiplier per octave.</param>
    /// <param name="persistence">Amplitude multiplier per octave.</param>
    /// <returns>The combined noise value, normalized to approximately [-1, 1].</returns>
    public float FractionalBrownianMotion3D(
        float x,
        float y,
        float z,
        int octaves,
        float frequency,
        float lacunarity,
        float persistence)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(octaves, 1);

        float result = 0f;
        float amplitude = 1f;
        float maxValue = 0f;
        float currentFrequency = frequency;

        for (int i = 0; i < octaves; i++)
        {
            result += Sample3D(x * currentFrequency, y * currentFrequency, z * currentFrequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            currentFrequency *= lacunarity;
        }

        return result / maxValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Noise2(float x, float y)
    {
        float skew = (x + y) * Skew2D;
        int i = FastFloor(x + skew);
        int j = FastFloor(y + skew);

        float unskew = (i + j) * Unskew2D;
        float x0 = x - (i - unskew);
        float y0 = y - (j - unskew);

        int i1, j1;

        if (x0 > y0)
        {
            i1 = 1;
            j1 = 0;
        }
        else
        {
            i1 = 0;
            j1 = 1;
        }

        float x1 = x0 - i1 + Unskew2D;
        float y1 = y0 - j1 + Unskew2D;
        float x2 = x0 - 1f + 2f * Unskew2D;
        float y2 = y0 - 1f + 2f * Unskew2D;

        float noise = 0f;
        noise += Contrib2(x0, y0, i, j);
        noise += Contrib2(x1, y1, i + i1, j + j1);
        noise += Contrib2(x2, y2, i + 1, j + 1);
        return noise * NoiseScale2D;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Contrib2(float dx, float dy, int gi, int gj)
    {
        float attn = Attn2DBase - dx * dx - dy * dy;

        if (attn <= 0f)
        {
            return 0f;
        }

        int gi2 = _permGrad2[(gi + _perm[gj & PermMask]) & PermMask] * 2;
        return attn * attn * attn * attn * (Grad2[gi2] * dx + Grad2[gi2 + 1] * dy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Noise3(float x, float y, float z)
    {
        float skew = (x + y + z) * Skew3D;
        int i = FastFloor(x + skew);
        int j = FastFloor(y + skew);
        int k = FastFloor(z + skew);

        float unskew = (i + j + k) * Unskew3D;
        float x0 = x - (i - unskew);
        float y0 = y - (j - unskew);
        float z0 = z - (k - unskew);

        int i1, j1, k1, i2, j2, k2;

        if (x0 >= y0)
        {
            if (y0 >= z0)
            {
                i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0;
            }
            else if (x0 >= z0)
            {
                i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1;
            }
            else
            {
                i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1;
            }
        }
        else
        {
            if (y0 < z0)
            {
                i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1;
            }
            else if (x0 < z0)
            {
                i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1;
            }
            else
            {
                i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0;
            }
        }

        float x1 = x0 - i1 + Unskew3D;
        float y1 = y0 - j1 + Unskew3D;
        float z1 = z0 - k1 + Unskew3D;
        float x2 = x0 - i2 + 2f * Unskew3D;
        float y2 = y0 - j2 + 2f * Unskew3D;
        float z2 = z0 - k2 + 2f * Unskew3D;
        float x3 = x0 - 1f + 3f * Unskew3D;
        float y3 = y0 - 1f + 3f * Unskew3D;
        float z3 = z0 - 1f + 3f * Unskew3D;

        float noise = 0f;
        noise += Contrib3(x0, y0, z0, i, j, k);
        noise += Contrib3(x1, y1, z1, i + i1, j + j1, k + k1);
        noise += Contrib3(x2, y2, z2, i + i2, j + j2, k + k2);
        noise += Contrib3(x3, y3, z3, i + 1, j + 1, k + 1);
        return noise * NoiseScale3D;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Contrib3(float dx, float dy, float dz, int gi, int gj, int gk)
    {
        float attn = Attn3DBase - dx * dx - dy * dy - dz * dz;

        if (attn <= 0f)
        {
            return 0f;
        }

        int gi3 = _permGrad3[(gi + _perm[(gj + _perm[gk & PermMask]) & PermMask]) & PermMask] * 3;
        return attn * attn * attn * attn
               * (Grad3[gi3] * dx + Grad3[gi3 + 1] * dy + Grad3[gi3 + 2] * dz);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FastFloor(float x)
    {
        int xi = (int)x;
        return x < xi ? xi - 1 : xi;
    }
}
