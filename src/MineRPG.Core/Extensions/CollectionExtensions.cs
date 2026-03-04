using System.Runtime.CompilerServices;

namespace MineRPG.Core.Extensions;

public static class CollectionExtensions
{
    /// <summary>
    /// Removes and returns the last element. Cheaper than RemoveAt(0) on lists.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T PopLast<T>(this List<T> list)
    {
        var last = list.Count - 1;
        if (last < 0)
            throw new InvalidOperationException("Cannot pop from an empty list.");

        var item = list[last];
        list.RemoveAt(last);
        return item;
    }

    /// <summary>
    /// Fisher-Yates shuffle using a provided <see cref="Random"/> instance.
    /// Pass a seeded Random for deterministic behavior in worldgen.
    /// </summary>
    public static void Shuffle<T>(this List<T> list, Random random)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Add a range from a <see cref="ReadOnlySpan{T}"/> without allocating an intermediate array.
    /// </summary>
    public static void AddRange<T>(this List<T> list, ReadOnlySpan<T> span)
    {
        for (var i = 0; i < span.Length; i++)
            list.Add(span[i]);
    }

    /// <summary>
    /// Weighted random selection. Each item provides its own weight.
    /// Returns the selected item, or default if the list is empty.
    /// </summary>
    public static T? WeightedRandom<T>(this IReadOnlyList<T> items, Func<T, float> weightSelector, Random random)
    {
        if (items.Count == 0)
            return default;

        var total = 0f;
        for (var i = 0; i < items.Count; i++)
            total += weightSelector(items[i]);

        if (total <= 0f)
            throw new InvalidOperationException(
                $"WeightedRandom: total weight must be positive, got {total}.");

        var roll = (float)random.NextDouble() * total;
        var cumulative = 0f;

        for (var i = 0; i < items.Count; i++)
        {
            cumulative += weightSelector(items[i]);
            if (roll <= cumulative)
                return items[i];
        }

        return items[items.Count - 1];
    }

    /// <summary>
    /// Returns true and fills <paramref name="result"/> if the span is non-empty.
    /// </summary>
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
