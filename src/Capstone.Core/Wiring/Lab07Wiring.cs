using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public class Lab07Wiring : ILab07Wiring
{
    private readonly Lab02Wiring _lab02 = new();

    public IItemStore CreateStore() => new GameStore();

    public IItem CreateSample(int seed) => _lab02.CreateSample(seed);

    // ── Deferred projection — queries the store itself, not a snapshot ─────────
    // The contract requires the query to pick up items added AFTER Names() is called,
    // and to survive being enumerated twice. Querying the GameStore (which is IEnumerable<IItem>)
    // directly (rather than calling All()) satisfies both requirements.
    public IEnumerable<string> Names(IItemStore store)
    {
        var gs = (GameStore)store;
        return gs.Select(i => i.DisplayName);
    }

    // ── Sorted: name order (OrdinalIgnoreCase, tie-break by Id) ──────────────
    public IReadOnlyList<IItem> Sorted(IEnumerable<IItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return items
            .OrderBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.Id, StringComparer.Ordinal)
            .ToList()
            .AsReadOnly();
    }

    // ── Page: skip/take in name order ─────────────────────────────────────────
    public IReadOnlyList<IItem> Page(IEnumerable<IItem> items, int skip, int take)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip), "skip must be non-negative.");
        if (take < 0) throw new ArgumentOutOfRangeException(nameof(take), "take must be non-negative.");

        return items
            .OrderBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.Id, StringComparer.Ordinal)
            .Skip(skip)
            .Take(take)
            .ToList()
            .AsReadOnly();
    }

    // ── Longest: delegates to the extension method ────────────────────────────
    public IReadOnlyList<IItem> Longest(IEnumerable<IItem> items, int n)
        => items.Longest(n).ToList().AsReadOnly();

    // ── Extension host ────────────────────────────────────────────────────────
    public Type ExtensionHost => typeof(ItemExtensions);

    // ── Summary ───────────────────────────────────────────────────────────────
    public StoreStats Summary(IEnumerable<IItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var list = items.ToList();   // enumerate once

        if (list.Count == 0)
            return new StoreStats(0, 0, null, 0);

        int count            = list.Count;
        int distinctInitials = list.Select(i => char.ToUpperInvariant(i.DisplayName[0])).Distinct().Count();
        string? longestName  = list.Longest(1).FirstOrDefault()?.DisplayName;
        double  avgLength    = list.Average(i => (double)i.DisplayName.Length);

        return new StoreStats(count, distinctInitials, longestName, avgLength);
    }
}

