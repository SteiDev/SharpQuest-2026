using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public class Lab05Wiring : ILab05Wiring
{
    private readonly Lab02Wiring _lab02 = new();

    public IItemStore CreateStore() => new GameStore();

    public IItem CreateSample(int seed) => _lab02.CreateSample(seed);

    public Type CollectionType => typeof(GameStore);

    public Type GenericDefinition => typeof(RecentlyPlayed<>);

    public IItem At(IItemStore store, int index)
    {
        if (store is GameStore gs) return gs[index];
        throw new InvalidOperationException("Store is not a GameStore.");
    }

    public IItem? ById(IItemStore store, string id)
    {
        if (store is GameStore gs) return gs[id];
        return store.FindById(id);
    }

    public IReadOnlyList<IItem> EachInOrder(IItemStore store)
    {
        var result = new List<IItem>();
        foreach (var item in (GameStore)store)
            result.Add(item);
        return result.AsReadOnly();
    }

    public IEnumerable<IItem> Matching(IItemStore store, string text)
    {
        if (store is GameStore gs)
            return gs.Matching(text);
        return store.All().Where(i => i.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<T> Recent<T>(int capacity, params T[] pushed)
    {
        var recent = new RecentlyPlayed<T>(capacity);
        foreach (var item in pushed)
            recent.Push(item);
        return recent.ToList();
    }
}
