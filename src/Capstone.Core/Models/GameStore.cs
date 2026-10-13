using SharpQuest.Contracts;

namespace Capstone.Core.Models;

/// <summary>
/// An in-memory store of IItem objects, maintaining insertion order.
/// </summary>
public class GameStore : IItemStore
{
    // Use a list to preserve insertion order; a dict for O(1) lookup.
    private readonly List<IItem> _items = new();
    private readonly Dictionary<string, IItem> _byId = new(StringComparer.Ordinal);

    public int Count => _items.Count;

    public void Add(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_byId.ContainsKey(item.Id))
            throw new ArgumentException($"An item with Id '{item.Id}' is already in the store.", nameof(item));

        _items.Add(item);
        _byId[item.Id] = item;
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
}
