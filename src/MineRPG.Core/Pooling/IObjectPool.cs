namespace MineRPG.Core.Pooling;

/// <summary>
/// Reusable object pool. Avoids GC pressure in hot paths (projectiles,
/// particles, chunk job structs, temporary lists).
/// </summary>
public interface IObjectPool<T> where T : class
{
    int IdleCount { get; }

    /// <summary>
    /// Retrieve an object from the pool, or create a new one if empty.
    /// The returned object may contain leftover state — callers must reset it.
    /// </summary>
    T Rent();

    /// <summary>
    /// Return <paramref name="item"/> to the pool for reuse.
    /// The caller must not use <paramref name="item"/> after returning it.
    /// </summary>
    void Return(T item);
}
