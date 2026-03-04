using MineRPG.Core.Logging;
using MineRPG.Core.Math;

namespace MineRPG.World.Chunks;

/// <summary>
/// Orchestrates chunk save/load using an IChunkSerializer and IChunkStorage.
/// Pure logic — no file I/O or Godot dependency.
/// </summary>
public sealed class ChunkPersistenceService
{
    private readonly IChunkSerializer _serializer;
    private readonly IChunkStorage _storage;
    private readonly ILogger _logger;

    public ChunkPersistenceService(IChunkSerializer serializer, IChunkStorage storage, ILogger logger)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if a saved chunk exists in storage.
    /// </summary>
    public bool HasSavedChunk(ChunkCoord coord) => _storage.Exists(coord);

    /// <summary>
    /// Load a chunk from storage into the target ChunkData.
    /// Returns true if loaded successfully, false if no save exists.
    /// </summary>
    public bool TryLoad(ChunkCoord coord, ChunkData target)
    {
        if (!_storage.Exists(coord))
            return false;

        try
        {
            var data = _storage.Load(coord);
            _serializer.Deserialize(data, target);
            _logger.Debug("Loaded chunk {0} from storage ({1} bytes)", coord, data.Length);
            return true;
        }
        catch (ChunkSerializationException ex)
        {
            _logger.Error("Failed to load chunk {0}: {1}", ex, coord, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Save a chunk to storage if it has been modified.
    /// Returns true if saved, false if not modified.
    /// </summary>
    public bool SaveIfModified(ChunkEntry entry)
    {
        if (!entry.IsModified)
            return false;

        var data = _serializer.Serialize(entry.Data);
        _storage.Save(entry.Coord, data);
        entry.IsModified = false;
        _logger.Debug("Saved chunk {0} to storage ({1} bytes)", entry.Coord, data.Length);
        return true;
    }

    /// <summary>
    /// Force save a chunk regardless of modification state.
    /// </summary>
    public void Save(ChunkEntry entry)
    {
        var data = _serializer.Serialize(entry.Data);
        _storage.Save(entry.Coord, data);
        entry.IsModified = false;
        _logger.Debug("Saved chunk {0} to storage ({1} bytes)", entry.Coord, data.Length);
    }
}
