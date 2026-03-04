using System;
using System.Runtime.CompilerServices;

using Newtonsoft.Json;

namespace MineRPG.World.Biomes.Climate;

/// <summary>
/// Defines a min/max range for a single climate parameter.
/// Used by <see cref="BiomeClimateTarget"/> to specify where a biome lives
/// in the 6-dimensional climate space.
/// </summary>
public readonly struct ParameterRange
{
    /// <summary>Minimum value of the range (inclusive).</summary>
    [JsonProperty("min")]
    public float Min { get; init; }

    /// <summary>Maximum value of the range (inclusive).</summary>
    [JsonProperty("max")]
    public float Max { get; init; }

    /// <summary>
    /// Creates a parameter range with the given bounds.
    /// </summary>
    /// <param name="min">Minimum value (inclusive).</param>
    /// <param name="max">Maximum value (inclusive).</param>
    public ParameterRange(float min, float max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Returns the center of this range.
    /// </summary>
    public float Center => (Min + Max) * 0.5f;

    /// <summary>
    /// Returns the distance from the given value to this range.
    /// Returns 0 if the value is inside the range.
    /// </summary>
    /// <param name="value">The value to measure distance from.</param>
    /// <returns>Distance to the range, or 0 if inside.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float DistanceTo(float value)
    {
        if (value < Min)
        {
            return Min - value;
        }

        if (value > Max)
        {
            return value - Max;
        }

        return 0f;
    }

    /// <summary>
    /// Returns true if the given value falls within this range (inclusive).
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns>True if the value is within [Min, Max].</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(float value) => value >= Min && value <= Max;

    /// <summary>
    /// A range covering the full [-1, 1] interval.
    /// </summary>
    public static readonly ParameterRange Full = new(-1f, 1f);

    /// <summary>
    /// A range covering [0, 1] for depth parameters.
    /// </summary>
    public static readonly ParameterRange FullDepth = new(0f, 1f);
}
