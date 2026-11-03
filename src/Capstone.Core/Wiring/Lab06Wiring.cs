using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public class Lab06Wiring : ILab06Wiring
{
    private readonly Lab02Wiring _lab02 = new();

    public IItemStore CreateStore() => new GameStore();

    public IItem CreateSample(int seed) => _lab02.CreateSample(seed);

    public Type PublisherType => typeof(GameStore);

    public Action Subscribe(IItemStore store, Action<IItem> handler)
    {
        var gs = (GameStore)store;
        gs.ItemAdded += handler;
        return () => gs.ItemAdded -= handler;
    }

    public IReadOnlyList<IItem> Where(IItemStore store, Func<IItem, bool> keep)
    {
        ArgumentNullException.ThrowIfNull(keep);
        return store.All().Where(keep).ToList().AsReadOnly();
    }

    public IReadOnlyList<string> Map(IItemStore store, Func<IItem, string> describe)
    {
        ArgumentNullException.ThrowIfNull(describe);
        return store.All().Select(describe).ToList().AsReadOnly();
    }

    public void ForEach(IItemStore store, Action<IItem> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        foreach (var item in store.All())
            action(item);
    }
}
