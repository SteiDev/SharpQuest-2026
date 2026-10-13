using SharpQuest.Contracts;

namespace Capstone.Core.Models;

/// <summary>
/// The player's personal game library — a collection of all owned games.
/// Session 5 will grow this into a custom indexed collection.
/// </summary>
public class Library
{
    private readonly GameStore _store = new();

    public int Count => _store.Count;

    public void Add(Game game) => _store.Add(game);

    public Game? FindById(string id) => _store.FindById(id) as Game;

    public IReadOnlyList<IItem> All() => _store.All();

    public bool Remove(string id) => _store.Remove(id);
}