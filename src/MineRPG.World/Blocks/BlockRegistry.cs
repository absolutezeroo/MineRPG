using System;
using System.Collections.Generic;

using MineRPG.Core.DataLoading;
using MineRPG.Core.Logging;
using MineRPG.Core.Registry;

namespace MineRPG.World.Blocks;

/// <summary>
/// Loads all block definitions from Data/Blocks/, assigns sequential runtime ushort IDs,
/// builds a TextureAtlasLayout, and computes per-face UVs.
/// Air ("minerpg:air") is always synthesized at RuntimeId=0.
/// The <see cref="Get(ushort)"/> method is the hot-path O(1) lookup used by meshing
/// and generation. The <see cref="TryGet(string, out BlockDefinition)"/> method is the
/// cold-path dictionary lookup used at startup for configuration and linking.
/// </summary>
public sealed class BlockRegistry
{
    private const ushort AirRuntimeId = 0;
    private const string AirBlockId = "minerpg:air";
    private const int UvComponentsPerFace = 4;
    private const int InitialTableCapacity = 256;

    private readonly Registry<string, BlockDefinition> _inner = new();
    private BlockDefinition[] _runtimeTable = new BlockDefinition[InitialTableCapacity];
    private ushort _nextRuntimeId = 1;

    /// <summary>The underlying string-keyed registry of block definitions.</summary>
    public IRegistry<string, BlockDefinition> Inner => _inner;

    /// <summary>The computed texture atlas layout for all block textures.</summary>
    public TextureAtlasLayout AtlasLayout { get; }

    /// <summary>Total number of registered block types, including air.</summary>
    public int Count => _inner.Count + 1;

    /// <summary>
    /// Initializes the block registry by loading all block definitions, assigning
    /// runtime IDs, and computing atlas UVs.
    /// </summary>
    /// <param name="dataLoader">Data loader for reading block JSON files.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public BlockRegistry(IDataLoader dataLoader, ILogger logger)
    {
        BlockDefinition air = new BlockDefinition
        {
            Id = AirBlockId,
            DisplayName = "Air",
            Flags = BlockFlags.Transparent,
            RuntimeId = AirRuntimeId,
        };
        _runtimeTable[AirRuntimeId] = air;

        IReadOnlyList<BlockDefinition> definitions = dataLoader.LoadAll<BlockDefinition>("Blocks");

        foreach (BlockDefinition definition in definitions)
        {
            if (string.IsNullOrEmpty(definition.Id))
            {
                logger.Warning("BlockRegistry: Block definition with empty ID skipped.");
                continue;
            }

            if (string.Equals(definition.Id, AirBlockId, StringComparison.OrdinalIgnoreCase))
            {
                logger.Warning(
                    "BlockRegistry: JSON defines '{0}' (reserved for Air) — skipping.",
                    AirBlockId);
                continue;
            }

            definition.RuntimeId = _nextRuntimeId;

            if (_nextRuntimeId >= _runtimeTable.Length)
            {
                Array.Resize(ref _runtimeTable, _runtimeTable.Length * 2);
            }

            _runtimeTable[_nextRuntimeId] = definition;
            _nextRuntimeId++;

            _inner.Register(definition.Id, definition);
        }

        // Collect unique texture names from all blocks
        HashSet<string> textureNames = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < _nextRuntimeId; i++)
        {
            BlockDefinition block = _runtimeTable[i];

            if (block?.Textures is null)
            {
                continue;
            }

            string?[] resolved = block.Textures.Resolve();

            for (int face = 0; face < BlockFaceTextures.FaceCount; face++)
            {
                if (resolved[face] is not null)
                {
                    textureNames.Add(resolved[face]!);
                }
            }
        }

        AtlasLayout = new TextureAtlasLayout(textureNames);

        // Compute per-face UVs for each block
        for (int i = 0; i < _nextRuntimeId; i++)
        {
            if (_runtimeTable[i] is not null)
            {
                ComputeFaceUvs(_runtimeTable[i]);
            }
        }

        _inner.Freeze();

        logger.Info(
            "BlockRegistry: Loaded {0} block types (including air), {1} unique textures.",
            _nextRuntimeId, AtlasLayout.TextureNames.Count);
    }

    /// <summary>
    /// Gets a block definition by runtime ushort ID. Returns the air block if out of range.
    /// Hot-path: O(1) array lookup, zero allocation.
    /// </summary>
    /// <param name="runtimeId">The runtime block ID.</param>
    /// <returns>The block definition, or air if not found.</returns>
    public BlockDefinition Get(ushort runtimeId)
    {
        if (runtimeId >= _nextRuntimeId)
        {
            return _runtimeTable[AirRuntimeId];
        }

        return _runtimeTable[runtimeId] ?? _runtimeTable[AirRuntimeId];
    }

    /// <summary>
    /// Attempts to get a block definition by namespaced string ID.
    /// Cold-path: used at startup for configuration and linking only.
    /// </summary>
    /// <param name="id">The namespaced block ID (e.g., "minerpg:stone").</param>
    /// <param name="definition">The found definition, or null.</param>
    /// <returns>True if found.</returns>
    public bool TryGet(string id, out BlockDefinition definition)
        => _inner.TryGet(id, out definition!);

    /// <summary>
    /// Gets a block definition by namespaced string ID. Throws if not found.
    /// Cold-path: used at startup only.
    /// </summary>
    /// <param name="id">The namespaced block ID.</param>
    /// <returns>The block definition.</returns>
    public BlockDefinition GetById(string id)
    {
        if (!_inner.TryGet(id, out BlockDefinition? definition) || definition is null)
        {
            throw new KeyNotFoundException($"Block '{id}' not found in registry.");
        }

        return definition;
    }

    /// <summary>
    /// Returns all registered block definitions including air.
    /// </summary>
    /// <returns>An enumerable of all block definitions.</returns>
    public IEnumerable<BlockDefinition> GetAll()
    {
        for (int i = 0; i < _nextRuntimeId; i++)
        {
            if (_runtimeTable[i] is not null)
            {
                yield return _runtimeTable[i];
            }
        }
    }

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
