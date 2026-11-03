using System.Collections;
using SharpQuest.Contracts;

namespace Capstone.Core.Models;

/// <summary>
/// An in-memory store of IItem objects, maintaining insertion order.
/// </summary>
public class GameStore : IItemStore, IEnumerable<IItem>
{
    // Use a list to preserve insertion order; a dict for O(1) lookup.
    private readonly List<IItem> _items = new();
    private readonly Dictionary<string, IItem> _byId = new(StringComparer.Ordinal);

    public event Action<IItem>? ItemAdded;

    public int Count => _items.Count;

    public void Add(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_byId.ContainsKey(item.Id))
            throw new ArgumentException($"An item with Id '{item.Id}' is already in the store.", nameof(item));

        _items.Add(item);
        _byId[item.Id] = item;
        ItemAdded?.Invoke(item);
    }

    public IItem? FindById(string id) =>
        _byId.TryGetValue(id, out var found) ? found : null;

    public IReadOnlyList<IItem> All() => _items.ToList().AsReadOnly();

    public bool Remove(string id)
    {
        if (!_byId.TryGetValue(id, out var item)) return false;
        _byId.Remove(id);
        _items.Remove(item);
        return true;
    }

    // Lab 05: indexer by position
    public IItem this[int index]
    {
        get
        {
            if (index < 0 || index >= _items.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _items[index];
        }
    }

    // Lab 05: indexer by id
    public IItem? this[string id] => FindById(id);

    // Lab 05: IEnumerable<IItem> with yield return
    public IEnumerator<IItem> GetEnumerator()
    {
        foreach (var item in _items)
            yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // Lab 05: lazy matching iterator
    public IEnumerable<IItem> Matching(string text)
    {
        foreach (var item in _items)
        {
            if (item.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase))
                yield return item;
        }
    }
}
