using System;

using MineRPG.Core.Logging;
using MineRPG.Core.Math;

namespace MineRPG.World.Chunks.Serialization;

/// <summary>
/// Orchestrates chunk save/load using an IChunkSerializer and IChunkStorage.
/// Pure logic — no file I/O or Godot dependency.
/// </summary>
public sealed class ChunkPersistenceService
{
    private readonly IChunkSerializer _serializer;
    private readonly IChunkStorage _storage;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new chunk persistence service.
    /// </summary>
    /// <param name="serializer">Serializer for chunk data.</param>
    /// <param name="storage">Storage backend for persisted chunks.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public ChunkPersistenceService(IChunkSerializer serializer, IChunkStorage storage, ILogger logger)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if a saved chunk exists in storage.
    /// </summary>
    /// <param name="coord">The chunk coordinate to check.</param>
    /// <returns>True if a saved chunk exists.</returns>
    public bool HasSavedChunk(ChunkCoord coord) => _storage.Exists(coord);

    /// <summary>
    /// Load a chunk from storage into the target ChunkData.
    /// Returns true if loaded successfully, false if no save exists.
    /// </summary>
    /// <param name="coord">The chunk coordinate to load.</param>
    /// <param name="target">The target chunk data to populate.</param>
    /// <returns>True if loaded successfully, false otherwise.</returns>
    public bool TryLoad(ChunkCoord coord, ChunkData target)
    {
        if (!_storage.Exists(coord))
        {
            return false;
        }

        try
        {
            byte[] data = _storage.Load(coord);
            _serializer.Deserialize(data, target);
            _logger.Debug("Loaded chunk {0} from storage ({1} bytes)", coord, data.Length);
            return true;
        }
        catch (ChunkSerializationException exception)
        {
            _logger.Error("Failed to load chunk {0}: {1}", exception, coord, exception.Message);
            return false;
        }
    }

    /// <summary>
    /// Save a chunk to storage if it has been modified.
    /// Returns true if saved, false if not modified.
    /// </summary>
    /// <param name="entry">The chunk entry to save.</param>
    /// <returns>True if the chunk was saved.</returns>
    public bool SaveIfModified(ChunkEntry entry)
    {
        if (!entry.IsModified)
        {
            return false;
        }

        byte[] data = _serializer.Serialize(entry.Data);
        _storage.Save(entry.Coord, data);
        entry.IsModified = false;
        _logger.Debug("Saved chunk {0} to storage ({1} bytes)", entry.Coord, data.Length);
        return true;
    }

    /// <summary>
    /// Force save a chunk regardless of modification state.
    /// </summary>
    /// <param name="entry">The chunk entry to save.</param>
    public void Save(ChunkEntry entry)
    {
        byte[] data = _serializer.Serialize(entry.Data);
        _storage.Save(entry.Coord, data);
        entry.IsModified = false;
        _logger.Debug("Saved chunk {0} to storage ({1} bytes)", entry.Coord, data.Length);
    }

    /// <summary>
    /// Serializes and saves a pre-snapshotted block buffer for the given coordinate.
    /// Safe to call from background threads — no ChunkEntry or ChunkData access.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <param name="blockSnapshot">A pre-copied block array of length ChunkData.TotalBlocks.</param>
    public void SaveSnapshot(ChunkCoord coord, ushort[] blockSnapshot)
    {
        ChunkData temporary = new(coord);
        temporary.LoadFromSpan(blockSnapshot);

        byte[] data = _serializer.Serialize(temporary);
        _storage.Save(coord, data);
        _logger.Debug("Saved chunk {0} snapshot to storage ({1} bytes)", coord, data.Length);
    }
}
