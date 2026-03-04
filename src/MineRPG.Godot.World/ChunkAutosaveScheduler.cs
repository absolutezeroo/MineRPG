using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Logging;
using MineRPG.World.Chunks;

namespace MineRPG.Godot.World;

/// <summary>
/// Periodically saves modified chunks to disk.
/// Extracted from <see cref="ChunkLoadingScheduler"/> to keep file lengths manageable.
/// Runs autosave every <see cref="AutosaveIntervalSeconds"/> and saves all dirty chunks on exit.
/// </summary>
public sealed partial class ChunkAutosaveScheduler : Node
{
    private const float AutosaveIntervalSeconds = 60f;

    private IChunkManager _chunkManager = null!;
    private ILogger _logger = null!;
    private ChunkPersistenceService? _persistence;
    private float _autosaveTimer;

    /// <inheritdoc />
    public override void _Ready()
    {
        _chunkManager = ServiceLocator.Instance.Get<IChunkManager>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        if (ServiceLocator.Instance.TryGet<ChunkPersistenceService>(out ChunkPersistenceService? persistence))
        {
            _persistence = persistence;
        }
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        SaveAllDirtyChunks();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        _autosaveTimer += (float)delta;

        if (_autosaveTimer >= AutosaveIntervalSeconds)
        {
            _autosaveTimer = 0f;
            SaveAllDirtyChunks();
        }
    }

    /// <summary>
    /// Saves a single chunk if it has been modified. Called during chunk unload.
    /// </summary>
    /// <param name="entry">The chunk entry to save.</param>
    public void SaveIfModified(ChunkEntry entry)
    {
        _persistence?.SaveIfModified(entry);
    }

    /// <summary>
    /// Saves all modified chunks to disk immediately.
    /// </summary>
    public void SaveAllDirtyChunks()
    {
        if (_persistence is null)
        {
            return;
        }

        int saved = 0;

        foreach (ChunkEntry entry in _chunkManager.GetAll())
        {
            if (_persistence.SaveIfModified(entry))
            {
                saved++;
            }
        }

        if (saved > 0)
        {
            _logger.Info("Autosave: Saved {0} modified chunks.", saved);
        }
    }
}
