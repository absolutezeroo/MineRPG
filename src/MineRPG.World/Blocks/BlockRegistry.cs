using MineRPG.Core.DataLoading;
using MineRPG.Core.Logging;
using MineRPG.Core.Registry;

namespace MineRPG.World.Blocks;

/// <summary>
/// Loads all block definitions from Data/Blocks/, builds a TextureAtlasLayout
/// from unique texture names, computes per-face UVs, and stores blocks keyed by ID.
/// The air block (ID=0) is always synthesized.
/// </summary>
public sealed class BlockRegistry
{
    private readonly Registry<ushort, BlockDefinition> _inner = new();
    private readonly Dictionary<string, BlockDefinition> _nameIndex =
        new(StringComparer.OrdinalIgnoreCase);

    public IRegistry<ushort, BlockDefinition> Inner => _inner;
    public TextureAtlasLayout AtlasLayout { get; }

    public BlockRegistry(IDataLoader dataLoader, ILogger logger)
    {
        var air = new BlockDefinition
        {
            Id = 0,
            Name = "Air",
            Flags = BlockFlags.Transparent,
        };
        _inner.Register(0, air);
        _nameIndex["Air"] = air;

        var definitions = dataLoader.LoadAll<BlockDefinition>("Blocks");
        foreach (var def in definitions)
        {
            if (def.Id == 0)
            {
                logger.Warning("BlockRegistry: JSON file defines ID=0 (reserved for Air) — skipping.");
                continue;
            }

            _inner.Register(def.Id, def);
            _nameIndex[def.Name] = def;
        }

        // Collect unique texture names from all blocks
        var textureNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in _inner.GetAll())
        {
            if (def.Textures is null)
                continue;

            var resolved = def.Textures.Resolve();
            for (var i = 0; i < BlockFaceTextures.FaceCount; i++)
            {
                if (resolved[i] is not null)
                    textureNames.Add(resolved[i]!);
            }
        }

        AtlasLayout = new TextureAtlasLayout(textureNames);

        // Compute per-face UVs for each block
        foreach (var def in _inner.GetAll())
        {
            ComputeFaceUvs(def);
        }

        logger.Info("BlockRegistry: Loaded {0} block types (including air), {1} unique textures.",
            _inner.Count, AtlasLayout.TextureNames.Count);
    }

    public BlockDefinition Get(ushort id)
        => _inner.TryGet(id, out var def) ? def : _inner.Get(0);

    public BlockDefinition GetByName(string name)
        => _nameIndex.TryGetValue(name, out var def)
            ? def
            : throw new KeyNotFoundException($"Block '{name}' not found in registry.");

    public bool TryGetByName(string name, out BlockDefinition definition)
        => _nameIndex.TryGetValue(name, out definition!);

    private void ComputeFaceUvs(BlockDefinition def)
    {
        if (def.Textures is null || AtlasLayout.TextureNames.Count == 0)
            return;

        var resolved = def.Textures.Resolve();
        for (var face = 0; face < BlockFaceTextures.FaceCount; face++)
        {
            var texName = resolved[face];
            if (texName is null || !AtlasLayout.Contains(texName))
                continue;

            var (u0, v0, u1, v1) = AtlasLayout.GetUvBounds(texName);
            def.FaceUvs[face * 4 + 0] = u0;
            def.FaceUvs[face * 4 + 1] = v0;
            def.FaceUvs[face * 4 + 2] = u1;
            def.FaceUvs[face * 4 + 3] = v1;
        }
    }
}
