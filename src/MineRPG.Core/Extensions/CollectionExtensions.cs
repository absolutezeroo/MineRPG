using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MineRPG.Core.Extensions;

/// <summary>
/// Extension methods for collections used throughout the engine.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Removes and returns the last element. Cheaper than RemoveAt(0) on lists.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="list">The list to pop from.</param>
    /// <returns>The last element that was removed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T PopLast<T>(this List<T> list)
    {
        int last = list.Count - 1;

        if (last < 0)
        {
            throw new InvalidOperationException("Cannot pop from an empty list.");
        }

        T item = list[last];
        list.RemoveAt(last);
        return item;
    }

    /// <summary>
    /// Fisher-Yates shuffle using a provided <see cref="Random"/> instance.
    /// Pass a seeded Random for deterministic behavior in worldgen.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="list">The list to shuffle in place.</param>
    /// <param name="random">The random number generator to use.</param>
    public static void Shuffle<T>(this List<T> list, Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Add a range from a <see cref="ReadOnlySpan{T}"/> without allocating an intermediate array.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="list">The list to add elements to.</param>
    /// <param name="span">The span of elements to add.</param>
    public static void AddRange<T>(this List<T> list, ReadOnlySpan<T> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            list.Add(span[i]);
        }
    }

    /// <summary>
    /// Weighted random selection. Each item provides its own weight.
    /// Returns the selected item, or default if the list is empty.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="items">The items to select from.</param>
    /// <param name="weightSelector">Function that returns the weight for each item.</param>
    /// <param name="random">The random number generator to use.</param>
    /// <returns>The randomly selected item, or default if the list is empty.</returns>
    public static T? WeightedRandom<T>(this IReadOnlyList<T> items, Func<T, float> weightSelector, Random random)
    {
        if (items.Count == 0)
        {
            return default;
        }

        float total = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            total += weightSelector(items[i]);
        }

        if (total <= 0f)
        {
            throw new InvalidOperationException(
                $"WeightedRandom: total weight must be positive, got {total}.");
        }

        float roll = (float)random.NextDouble() * total;
        float cumulative = 0f;

        for (int i = 0; i < items.Count; i++)
        {
            cumulative += weightSelector(items[i]);

            if (roll <= cumulative)
            {
                return items[i];
            }
        }

        return items[items.Count - 1];
    }

    /// <summary>
    /// Returns true and fills <paramref name="result"/> if the span is non-empty.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="span">The span to check.</param>
    /// <param name="result">The first element if the span is non-empty.</param>
    /// <returns>True if the span contained at least one element.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetFirst<T>(this ReadOnlySpan<T> span, out T result)
    {
        if (span.Length > 0)
        {
            result = span[0];
            return true;
        }

        result = default!;
        return false;
    }
}
