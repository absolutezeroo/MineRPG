using System;
using System.Collections.Generic;

namespace MineRPG.Core.Registry;

/// <summary>
/// Default implementation of <see cref="IRegistry{TKey,TValue}"/>.
/// Not thread-safe — registries are populated at startup on the main thread,
/// then only read during gameplay.
/// </summary>
public sealed class Registry<TKey, TValue> : IRegistry<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _entries = new();
    private readonly List<TValue> _orderedValues = new();

    /// <inheritdoc />
    public int Count => _entries.Count;

    /// <inheritdoc />
    public void Register(TKey key, TValue value)
    {
        if (!_entries.TryAdd(key, value))
        {
            throw new InvalidOperationException(
                $"Key '{key}' is already registered in {typeof(TValue).Name} registry.");
        }

        _orderedValues.Add(value);
    }

    /// <inheritdoc />
    public TValue Get(TKey key)
    {
        if (!_entries.TryGetValue(key, out TValue? value))
        {
            throw new KeyNotFoundException(
                $"Key '{key}' not found in {typeof(TValue).Name} registry.");
        }

        return value;
    }

    /// <inheritdoc />
    public bool TryGet(TKey key, out TValue value) => _entries.TryGetValue(key, out value!);

    /// <inheritdoc />
    public IReadOnlyList<TValue> GetAll() => _orderedValues;

    /// <inheritdoc />
    public bool Contains(TKey key) => _entries.ContainsKey(key);
}
