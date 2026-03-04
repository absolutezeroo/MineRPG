using FluentAssertions;

using MineRPG.Core.Math;

namespace MineRPG.Tests.Core;

public sealed class FastNoiseTests
{
    private readonly FastNoise _noise = new(seed: 42);

    [Fact]
    public void Sample2D_ReturnsDeterministicValues()
    {
        float a = _noise.Sample2D(1.5f, 2.5f);
        float b = _noise.Sample2D(1.5f, 2.5f);

        a.Should().Be(b);
    }

    [Fact]
    public void Sample2D_DifferentInputs_ReturnsDifferentValues()
    {
        float a = _noise.Sample2D(0f, 0f);
        float b = _noise.Sample2D(100f, 100f);

        a.Should().NotBe(b);
    }

    [Fact]
    public void Sample3D_ReturnsDeterministicValues()
    {
        float a = _noise.Sample3D(1f, 2f, 3f);
        float b = _noise.Sample3D(1f, 2f, 3f);

        a.Should().Be(b);
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentValues()
    {
        FastNoise noise1 = new FastNoise(seed: 1);
        FastNoise noise2 = new FastNoise(seed: 2);

        float a = noise1.Sample2D(5f, 5f);
        float b = noise2.Sample2D(5f, 5f);

        a.Should().NotBe(b);
    }

    [Fact]
    public void FractionalBrownianMotion2D_ReturnsValueInRange()
    {
        // Sample many points and verify they stay within a reasonable range
        FastNoise noise = new FastNoise(seed: 123);
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int x = 0; x < 100; x++)
        {
            for (int z = 0; z < 100; z++)
            {
                float value = noise.FractionalBrownianMotion2D(x, z, octaves: 6,
                    frequency: 0.01f, lacunarity: 2f, persistence: 0.5f);
                if (value < min)
                {
                    min = value;
                }

                if (value > max)
                {
                    max = value;
                }
            }
        }

        // fBm values should be approximately in [-1, 1]
        min.Should().BeGreaterThan(-2f);
        max.Should().BeLessThan(2f);
    }

    [Fact]
    public void FractionalBrownianMotion3D_ReturnsDeterministicValues()
    {
        float a = _noise.FractionalBrownianMotion3D(1f, 2f, 3f, octaves: 4,
            frequency: 0.05f, lacunarity: 2f, persistence: 0.5f);
        float b = _noise.FractionalBrownianMotion3D(1f, 2f, 3f, octaves: 4,
            frequency: 0.05f, lacunarity: 2f, persistence: 0.5f);

        a.Should().Be(b);
    }

    [Fact]
    public void Seed_PropertyReturnsConstructorValue()
    {
        FastNoise noise = new FastNoise(seed: 777);
        noise.Seed.Should().Be(777);
    }

    [Fact]
    public void Sample2D_ReturnsValueInExpectedRange()
    {
        FastNoise noise = new FastNoise(seed: 99);
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int x = 0; x < 200; x++)
        {
            for (int z = 0; z < 200; z++)
            {
                float value = noise.Sample2D(x * 0.1f, z * 0.1f);
                if (value < min)
                {
                    min = value;
                }

                if (value > max)
                {
                    max = value;
                }
            }
        }

        min.Should().BeGreaterThan(-1.1f);
        max.Should().BeLessThan(1.1f);
        // The fix ensures meaningful range (was near-zero before the * 47.0f fix)
        (max - min).Should().BeGreaterThan(0.5f);
    }
}
