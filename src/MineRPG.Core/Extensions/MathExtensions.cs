using System;
using System.Runtime.CompilerServices;

namespace MineRPG.Core.Extensions;

/// <summary>
/// Extension methods for numeric types used in math-heavy code paths.
/// </summary>
public static class MathExtensions
{
    private const float DegreesToRadiansFactor = MathF.PI / 180f;
    private const float RadiansToDegreesFactor = 180f / MathF.PI;

    /// <summary>
    /// Returns true if the integer is within the inclusive range [min, max].
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <returns>True if the value is between min and max (inclusive).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBetween(this int value, int min, int max)
        => value >= min && value <= max;

    /// <summary>
    /// Returns true if the float is within the inclusive range [min, max].
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The inclusive upper bound.</param>
    /// <returns>True if the value is between min and max (inclusive).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBetween(this float value, float min, float max)
        => value >= min && value <= max;

    /// <summary>
    /// Next power of two greater than or equal to <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The input value.</param>
    /// <returns>The smallest power of two that is greater than or equal to the input.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NextPowerOfTwo(this int value)
    {
        if (value <= 1)
        {
            return 1;
        }

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
    /// <param name="value">The value to remap.</param>
    /// <param name="fromMin">Source range minimum.</param>
    /// <param name="fromMax">Source range maximum.</param>
    /// <param name="toMin">Target range minimum.</param>
    /// <param name="toMax">Target range maximum.</param>
    /// <returns>The remapped value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        => toMin + (value - fromMin) / (fromMax - fromMin) * (toMax - toMin);

    /// <summary>
    /// Wraps an integer into [0, max) - correct for negative values
    /// unlike the % operator.
    /// </summary>
    /// <param name="value">The value to wrap.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>The wrapped value in [0, max).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Wrap(this int value, int max)
        => ((value % max) + max) % max;

    /// <summary>
    /// Converts degrees to radians.
    /// </summary>
    /// <param name="degrees">The angle in degrees.</param>
    /// <returns>The angle in radians.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToRadians(this float degrees)
        => degrees * DegreesToRadiansFactor;

    /// <summary>
    /// Converts radians to degrees.
    /// </summary>
    /// <param name="radians">The angle in radians.</param>
    /// <returns>The angle in degrees.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ToDegrees(this float radians)
        => radians * RadiansToDegreesFactor;
}
