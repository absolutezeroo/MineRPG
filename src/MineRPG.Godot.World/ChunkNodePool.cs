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

    public int IdleCount => _idle.Count;
    public int ActiveCount => _activeCount;
    public long RecycleCount => _recycleCount;

    /// <summary>
    /// Retrieves a ChunkNode from the pool, or creates a new one if empty.
    /// The returned node must be initialized with <see cref="ChunkNode.Initialize"/>.
    /// </summary>
    public ChunkNode Rent()
    {
        if (_idle.TryPop(out var node))
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
    public void Return(ChunkNode node)
    {
        node.ClearMesh();
        node.Visible = false;

        var parent = node.GetParent();
        parent?.RemoveChild(node);

        _idle.Push(node);
        _activeCount--;
        _recycleCount++;
    }
}
