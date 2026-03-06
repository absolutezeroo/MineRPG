using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Logging;
using MineRPG.World.Chunks;

namespace MineRPG.Godot.World.Chunks;

/// <summary>
/// Periodically snapshots modified chunks and enqueues them for async background saving.
/// File I/O is entirely off the main thread - this node only copies block data and hands
/// save work items to <see cref="ChunkLoadingScheduler"/>'s worker pool.
/// </summary>
public sealed partial class ChunkAutosaveScheduler : Node
{
    private const float AutosaveIntervalSeconds = 60f;

    private IChunkManager _chunkManager = null!;
    private ILogger _logger = null!;
    private ChunkLoadingScheduler? _scheduler;
    private float _autosaveTimer;

    /// <inheritdoc />
    public override void _Ready()
    {
        _chunkManager = ServiceLocator.Instance.Get<IChunkManager>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        if (ServiceLocator.Instance.TryGet<ChunkLoadingScheduler>(out ChunkLoadingScheduler? scheduler))
        {
            _scheduler = scheduler;
        }
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        // Enqueue final saves for all remaining dirty chunks.
        // ChunkLoadingScheduler._ExitTree will also sweep and flush as a safety net.
        EnqueueAllDirtyChunks();
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        _autosaveTimer += (float)delta;

        if (_autosaveTimer >= AutosaveIntervalSeconds)
        {
            _autosaveTimer = 0f;
            EnqueueAllDirtyChunks();
        }
    }

    /// <summary>
    /// Snapshots all modified chunks and enqueues them for async background saving.
    /// No file I/O occurs on the calling thread.
    /// </summary>
    public void EnqueueAllDirtyChunks()
    {
        if (_scheduler is null)
        {
            return;
        }

        int enqueued = 0;

        foreach (ChunkEntry entry in _chunkManager.GetAll())
        {
            // Skip chunks that are not modified or are already being unloaded
            if (!entry.IsModified || entry.State == ChunkState.Unloading)
            {
                continue;
            }

            ushort[] snapshot = new ushort[ChunkData.TotalBlocks];
            entry.Data.CopyBlocksUnderReadLock(snapshot);
            entry.IsModified = false;

            _scheduler.EnqueueSave(entry.Coord, snapshot);
            enqueued++;
        }

        if (enqueued > 0)
        {
            _logger.Info("Autosave: Enqueued {0} chunks for async save.", enqueued);
        }
    }
}
