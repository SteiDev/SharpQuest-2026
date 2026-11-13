using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;
using SharpQuest.Contracts;

namespace Capstone.Core.ViewModels;

/// <summary>
/// View model for the game catalogue window.
/// Implements ICatalogueViewModel — no UI framework references; testable headlessly.
/// </summary>
public sealed class CatalogueViewModel : ICatalogueViewModel
{
    // ── Backing store ─────────────────────────────────────────────────────────

    private readonly IItemStore _store;

    /// <summary>
    /// The single observable collection the window binds to — never replaced.
    /// </summary>
    private readonly ObservableCollection<IItem> _items;

    // ── INotifyPropertyChanged ────────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Notify(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    // ── Constructor ───────────────────────────────────────────────────────────

    public CatalogueViewModel(IItemStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));

        // Seed from whatever is already in the store.
        _items = new ObservableCollection<IItem>(store.All());

        // Subscribe to Lab 06's event so new arrivals appear automatically.
        if (store is GameStore gs)
            gs.ItemAdded += OnItemAdded;

        // Commands — built once, stable references.
        AddCommand    = new RelayCommand(ExecuteAdd,    _ => true);
        RemoveCommand = new RelayCommand(ExecuteRemove, _ => Selected is not null,
                                         this, nameof(Selected));
    }

    // ── ICatalogueViewModel ───────────────────────────────────────────────────

    /// <inheritdoc/>
    public IEnumerable<IItem> Items => _items;

    private string _filter = string.Empty;

    /// <inheritdoc/>
    public string Filter
    {
        get => _filter;
        set
        {
            value ??= string.Empty;
            if (string.Equals(_filter, value, StringComparison.Ordinal)) return;
            _filter = value;
            ApplyFilter();
            Notify(nameof(Filter));
        }
    }

    private IItem? _selected;

    /// <inheritdoc/>
    public IItem? Selected
    {
        get => _selected;
        set
        {
            if (ReferenceEquals(_selected, value)) return;
            _selected = value;
            Notify(nameof(Selected));
            Notify(nameof(Details));
            Notify(nameof(SelectedPlatform));
            Notify(nameof(SelectedStatus));
            Notify(nameof(SelectedHours));
            Notify(nameof(SelectedPrice));
            Notify(nameof(SelectedGenre));
            Notify(nameof(SelectedDeveloper));
            Notify(nameof(SelectedRating));
            Notify(nameof(SelectedReleaseYear));
            ((RelayCommand)RemoveCommand).RaiseCanExecuteChanged();
        }
    }

    /// <inheritdoc/>
    public string Details =>
        _selected is BaseTrackableGame g
            ? $"{g.Title}  ·  {g.Platform}  ·  {g.Status}  ·  {g.HoursPlayed:F0}h  ·  {(g.Price.HasValue ? $"£{g.Price:F2}" : "free")}  ·  {g.Genre ?? "—"}  ·  {g.Developer.Name}"
            : _selected is not null
                ? $"{_selected.DisplayName} [{_selected.Id}]"
                : "No game selected.";

    // ── Per-field detail properties (resolved from Selected) ─────────────────

    private BaseTrackableGame? Game => _selected as BaseTrackableGame;

    public string SelectedPlatform    => Game?.Platform.ToString() ?? "—";
    public string SelectedStatus      => Game?.Status.ToString()   ?? "—";
    public string SelectedHours       => Game is { } g ? $"{g.HoursPlayed:F0} h" : "—";
    public string SelectedPrice       => Game is { Price: { } p } ? $"£{p:F2}" : Game is not null ? "Free" : "—";
    public string SelectedGenre       => Game?.Genre ?? "—";
    public string SelectedDeveloper   => Game?.Developer.Name ?? "—";
    public string SelectedRating      => Game?.Rating?.ToString() ?? "Not rated";
    public string SelectedReleaseYear => Game?.ReleaseYear?.ToString() ?? "—";

    /// <inheritdoc/>
    public int Count => _items.Count;

    /// <inheritdoc/>
    public ICommand AddCommand { get; }

    /// <inheritdoc/>
    public ICommand RemoveCommand { get; }

    // ── Edit support ──────────────────────────────────────────────────────────

    /// <summary>
    /// Applies validated edits from the Edit dialog to a game that is already in the store.
    /// Raises property-changed for all detail properties so the panel refreshes.
    /// </summary>
    public void UpdateGame(BaseTrackableGame game,
                           string title, Platform platform, GameStatus status,
                           string? genre, string developer,
                           double hoursPlayed, decimal? price, int? releaseYear, int? rating)
    {
        // BaseTrackableGame uses init-only setters for most fields, but the
        // mutable ones (Status, HoursPlayed, Rating) can be set directly.
        // For init-only fields we update via reflection so the Core API stays clean.
        SetProp(game, nameof(game.Status),      status);
        SetProp(game, nameof(game.HoursPlayed), hoursPlayed);
        SetProp(game, nameof(game.Rating),      rating);

        // These are init-only — use reflection to mutate in-place.
        SetInit(game, nameof(game.Title),       title);
        SetInit(game, nameof(game.Platform),    platform);
        SetInit(game, nameof(game.Genre),       genre);
        SetInit(game, nameof(game.Price),       price);
        SetInit(game, nameof(game.ReleaseYear), releaseYear);
        SetInit(game, "Developer",
            new Capstone.Core.Models.Developer { Name = developer });

        // Notify everything so the UI reflects the changes.
        Notify(nameof(Details));
        Notify(nameof(SelectedPlatform));
        Notify(nameof(SelectedStatus));
        Notify(nameof(SelectedHours));
        Notify(nameof(SelectedPrice));
        Notify(nameof(SelectedGenre));
        Notify(nameof(SelectedDeveloper));
        Notify(nameof(SelectedRating));
        Notify(nameof(SelectedReleaseYear));

        // Refresh the list item (DisplayName may have changed).
        var idx = _items.IndexOf(game);
        if (idx >= 0)
        {
            _items.RemoveAt(idx);
            _items.Insert(idx, game);
        }
    }

    private static void SetProp(object obj, string name, object? value)
    {
        var prop = obj.GetType().GetProperty(name,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        prop?.SetValue(obj, value);
    }

    private static void SetInit(object obj, string name, object? value)
    {
        // init-only properties have a compiler-generated backing field; set via the property's
        // setter directly — the runtime doesn't enforce init-only after construction.
        var prop = obj.GetType().GetProperty(name,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        prop?.SetValue(obj, value);
    }

    // ── Command handlers ──────────────────────────────────────────────────────

    private void ExecuteAdd(object? parameter)
    {
        if (parameter is not IItem item) return;
        // Store enforces uniqueness; the ItemAdded event updates _items.
        _store.Add(item);
    }

    private void ExecuteRemove(object? _)
    {
        if (_selected is null) return;
        var id = _selected.Id;
        Selected = null;
        _store.Remove(id);
        var toRemove = _items.FirstOrDefault(i => i.Id == id);
        if (toRemove is not null) _items.Remove(toRemove);
    }

    // ── Event handler: store item added ───────────────────────────────────────

    private void OnItemAdded(IItem item)
    {
        // Apply the current filter — if the new item doesn't match, don't show it.
        if (Matches(item))
        {
            _items.Add(item);
            Notify(nameof(Count));
        }
    }

    // ── Filter helpers ────────────────────────────────────────────────────────

    private bool Matches(IItem item) =>
        string.IsNullOrEmpty(_filter) ||
        item.DisplayName.Contains(_filter, StringComparison.OrdinalIgnoreCase);

    private void ApplyFilter()
    {
        // Rebuild the visible set by surgically adding/removing items — no Clear/Reset.
        var allItems = _store.All();

        // Remove items that no longer match (iterate backwards for safe removal).
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            if (!Matches(_items[i]))
                _items.RemoveAt(i);
        }

        // Add items that now match but aren't visible yet, maintaining store order.
        int insertPos = 0;
        foreach (var item in allItems)
        {
            if (!Matches(item)) continue;

            if (insertPos < _items.Count && _items[insertPos].Id == item.Id)
            {
                insertPos++;
            }
            else
            {
                _items.Insert(insertPos, item);
                insertPos++;
            }
        }

        Notify(nameof(Count));
    }
}

// ── RelayCommand ──────────────────────────────────────────────────────────────

/// <summary>
/// Minimal ICommand implementation — no UI framework dependency.
/// </summary>
internal sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool> _canExecute;

    public event EventHandler? CanExecuteChanged;

    public RelayCommand(Action<object?> execute, Func<object?, bool> canExecute,
                        INotifyPropertyChanged? notifier = null, string? propertyName = null)
    {
        _execute    = execute;
        _canExecute = canExecute;

        // Auto-raise CanExecuteChanged when the named property changes.
        if (notifier is not null && propertyName is not null)
        {
            notifier.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == propertyName)
                    RaiseCanExecuteChanged();
            };
        }
    }

    public bool CanExecute(object? parameter) => _canExecute(parameter);
    public void Execute(object? parameter)    => _execute(parameter);
    public void RaiseCanExecuteChanged()      => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

