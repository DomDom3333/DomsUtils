using System.Diagnostics.CodeAnalysis;

namespace DomsUtils.Tooling.ExtensionMethods;

/// <summary>
/// Provides extension methods for the <see cref="IEnumerable{T}"/> interface, offering enhanced functionality and utility for common operations.
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    /// Checks if an enumerable is null or empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="source">The enumerable to check.</param>
    /// <returns>
    /// true if the enumerable is null or does not contain any elements; otherwise, false.
    /// </returns>
    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IEnumerable<T>? source)
    {
        if (source == null) return true;
        // Avoids allocating an enumerator if using a collection
        // ICollection is the broadest (covers List, HashSet, arrays, etc.)
        if (source is ICollection<T> col) return col.Count == 0;
        if (source is System.Collections.ICollection ncol) return ncol.Count == 0;
        // Fallback: enumerator check for emptiness only
        using var e = source.GetEnumerator();
        return !e.MoveNext();
    }

    /// <summary>
    /// Checks if an enumerable has any items (is not null and has any elements).
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="source">The enumerable to check.</param>
    /// <returns>
    /// true if the enumerable is not null and contains at least one element; otherwise, false.
    /// </returns>
    public static bool HasItems<T>([NotNullWhen(true)] this IEnumerable<T>? source)
    {
        if (source == null) return false;
        if (source is ICollection<T> col) return col.Count > 0;
        if (source is System.Collections.ICollection ncol) return ncol.Count > 0;
        using var e = source.GetEnumerator();
        return e.MoveNext();
    }

    /// <summary>
    /// Executes the specified action on each element of the enumerable.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="source">The enumerable whose elements the action will be executed on.</param>
    /// <param name="action">The action to perform on each element of the enumerable.</param>
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);
        foreach (var item in source)
            action(item);
    }

    /// <summary>
    /// Returns a new enumerable excluding null elements for reference types.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable, constrained to reference types.</typeparam>
    /// <param name="source">The enumerable to filter.</param>
    /// <returns>
    /// An enumerable containing only non-null elements, or an empty enumerable if the input is null.
    /// </returns>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?>? source) where T : class
    {
        if (source is null) yield break;
        foreach (var item in source)
            if (item != null) yield return item;
    }

    /// <summary>
    /// Returns a new enumerable excluding null elements for reference types.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="source">The enumerable to filter.</param>
    /// <returns>
    /// An enumerable containing all non-null elements from the source enumerable.
    /// </returns>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?>? source) where T : struct
    {
        if (source is null) yield break;
        foreach (var item in source)
            if (item.HasValue) yield return item.Value;
    }

    /// <summary>
    /// Returns the first element of the sequence or a default value if the sequence is null or empty.
    /// Optimized to avoid overhead if the source is an <see cref="IList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="source">The sequence to retrieve the first element from.</param>
    /// <returns>
    /// The first element of the sequence if it is not null or empty; otherwise, the default value for type <typeparamref name="T"/>.
    /// </returns>
    public static T? FirstOrDefaultSafe<T>(this IEnumerable<T>? source)
    {
        if (source == null) return default;
        // Optimize for IList
        if (source is IList<T> list)
            return list.Count > 0 ? list[0] : default;
        using var e = source.GetEnumerator();
        return e.MoveNext() ? e.Current : default;
    }

    /// <summary>
    /// Returns the last element of the enumerable or a default value if the sequence is null or empty.
    /// Optimized for collections implementing <see cref="IList{T}"/> to provide better performance.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="source">The enumerable to retrieve the last element from.</param>
    /// <returns>
    /// The last element in the enumerable if it contains any elements; otherwise, the default value for type <typeparamref name="T"/>.
    /// </returns>
    public static T? LastOrDefaultSafe<T>(this IEnumerable<T>? source)
    {
        if (source == null) return default;
        // Optimize for IList
        if (source is IList<T> list)
            return list.Count > 0 ? list[^1] : default;
        // Fallback: normal approach
        T? result = default;
        bool found = false;
        foreach (var item in source)
        {
            result = item;
            found = true;
        }
        return found ? result : default;
    }

    /// <summary>
    /// Converts an enumerable to a HashSet, or returns an empty HashSet if the enumerable is null.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="source">The enumerable to convert to a HashSet. Can be null.</param>
    /// <returns>
    /// A HashSet containing the elements of the enumerable, or an empty HashSet if the enumerable is null.
    /// </returns>
    public static HashSet<T> ToHashSetSafe<T>(this IEnumerable<T>? source)
        => source == null ? [] : source as HashSet<T> ?? [..source];

    /// <summary>
    /// Returns the distinct elements of an enumerable or an empty enumerable if the source is null.
    /// </summary>
    /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
    /// <param name="source">The enumerable to filter for distinct elements. Can be null.</param>
    /// <returns>
    /// An enumerable containing only the distinct elements of the source enumerable,
    /// or an empty enumerable if the source is null.
    /// </returns>
    public static IEnumerable<T> DistinctSafe<T>(this IEnumerable<T>? source)
        => source == null ? [] : source.Distinct();
}