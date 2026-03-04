using System;

using MineRPG.World.Chunks;

namespace MineRPG.World.Generation.CaveFeatures;

/// <summary>
/// Orchestrates all cave feature generators (pillars, stalactites, stalagmites).
/// Runs after cave carving to add decorative formations.
/// Thread-safe: all state is readonly after construction.
/// </summary>
public sealed class CaveFeaturePipeline
{
    private readonly PillarGenerator _pillarGenerator;
    private readonly StalactiteGenerator _stalactiteGenerator;
    private readonly StalagmiteGenerator _stalagmiteGenerator;

    /// <summary>
    /// Creates a cave feature pipeline with all generators.
    /// </summary>
    /// <param name="config">Cave feature configuration.</param>
    /// <param name="formationBlockId">Block ID for formation material.</param>
    public CaveFeaturePipeline(CaveFeatureConfig config, ushort formationBlockId)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        _pillarGenerator = new PillarGenerator(config, formationBlockId);
        _stalactiteGenerator = new StalactiteGenerator(config, formationBlockId);
        _stalagmiteGenerator = new StalagmiteGenerator(config, formationBlockId);
    }

    /// <summary>
    /// Generates all cave features within the given chunk.
    /// </summary>
    /// <param name="data">Chunk data to modify.</param>
    /// <param name="chunkWorldX">World X of the chunk origin.</param>
    /// <param name="chunkWorldZ">World Z of the chunk origin.</param>
    /// <param name="worldSeed">World seed for deterministic placement.</param>
    public void Generate(ChunkData data, int chunkWorldX, int chunkWorldZ, int worldSeed)
    {
        int chunkSeed = HashCode.Combine(worldSeed, chunkWorldX, chunkWorldZ, 0xCAFE);
        Random random = new Random(chunkSeed);

        _pillarGenerator.Generate(data, random);
        _stalactiteGenerator.Generate(data, random);
        _stalagmiteGenerator.Generate(data, random);
    }
}
