namespace Kep.Explorer;

/// <summary>
/// Contains a set of extension methods for <see cref="IEnumerable{T}"/>.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Groups adjacent items based on a key. Similar to
    /// <see cref="Enumerable.GroupBy{TSource,TKey,TResult}(IEnumerable{TSource}, Func{TSource,TKey}, Func{TKey, IEnumerable{TSource}, TResult})"/>,
    /// except that multiple groups with the same key can be returned and this method has no memory beyond the current
    /// group.
    /// </summary>
    public static IEnumerable<TResult> GroupByAdjacent<TSource, TKey, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TKey, IReadOnlyList<TSource>, TResult> resultSelector)
    {
        List<TSource> group = [];
        TKey? key = default;
        foreach (var item in source)
        {
            var currKey = keySelector(item);
            if (group is [])
            {
                key = currKey;
                group.Add(item);
                continue;
            }

            if (Equals(key, currKey))
            {
                group.Add(item);
                continue;
            }

            yield return resultSelector(key!, group);

            key = currKey;
            group = [item];
        }
        
        if (group is not [])
            yield return resultSelector(key!, group);
    }


    /// <summary>
    /// Returns a single <see cref="Stats"/> that represents the <paramref name="source"/>.
    /// </summary>
    public static Stats Stats<T>(this IReadOnlyCollection<T> source, Func<T, double> selector)
    {
        var min = source.Min(selector);
        var max = source.Max(selector);
        var mean = source.Average(selector);
        var variance = source
            .Select(selector)
            .Average(value => (value - mean) * (value - mean));
        
        return new Stats(min, max, mean, variance);
    }
}
