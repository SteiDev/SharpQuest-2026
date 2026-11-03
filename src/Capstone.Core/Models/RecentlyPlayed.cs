namespace Capstone.Core.Models;

/// <summary>
/// A fixed-capacity buffer of recently used values, newest-first, no duplicates.
/// </summary>
public class RecentlyPlayed<T>
{
    private readonly LinkedList<T> _items = new();
    private readonly int _capacity;

    public RecentlyPlayed(int capacity)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be at least 1.");
        _capacity = capacity;
    }

    public void Push(T item)
    {
        var comparer = EqualityComparer<T>.Default;
        var node = _items.First;
        while (node is not null)
        {
            if (comparer.Equals(node.Value, item))
            {
                _items.Remove(node);
                break;
            }
            node = node.Next;
        }
        _items.AddFirst(item);
        while (_items.Count > _capacity)
            _items.RemoveLast();
    }

    public IReadOnlyList<T> ToList() => _items.ToList().AsReadOnly();
}
