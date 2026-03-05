using System.Collections.Generic;

using Godot;

namespace MineRPG.Godot.World;

/// <summary>
/// Recycles ChunkNode instances to avoid frequent allocation and QueueFree.
/// Nodes in the pool are removed from the scene tree (Visible=false, no parent)
/// and re-added when rented. All operations are main-thread only.
/// </summary>
public sealed class ChunkNodePool
{
    private readonly Stack<ChunkNode> _idle = new();

    private int _activeCount;
    private long _recycleCount;

    /// <summary>
    /// Gets the number of idle nodes currently in the pool.
    /// </summary>
    public int IdleCount => _idle.Count;

    /// <summary>
    /// Gets the number of actively used nodes rented from the pool.
    /// </summary>
    public int ActiveCount => _activeCount;

    /// <summary>
    /// Gets the total number of times a node has been returned to the pool.
    /// </summary>
    public long RecycleCount => _recycleCount;

    /// <summary>
    /// Retrieves a ChunkNode from the pool, or creates a new one if empty.
    /// The returned node must be initialized with <see cref="ChunkNode.Initialize"/>.
    /// </summary>
    /// <returns>A ChunkNode ready for initialization.</returns>
    public ChunkNode Rent()
    {
        if (_idle.TryPop(out ChunkNode? node))
        {
            _activeCount++;
            return node;
        }

        _activeCount++;
        return new ChunkNode();
    }

    /// <summary>
    /// Returns a ChunkNode to the pool. Clears its mesh and removes it from parent.
    /// </summary>
    /// <param name="node">The chunk node to return to the pool.</param>
    public void Return(ChunkNode node)
    {
        node.ClearMesh();
        node.Visible = false;

        Node? parent = node.GetParent();
        parent?.RemoveChild(node);

        _idle.Push(node);
        _activeCount--;
        _recycleCount++;
    }

    /// <summary>
    /// Frees all idle nodes in the pool via QueueFree. Call during shutdown
    /// to prevent Godot resource leaks.
    /// </summary>
    public void FreeAll()
    {
        while (_idle.TryPop(out ChunkNode? node))
        {
            node.QueueFree();
        }
    }
}
