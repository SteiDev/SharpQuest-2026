using Capstone.Core.Models;
using Capstone.Core.ViewModels;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

/// <summary>
/// Lab 09 wiring: tells the contract tests how to build our view model.
/// </summary>
public class Lab09Wiring : ILab09Wiring
{
    private readonly Lab02Wiring _lab02 = new();

    public IItemStore CreateStore() => new GameStore();

    public IItem CreateSample(int seed) => _lab02.CreateSample(seed);

    public ICatalogueViewModel CreateViewModel(IItemStore store) =>
        new CatalogueViewModel(store);
}

