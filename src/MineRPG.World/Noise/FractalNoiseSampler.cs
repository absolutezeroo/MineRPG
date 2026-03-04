using System;

using MineRPG.Core.Math;

namespace MineRPG.World.Noise;

/// <summary>
/// Fractal Brownian motion noise sampler wrapping <see cref="FastNoise"/>.
/// Stacks multiple octaves with configurable frequency scaling and gain.
/// Thread-safe: all state is readonly after construction.
/// </summary>
public sealed class FractalNoiseSampler : INoiseSampler
{
    private readonly FastNoise _noise;
    private readonly float _frequency;
    private readonly int _octaves;
    private readonly float _lacunarity;
    private readonly float _gain;

    /// <summary>
    /// Creates a fractal noise sampler from the given settings and world seed.
    /// </summary>
    /// <param name="settings">Noise configuration (frequency, octaves, etc.).</param>
    /// <param name="worldSeed">The world seed.</param>
    public FractalNoiseSampler(NoiseSettings settings, int worldSeed)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        _noise = new FastNoise(worldSeed + settings.SeedOffset);
        _frequency = settings.Frequency;
        _octaves = Math.Max(1, settings.Octaves);
        _lacunarity = settings.Lacunarity;
        _gain = settings.Gain;
    }

    /// <summary>
    /// Creates a fractal noise sampler with explicit parameters.
    /// </summary>
    /// <param name="seed">Noise seed.</param>
    /// <param name="frequency">Base frequency.</param>
    /// <param name="octaves">Number of octaves.</param>
    /// <param name="lacunarity">Frequency multiplier per octave.</param>
    /// <param name="gain">Amplitude multiplier per octave.</param>
    public FractalNoiseSampler(int seed, float frequency, int octaves, float lacunarity, float gain)
    {
        _noise = new FastNoise(seed);
        _frequency = frequency;
        _octaves = Math.Max(1, octaves);
        _lacunarity = lacunarity;
        _gain = gain;
    }

    /// <inheritdoc />
    public float Sample2D(float x, float z)
    {
        return _noise.FractionalBrownianMotion2D(
            x, z,
            octaves: _octaves,
            frequency: _frequency,
            lacunarity: _lacunarity,
            persistence: _gain);
    }

    /// <inheritdoc />
    public float Sample3D(float x, float y, float z)
    {
        return _noise.FractionalBrownianMotion3D(
            x, y, z,
            octaves: _octaves,
            frequency: _frequency,
            lacunarity: _lacunarity,
            persistence: _gain);
    }
}
