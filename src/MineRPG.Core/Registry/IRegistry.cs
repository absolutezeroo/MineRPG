using System.Collections.Generic;

namespace MineRPG.Core.Registry;

/// <summary>
/// Generic data-driven registry. The backbone for blocks, items, mobs, skills, etc.
/// Keyed registration is fail-fast - duplicate keys throw immediately at load time.
/// </summary>
/// <typeparam name="TKey">The type of the registry key.</typeparam>
/// <typeparam name="TValue">The type of the registered values.</typeparam>
public interface IRegistry<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    /// The number of entries currently registered.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Register <paramref name="value"/> under <paramref name="key"/>.
    /// Throws <see cref="InvalidOperationException"/> if the key already exists.
    /// </summary>
    /// <param name="key">The key to register under.</param>
    /// <param name="value">The value to register.</param>
    void Register(TKey key, TValue value);

    /// <summary>
    /// Retrieve the value for <paramref name="key"/>.
    /// Throws <see cref="KeyNotFoundException"/> if not found.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The registered value.</returns>
    TValue Get(TKey key);

    /// <summary>
    /// Try to retrieve the value for <paramref name="key"/> without throwing.
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">The value if found.</param>
    /// <returns>True if the key was found.</returns>
    bool TryGet(TKey key, out TValue value);

    /// <summary>
    /// All registered values in insertion order.
    /// </summary>
    /// <returns>A read-only list of all registered values.</returns>
    IReadOnlyList<TValue> GetAll();

    /// <summary>
    /// Check whether a key has been registered.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key exists in the registry.</returns>
    bool Contains(TKey key);

    /// <summary>
    /// Whether the registry has been frozen (no further registrations allowed).
    /// </summary>
    bool IsFrozen { get; }

    /// <summary>
    /// Freezes the registry, preventing any further calls to <see cref="Register"/>.
    /// </summary>
    void Freeze();
}
