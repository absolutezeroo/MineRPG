using System;
using System.Collections.Concurrent;
using System.Threading;

namespace MineRPG.Core.Pooling;

/// <summary>
/// Thread-safe object pool backed by <see cref="ConcurrentBag{T}"/>.
/// An optional reset action is invoked on Return before the object is stored.
/// When the max capacity is reached, returned items are silently dropped.
/// </summary>
public sealed class ObjectPool<T> : IObjectPool<T>
    where T : class
{
    private readonly ConcurrentBag<T> _bag = [];
    private readonly Func<T> _factory;
    private readonly Action<T>? _reset;
    private int _idleCount;

    /// <summary>
    /// Initializes a new instance of <see cref="ObjectPool{T}"/>.
    /// </summary>
    /// <param name="factory">Factory function to create new instances when the pool is empty.</param>
    /// <param name="reset">Optional action to reset an object before returning it to the pool.</param>
    /// <param name="maxCapacity">Maximum number of idle objects to retain in the pool.</param>
    public ObjectPool(Func<T> factory, Action<T>? reset = null, int maxCapacity = int.MaxValue)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _reset = reset;

        if (maxCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCapacity), "Max capacity must be greater than zero.");
        }

        MaxCapacity = maxCapacity;
    }

    /// <summary>
    /// Number of idle objects currently in the pool.
    /// </summary>
    public int IdleCount => _idleCount;

    /// <summary>
    /// Maximum number of idle objects the pool will retain.
    /// </summary>
    public int MaxCapacity { get; }

    /// <inheritdoc />
    public T Rent()
    {
        if (!_bag.TryTake(out T? item))
        {
            return _factory();
        }

        Interlocked.Decrement(ref _idleCount);
        return item;
    }

    /// <inheritdoc />
    public void Return(T item)
    {
        _reset?.Invoke(item);

        // Soft cap: approximate check avoids locking. In highly concurrent
        // scenarios the pool may briefly exceed maxCapacity - acceptable.
        if (_idleCount >= MaxCapacity)
        {
            return;
        }

        _bag.Add(item);
        Interlocked.Increment(ref _idleCount);
    }

    /// <inheritdoc />
    public void PreAllocate(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        for (int i = 0; i < count; i++)
        {
            if (_idleCount >= MaxCapacity)
            {
                break;
            }

            T item = _factory();
            _bag.Add(item);
            Interlocked.Increment(ref _idleCount);
        }
    }
}
