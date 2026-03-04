using Newtonsoft.Json;

namespace MineRPG.World.Generation;

/// <summary>
/// A single control point for a <see cref="HeightSpline"/>.
/// <see cref="InputValue"/> is a noise value in [-1, 1].
/// <see cref="OutputY"/> is the mapped output (world Y offset or multiplier).
/// </summary>
public readonly struct SplinePoint
{
    /// <summary>Noise input value, typically in [-1, 1].</summary>
    [JsonProperty("inputValue")]
    public float InputValue { get; init; }

    /// <summary>Mapped output height or multiplier.</summary>
    [JsonProperty("outputY")]
    public float OutputY { get; init; }

    /// <summary>
    /// Creates a spline control point.
    /// </summary>
    /// <param name="inputValue">Noise input value.</param>
    /// <param name="outputY">Mapped output value.</param>
    public SplinePoint(float inputValue, float outputY)
    {
        InputValue = inputValue;
        OutputY = outputY;
    }
}
