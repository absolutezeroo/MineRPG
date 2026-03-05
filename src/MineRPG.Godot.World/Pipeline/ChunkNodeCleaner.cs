using System.Collections.Concurrent;
using System.Diagnostics;

using MineRPG.Core.Math;

namespace MineRPG.Godot.World.Pipeline;

/// <summary>
/// Drains deferred chunk node cleanups on the main thread within a frame budget.
/// Nodes are returned to the pool, freeing their meshes and collision shapes.
/// </summary>
internal sealed class ChunkNodeCleaner
{
    private readonly ConcurrentQueue<NodeCleanupWork> _pendingCleanup = new();
    private readonly WorldNode _worldNode;

    /// <summary>
    /// Creates a node cleaner that returns nodes via the given world node.
    /// </summary>
    /// <param name="worldNode">World node that owns the chunk node pool.</param>
    public ChunkNodeCleaner(WorldNode worldNode)
    {
        _worldNode = worldNode;
    }

    /// <summary>
    /// Enqueues a chunk node for deferred cleanup.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    /// <param name="node">The extracted chunk node to clean up.</param>
    public void Enqueue(ChunkCoord coord, ChunkNode node)
    {
        _pendingCleanup.Enqueue(new NodeCleanupWork(coord, node));
    }

    /// <summary>
    /// Drains pending node cleanups within the given time budget.
    /// </summary>
    /// <param name="frameBudgetMs">Maximum milliseconds to spend on cleanup.</param>
    public void CleanNodes(int frameBudgetMs)
    {
        long budgetTicks = (long)(frameBudgetMs * (Stopwatch.Frequency / 1000.0));
        long startTick = Stopwatch.GetTimestamp();

        while (_pendingCleanup.TryDequeue(out NodeCleanupWork cleanupWork))
        {
            _worldNode.ReturnChunkNodeToPool(cleanupWork.Node);

            if (Stopwatch.GetTimestamp() - startTick >= budgetTicks)
            {
                break;
            }
        }
    }
}
