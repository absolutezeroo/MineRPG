using System;
using System.Runtime.CompilerServices;

namespace MineRPG.Core.Diagnostics;

/// <summary>
/// Fixed-size circular buffer for time-series data. Pre-allocated, zero-resize,
/// zero-allocation after construction. Supports efficient percentile computation.
/// </summary>
/// <typeparam name="T">The element type (must be unmanaged for stack-friendly operations).</typeparam>
public sealed class RingBuffer<T> where T : unmanaged
{
    private readonly T[] _buffer;
    private int _head;

    /// <summary>
    /// Creates a ring buffer with the specified capacity.
    /// </summary>
    /// <param name="capacity">The fixed capacity. Must be greater than zero.</param>
    public RingBuffer(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
        }

        _buffer = new T[capacity];
    }

    /// <summary>
    /// The fixed capacity of the buffer.
    /// </summary>
    public int Capacity => _buffer.Length;

    /// <summary>
    /// The number of elements currently stored. Never exceeds <see cref="Capacity"/>.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Whether the buffer has been filled at least once.
    /// </summary>
    public bool IsFull => Count == _buffer.Length;

    /// <summary>
    /// Pushes a value into the buffer, overwriting the oldest if full.
    /// </summary>
    /// <param name="value">The value to push.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Push(T value)
    {
        _buffer[_head] = value;
        _head = (_head + 1) % _buffer.Length;

        if (Count < _buffer.Length)
        {
            Count++;
        }
    }

    /// <summary>
    /// Returns the most recently pushed value.
    /// </summary>
    /// <returns>The latest value.</returns>
    public T PeekLatest()
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("Buffer is empty.");
        }

        int index = (_head - 1 + _buffer.Length) % _buffer.Length;
        return _buffer[index];
    }

    /// <summary>
    /// Returns the value at the given age (0 = most recent, 1 = second most recent, etc.).
    /// </summary>
    /// <param name="age">How many entries back from the most recent. Must be less than <see cref="Count"/>.</param>
    /// <returns>The value at the given age.</returns>
    public T PeekAt(int age)
    {
        if (age < 0 || age >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(age), age, "Age must be in [0, Count).");
        }

        int index = (_head - 1 - age + _buffer.Length * 2) % _buffer.Length;
        return _buffer[index];
    }

    /// <summary>
    /// Copies all stored values into the destination span in chronological order
    /// (oldest first). Returns the number of elements copied.
    /// </summary>
    /// <param name="destination">The destination span. Must be at least <see cref="Count"/> elements.</param>
    /// <returns>The number of elements copied.</returns>
    public int CopyTo(Span<T> destination)
    {
        if (destination.Length < Count)
        {
            throw new ArgumentException("Destination span is too small.", nameof(destination));
        }

        if (Count == 0)
        {
            return 0;
        }

        int start = (_head - Count + _buffer.Length) % _buffer.Length;

        if (start + Count <= _buffer.Length)
        {
            _buffer.AsSpan(start, Count).CopyTo(destination);
        }
        else
        {
            int firstPart = _buffer.Length - start;
            _buffer.AsSpan(start, firstPart).CopyTo(destination);
            _buffer.AsSpan(0, Count - firstPart).CopyTo(destination.Slice(firstPart));
        }

        return Count;
    }

    /// <summary>
    /// Resets the buffer to empty without deallocating.
    /// </summary>
    public void Clear()
    {
        _head = 0;
        Count = 0;
    }
}
