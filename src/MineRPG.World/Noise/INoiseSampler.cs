namespace MineRPG.World.Noise;

/// <summary>
/// Interface for noise sampling in 2D and 3D.
/// All implementations must be thread-safe.
/// </summary>
public interface INoiseSampler
{
    /// <summary>
    /// Samples 2D noise at the given coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <returns>Noise value in approximately [-1, 1].</returns>
    float Sample2D(float x, float z);

    /// <summary>
    /// Samples 3D noise at the given coordinates.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <returns>Noise value in approximately [-1, 1].</returns>
    float Sample3D(float x, float y, float z);
}
