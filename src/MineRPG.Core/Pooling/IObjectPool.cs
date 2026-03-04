namespace MineRPG.Core.Pooling;

/// <summary>
/// Reusable object pool. Avoids GC pressure in hot paths (projectiles,
/// particles, chunk job structs, temporary lists).
/// </summary>
/// <typeparam name="T">The type of objects managed by this pool.</typeparam>
public interface IObjectPool<T> where T : class
{
    /// <summary>
    /// Number of idle (available) objects currently in the pool.
    /// </summary>
    int IdleCount { get; }

    /// <summary>
    /// Retrieve an object from the pool, or create a new one if empty.
    /// The returned object may contain leftover state — callers must reset it.
    /// </summary>
    /// <returns>An object from the pool or a newly created one.</returns>
    T Rent();

    /// <summary>
    /// Return <paramref name="item"/> to the pool for reuse.
    /// The caller must not use <paramref name="item"/> after returning it.
    /// </summary>
    /// <param name="item">The item to return to the pool.</param>
    void Return(T item);
}
