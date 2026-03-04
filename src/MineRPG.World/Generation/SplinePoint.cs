using Newtonsoft.Json;

namespace MineRPG.World.Generation;

/// <summary>
/// A single control point for a <see cref="HeightSpline"/>.
/// <see cref="InputValue"/> is a noise value in [-1, 1].
/// <see cref="OutputY"/> is the mapped output (world Y offset or multiplier).
/// </summary>
public readonly struct SplinePoint(float inputValue, float outputY)
{
    [JsonProperty("inputValue")]
    public float InputValue { get; init; } = inputValue;

    [JsonProperty("outputY")]
    public float OutputY { get; init; } = outputY;
}
