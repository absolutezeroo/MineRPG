using System.Collections.Concurrent;

namespace MineRPG.Core.Pooling;

/// <summary>
/// Thread-safe object pool backed by <see cref="ConcurrentBag{T}"/>.
/// An optional reset action is invoked on Return before the object is stored.
/// When <paramref name="maxCapacity"/> is reached, returned items are silently dropped.
/// </summary>
public sealed class ObjectPool<T> : IObjectPool<T>
    where T : class
{
    private readonly ConcurrentBag<T> _bag = [];
    private readonly Func<T> _factory;
    private readonly Action<T>? _reset;
    private readonly int _maxCapacity;
    private int _idleCount;

    public ObjectPool(Func<T> factory, Action<T>? reset = null, int maxCapacity = int.MaxValue)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _reset = reset;

        if (maxCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Max capacity must be greater than zero.");

        _maxCapacity = maxCapacity;
    }

    public int IdleCount => _idleCount;
    public int MaxCapacity => _maxCapacity;

    public T Rent()
    {
        if (_bag.TryTake(out var item))
        {
            Interlocked.Decrement(ref _idleCount);
            return item;
        }

        return _factory();
    }

    public void Return(T item)
    {
        _reset?.Invoke(item);

        // Soft cap: approximate check avoids locking. In highly concurrent
        // scenarios the pool may briefly exceed maxCapacity — acceptable.
        if (_idleCount >= _maxCapacity)
            return;

        _bag.Add(item);
        Interlocked.Increment(ref _idleCount);
    }
}
