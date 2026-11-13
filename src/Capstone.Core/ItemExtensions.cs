using SharpQuest.Contracts;

namespace Capstone.Core;

/// <summary>
/// Extension methods on sequences of items.
/// </summary>
public static class ItemExtensions
{
    /// <summary>
    /// The <paramref name="n"/> items with the longest display names, in length order
    /// (longest first, ties broken by Id).
    /// </summary>
    public static IEnumerable<IItem> Longest(this IEnumerable<IItem> items, int n)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (n < 0) throw new ArgumentOutOfRangeException(nameof(n), "n must be non-negative.");

        return items
            .OrderByDescending(i => i.DisplayName.Length)
            .ThenBy(i => i.Id, StringComparer.Ordinal)
            .Take(n);
    }
}

