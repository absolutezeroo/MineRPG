using System.Runtime.CompilerServices;

namespace MineRPG.Core.Extensions;

public static class MathExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBetween(this int value, int min, int max)
        => value >= min && value <= max;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBetween(this float value, float min, float max)
        => value >= min && value <= max;

    /// <summary>Next power of two greater than or equal to <paramref name="value"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NextPowerOfTwo(this int value)
    {
        if (value <= 1) return 1;
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    /// <summary>
    /// Remaps <paramref name="value"/> from [fromMin, fromMax] to [toMin, toMax].
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        => toMin + (value - fromMin) / (fromMax - fromMin) * (toMax - toMin);

    /// <summary>
    /// Wraps an integer into [0, max) — correct for negative values
    /// unlike the % operator.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Wrap(this int value, int max)
        => ((value % max) + max) % max;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToRadians(this float degrees)
        => degrees * (MathF.PI / 180f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToDegrees(this float radians)
        => radians * (180f / MathF.PI);
}
