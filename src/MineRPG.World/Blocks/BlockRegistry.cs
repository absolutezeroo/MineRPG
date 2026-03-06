using System;
using System.Collections.Generic;

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
    private const ushort AirBlockId = 0;
    private const int UvComponentsPerFace = 4;

    private readonly Registry<ushort, BlockDefinition> _inner = new();
    private readonly Dictionary<string, BlockDefinition> _nameIndex =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The underlying registry of block definitions keyed by numeric ID.</summary>
    public IRegistry<ushort, BlockDefinition> Inner => _inner;

    /// <summary>The computed texture atlas layout for all block textures.</summary>
    public TextureAtlasLayout AtlasLayout { get; }

    /// <summary>
    /// Initializes the block registry by loading all block definitions and computing atlas UVs.
    /// </summary>
    /// <param name="dataLoader">Data loader for reading block JSON files.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public BlockRegistry(IDataLoader dataLoader, ILogger logger)
    {
        BlockDefinition air = new BlockDefinition
        {
            Id = AirBlockId,
            Name = "Air",
            Flags = BlockFlags.Transparent,
        };
        _inner.Register(AirBlockId, air);
        _nameIndex["Air"] = air;

        IReadOnlyList<BlockDefinition> definitions = dataLoader.LoadAll<BlockDefinition>("Blocks");

        foreach (BlockDefinition definition in definitions)
        {
            if (definition.Id == AirBlockId)
            {
                logger.Warning("BlockRegistry: JSON file defines ID=0 (reserved for Air) - skipping.");
                continue;
            }

            _inner.Register(definition.Id, definition);
            _nameIndex[definition.Name] = definition;
        }

        // Collect unique texture names from all blocks
        HashSet<string> textureNames = new(StringComparer.OrdinalIgnoreCase);

        foreach (BlockDefinition definition in _inner.GetAll())
        {
            if (definition.Textures is null)
            {
                continue;
            }

            string?[] resolved = definition.Textures.Resolve();

            for (int i = 0; i < BlockFaceTextures.FaceCount; i++)
            {
                if (resolved[i] is not null)
                {
                    textureNames.Add(resolved[i]!);
                }
            }
        }

        AtlasLayout = new TextureAtlasLayout(textureNames);

        // Compute per-face UVs for each block
        foreach (BlockDefinition definition in _inner.GetAll())
        {
            ComputeFaceUvs(definition);
        }

        logger.Info("BlockRegistry: Loaded {0} block types (including air), {1} unique textures.",
            _inner.Count, AtlasLayout.TextureNames.Count);
    }

    /// <summary>
    /// Gets a block definition by ID. Returns the air block if the ID is not registered.
    /// </summary>
    /// <param name="id">The block ID to look up.</param>
    /// <returns>The block definition, or air if not found.</returns>
    public BlockDefinition Get(ushort id)
        => _inner.TryGet(id, out BlockDefinition? definition) ? definition : _inner.Get(AirBlockId);

    /// <summary>
    /// Gets a block definition by name. Throws if not found.
    /// </summary>
    /// <param name="name">The block name to look up (case-insensitive).</param>
    /// <returns>The matching block definition.</returns>
    public BlockDefinition GetByName(string name)
        => _nameIndex.TryGetValue(name, out BlockDefinition? definition)
            ? definition
            : throw new KeyNotFoundException($"Block '{name}' not found in registry.");

    /// <summary>
    /// Attempts to get a block definition by name.
    /// </summary>
    /// <param name="name">The block name to look up (case-insensitive).</param>
    /// <param name="definition">The found block definition, if any.</param>
    /// <returns>True if the block was found, false otherwise.</returns>
    public bool TryGetByName(string name, out BlockDefinition definition)
        => _nameIndex.TryGetValue(name, out definition!);

    private void ComputeFaceUvs(BlockDefinition definition)
    {
        if (definition.Textures is null || AtlasLayout.TextureNames.Count == 0)
        {
            return;
        }

        string?[] resolved = definition.Textures.Resolve();

        for (int face = 0; face < BlockFaceTextures.FaceCount; face++)
        {
            string? textureName = resolved[face];

            if (textureName is null || !AtlasLayout.Contains(textureName))
            {
                continue;
            }

            (float u0, float v0, float u1, float v1) = AtlasLayout.GetUvBounds(textureName);
            definition.FaceUvs[face * UvComponentsPerFace + 0] = u0;
            definition.FaceUvs[face * UvComponentsPerFace + 1] = v0;
            definition.FaceUvs[face * UvComponentsPerFace + 2] = u1;
            definition.FaceUvs[face * UvComponentsPerFace + 3] = v1;
        }
    }
}
