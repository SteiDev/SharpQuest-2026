using Capstone.Core.Models;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public class Lab03Wiring : ILab03Wiring
{
    private readonly Lab02Wiring _lab02 = new();

    public IItemStore CreateStore() => _lab02.CreateStore();

    public IItem CreateSample(int seed) => _lab02.CreateSample(seed);

    public IReadOnlyList<IItem> Search(IItemStore store, string query)
    {
        var matcher = SearchMatcher.Create();
        return store.All()
            .Where(item => matcher.Matches(item.DisplayName, query))
            .ToList()
            .AsReadOnly();
    }
}
