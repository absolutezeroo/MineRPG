using System;

namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Converts climate parameter values (typically in [-1, 1]) to gradient colors
/// for the biome overlay visualization modes.
/// All methods return <see cref="DebugColor"/>.
/// </summary>
public static class ClimateVisualizer
{
    /// <summary>
    /// Temperature gradient: blue (cold, -1) to red (hot, +1).
    /// </summary>
    /// <param name="temperature">Temperature value, typically [-1, 1].</param>
    /// <returns>A gradient color from blue to red.</returns>
    public static DebugColor TemperatureToColor(float temperature)
    {
        float t = NormalizeToUnit(temperature);
        return new DebugColor(t, 0.2f * (1f - MathF.Abs(t * 2f - 1f)), 1f - t);
    }

    /// <summary>
    /// Humidity gradient: yellow (dry, -1) to green (humid, +1).
    /// </summary>
    /// <param name="humidity">Humidity value, typically [-1, 1].</param>
    /// <returns>A gradient color from yellow to green.</returns>
    public static DebugColor HumidityToColor(float humidity)
    {
        float t = NormalizeToUnit(humidity);
        return new DebugColor(1f - t, 0.5f + 0.5f * t, 0.1f * (1f - t));
    }

    /// <summary>
    /// Continentalness gradient: dark blue (ocean, -1) to brown (inland, +1).
    /// </summary>
    /// <param name="continentalness">Continentalness value, typically [-1, 1].</param>
    /// <returns>A gradient color from dark blue to brown.</returns>
    public static DebugColor ContinentalnessToColor(float continentalness)
    {
        float t = NormalizeToUnit(continentalness);
        return new DebugColor(0.3f * t + 0.1f, 0.2f * t + 0.1f, 0.7f * (1f - t));
    }

    /// <summary>
    /// Erosion gradient: green (flat, -1) to gray (rugged, +1).
    /// </summary>
    /// <param name="erosion">Erosion value, typically [-1, 1].</param>
    /// <returns>A gradient color from green to gray.</returns>
    public static DebugColor ErosionToColor(float erosion)
    {
        float t = NormalizeToUnit(erosion);
        float gray = 0.3f + 0.5f * t;
        return new DebugColor(gray * t + 0.2f * (1f - t), 0.6f * (1f - t) + gray * t, gray * t + 0.1f * (1f - t));
    }

    /// <summary>
    /// Peaks and Valleys gradient: purple (valley, -1) to white (peak, +1).
    /// </summary>
    /// <param name="peaksAndValleys">PeaksAndValleys value, typically [-1, 1].</param>
    /// <returns>A gradient color from purple to white.</returns>
    public static DebugColor PeaksAndValleysToColor(float peaksAndValleys)
    {
        float t = NormalizeToUnit(peaksAndValleys);
        return new DebugColor(0.4f + 0.6f * t, 0.1f + 0.9f * t, 0.6f + 0.4f * t);
    }

    /// <summary>
    /// Height map gradient: black (low, 0) to white (high, 256).
    /// </summary>
    /// <param name="height">Height value, typically [0, 256].</param>
    /// <param name="maxHeight">Maximum height for normalization.</param>
    /// <returns>A grayscale color from black to white.</returns>
    public static DebugColor HeightToColor(float height, float maxHeight = 256f)
    {
        float t = System.Math.Clamp(height / maxHeight, 0f, 1f);
        return new DebugColor(t, t, t);
    }

    /// <summary>
    /// Cave density visualization: solid = transparent, cave = red.
    /// </summary>
    /// <param name="density">Cave density value. Negative = carved.</param>
    /// <returns>Transparent for solid, red for carved space.</returns>
    public static DebugColor CaveDensityToColor(float density)
    {
        if (density >= 0f)
        {
            return new DebugColor(0.2f, 0.2f, 0.2f, 0.3f);
        }

        float intensity = System.Math.Clamp(-density, 0f, 1f);
        return new DebugColor(1f, 0.2f, 0.1f, 0.5f + 0.5f * intensity);
    }

    /// <summary>
    /// Normalizes a value from [-1, 1] to [0, 1].
    /// </summary>
    private static float NormalizeToUnit(float value) => System.Math.Clamp((value + 1f) * 0.5f, 0f, 1f);
}
