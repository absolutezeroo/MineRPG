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

    public int Seed { get; }

    public FastNoise(int seed = 0)
    {
        Seed = seed;
        _perm = new short[PermSize];
        _permGrad2 = new short[PermSize];
        _permGrad3 = new short[PermSize];

        var source = new short[PermSize];
        for (short i = 0; i < PermSize; i++)
            source[i] = i;

        // Knuth shuffle seeded by the given seed
        long s = seed;
        for (var i = PermSize - 1; i >= 0; i--)
        {
            s = s * 6364136223846793005L + 1442695040888963407L;
            var r = (int)((s + 31) % (i + 1));
            if (r < 0) r += i + 1;

            _perm[i] = source[r];
            _permGrad2[i] = (short)(_perm[i] % (Grad2.Length / 2));
            _permGrad3[i] = (short)(_perm[i] % (Grad3.Length / 3));
            source[r] = source[i];
        }
    }

    /// <summary>2D OpenSimplex2S noise. Returns value in [-1, 1].</summary>
    public float Sample2D(float x, float y) => Noise2(x, y);

    /// <summary>3D OpenSimplex2S noise. Returns value in [-1, 1].</summary>
    public float Sample3D(float x, float y, float z) => Noise3(x, y, z);

    /// <summary>
    /// Fractional Brownian Motion 2D — sums multiple octaves of noise
    /// for natural-looking terrain height maps.
    /// </summary>
    public float FractionalBrownianMotion2D(
        float x,
        float z,
        int octaves,
        float frequency,
        float lacunarity,
        float persistence)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(octaves, 1);

        var result = 0f;
        var amplitude = 1f;
        var maxValue = 0f;
        var freq = frequency;

        for (var i = 0; i < octaves; i++)
        {
            result += Sample2D(x * freq, z * freq) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            freq *= lacunarity;
        }

        return result / maxValue;
    }

    /// <summary>
    /// Fractional Brownian Motion 3D — for cave density fields.
    /// </summary>
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

        var result = 0f;
        var amplitude = 1f;
        var maxValue = 0f;
        var freq = frequency;

        for (var i = 0; i < octaves; i++)
        {
            result += Sample3D(x * freq, y * freq, z * freq) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            freq *= lacunarity;
        }

        return result / maxValue;
    }

    // OpenSimplex2S 2D core
    private const float Skew2D = 0.366025403784439f;   // (sqrt(3)-1)/2
    private const float Unskew2D = 0.211324865405187f;  // (3-sqrt(3))/6

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Noise2(float x, float y)
    {
        var s = (x + y) * Skew2D;
        var i = FastFloor(x + s);
        var j = FastFloor(y + s);

        var t = (i + j) * Unskew2D;
        var x0 = x - (i - t);
        var y0 = y - (j - t);

        int i1, j1;
        if (x0 > y0) { i1 = 1; j1 = 0; }
        else { i1 = 0; j1 = 1; }

        var x1 = x0 - i1 + Unskew2D;
        var y1 = y0 - j1 + Unskew2D;
        var x2 = x0 - 1f + 2f * Unskew2D;
        var y2 = y0 - 1f + 2f * Unskew2D;

        var n = 0f;
        n += Contrib2(x0, y0, i, j);
        n += Contrib2(x1, y1, i + i1, j + j1);
        n += Contrib2(x2, y2, i + 1, j + 1);
        return n * 47.0f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Contrib2(float dx, float dy, int gi, int gj)
    {
        var attn = 0.5f - dx * dx - dy * dy;
        if (attn <= 0f)
            return 0f;

        var gi2 = _permGrad2[(gi + _perm[gj & PermMask]) & PermMask] * 2;
        return attn * attn * attn * attn * (Grad2[gi2] * dx + Grad2[gi2 + 1] * dy);
    }

    // OpenSimplex2S 3D core
    private const float Skew3D = 1f / 3f;
    private const float Unskew3D = 1f / 6f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Noise3(float x, float y, float z)
    {
        var s = (x + y + z) * Skew3D;
        var i = FastFloor(x + s);
        var j = FastFloor(y + s);
        var k = FastFloor(z + s);

        var t = (i + j + k) * Unskew3D;
        var x0 = x - (i - t);
        var y0 = y - (j - t);
        var z0 = z - (k - t);

        int i1, j1, k1, i2, j2, k2;
        if (x0 >= y0)
        {
            if (y0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
            else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; }
            else { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; }
        }
        else
        {
            if (y0 < z0) { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; }
            else if (x0 < z0) { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; }
            else { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
        }

        var x1 = x0 - i1 + Unskew3D;
        var y1 = y0 - j1 + Unskew3D;
        var z1 = z0 - k1 + Unskew3D;
        var x2 = x0 - i2 + 2f * Unskew3D;
        var y2 = y0 - j2 + 2f * Unskew3D;
        var z2 = z0 - k2 + 2f * Unskew3D;
        var x3 = x0 - 1f + 3f * Unskew3D;
        var y3 = y0 - 1f + 3f * Unskew3D;
        var z3 = z0 - 1f + 3f * Unskew3D;

        var n = 0f;
        n += Contrib3(x0, y0, z0, i, j, k);
        n += Contrib3(x1, y1, z1, i + i1, j + j1, k + k1);
        n += Contrib3(x2, y2, z2, i + i2, j + j2, k + k2);
        n += Contrib3(x3, y3, z3, i + 1, j + 1, k + 1);
        return n * 32f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float Contrib3(float dx, float dy, float dz, int gi, int gj, int gk)
    {
        var attn = 0.6f - dx * dx - dy * dy - dz * dz;
        if (attn <= 0f)
            return 0f;

        var gi3 = _permGrad3[(gi + _perm[(gj + _perm[gk & PermMask]) & PermMask]) & PermMask] * 3;
        return attn * attn * attn * attn
               * (Grad3[gi3] * dx + Grad3[gi3 + 1] * dy + Grad3[gi3 + 2] * dz);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int FastFloor(float x)
    {
        var xi = (int)x;
        return x < xi ? xi - 1 : xi;
    }
}
