namespace MineRPG.Core.Registry;

/// <summary>
/// Generic data-driven registry. The backbone for blocks, items, mobs, skills, etc.
/// Keyed registration is fail-fast — duplicate keys throw immediately at load time.
/// </summary>
public interface IRegistry<TKey, TValue>
    where TKey : notnull
{
    int Count { get; }

    /// <summary>
    /// Register <paramref name="value"/> under <paramref name="key"/>.
    /// Throws <see cref="InvalidOperationException"/> if the key already exists.
    /// </summary>
    void Register(TKey key, TValue value);

    /// <summary>
    /// Retrieve the value for <paramref name="key"/>.
    /// Throws <see cref="KeyNotFoundException"/> if not found.
    /// </summary>
    TValue Get(TKey key);

    bool TryGet(TKey key, out TValue value);

    /// <summary>All registered values in insertion order.</summary>
    IReadOnlyList<TValue> GetAll();

    bool Contains(TKey key);
}
