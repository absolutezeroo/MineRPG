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
    private readonly List<TValue> _orderedValues =
    [
    ];

    public int Count => _entries.Count;

    public void Register(TKey key, TValue value)
    {
        if (!_entries.TryAdd(key, value))
            throw new InvalidOperationException(
                $"Key '{key}' is already registered in {typeof(TValue).Name} registry.");

        _orderedValues.Add(value);
    }

    public TValue Get(TKey key)
    {
        if (!_entries.TryGetValue(key, out var value))
            throw new KeyNotFoundException(
                $"Key '{key}' not found in {typeof(TValue).Name} registry.");

        return value;
    }

    public bool TryGet(TKey key, out TValue value)
        => _entries.TryGetValue(key, out value!);

    public IReadOnlyList<TValue> GetAll() => _orderedValues;

    public bool Contains(TKey key) => _entries.ContainsKey(key);
}
