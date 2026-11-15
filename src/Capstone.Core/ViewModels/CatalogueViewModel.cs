using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;
using Capstone.Core.Models.Storage;
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

    /// <summary>Optional SQLite repository — wired by the UI host for persistence.</summary>
    private readonly GameRepository? _repo;

    /// <summary>
    /// The single observable collection the window binds to — never replaced.
    /// </summary>
    private readonly ObservableCollection<IItem> _items;

    // ── INotifyPropertyChanged ────────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Notify(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    // ── Constructor ───────────────────────────────────────────────────────────

    /// <param name="store">In-memory item store (required).</param>
    /// <param name="repo">SQLite repository for persistence (optional; pass null in tests).</param>
    public CatalogueViewModel(IItemStore store, GameRepository? repo = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _repo  = repo;

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

    // ── Active filter sets ────────────────────────────────────────────────────

    /// <summary>When non-empty, only games whose Status is in this set are shown.</summary>
    public HashSet<GameStatus> ActiveStatuses { get; } = new();

    /// <summary>When non-empty, only games whose Platform is in this set are shown.</summary>
    public HashSet<Platform> ActivePlatforms { get; } = new();

    public string SelectedGenreFilter { get; set; } = "All";
    public string SelectedPriceFilter { get; set; } = "All";
    public string SelectedRatingFilter { get; set; } = "All";
    public string SelectedPlaytimeFilter { get; set; } = "All";
    public bool SearchAllFields { get; set; } = false;

    /// <summary>True when any status/platform or advanced filter is active.</summary>
    public bool HasActiveFilters =>
        ActiveStatuses.Count > 0 ||
        ActivePlatforms.Count > 0 ||
        (!string.IsNullOrEmpty(SelectedGenreFilter) && SelectedGenreFilter != "All") ||
        (SelectedPriceFilter != "All") ||
        (SelectedRatingFilter != "All") ||
        (SelectedPlaytimeFilter != "All") ||
        !string.IsNullOrEmpty(_filter);

    /// <summary>Toggles a status chip on/off and re-applies the filter.</summary>
    public void ToggleStatus(GameStatus status)
    {
        if (!ActiveStatuses.Remove(status))
            ActiveStatuses.Add(status);
        Notify(nameof(HasActiveFilters));
        ApplyFilter();
    }

    /// <summary>Toggles a platform chip on/off and re-applies the filter.</summary>
    public void TogglePlatform(Platform platform)
    {
        if (!ActivePlatforms.Remove(platform))
            ActivePlatforms.Add(platform);
        Notify(nameof(HasActiveFilters));
        ApplyFilter();
    }

    public void SetGenreFilter(string? genre)
    {
        SelectedGenreFilter = string.IsNullOrWhiteSpace(genre) ? "All" : genre;
        Notify(nameof(HasActiveFilters));
        ApplyFilter();
    }

    public void SetPriceFilter(string filter)
    {
        SelectedPriceFilter = filter;
        Notify(nameof(HasActiveFilters));
        ApplyFilter();
    }

    public void SetRatingFilter(string filter)
    {
        SelectedRatingFilter = filter;
        Notify(nameof(HasActiveFilters));
        ApplyFilter();
    }

    public void SetPlaytimeFilter(string filter)
    {
        SelectedPlaytimeFilter = filter;
        Notify(nameof(HasActiveFilters));
        ApplyFilter();
    }

    public void SetSearchAllFields(bool searchAll)
    {
        SearchAllFields = searchAll;
        ApplyFilter();
    }

    public IReadOnlyList<string> GetDistinctGenres()
    {
        var genres = _store.All()
            .OfType<BaseTrackableGame>()
            .Select(g => g.Genre?.Trim())
            .Where(g => !string.IsNullOrEmpty(g))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g)
            .ToList();

        var list = new List<string> { "All" };
        list.AddRange(genres!);
        return list;
    }

    /// <summary>Clears all status and platform and advanced filters.</summary>
    public void ClearFilters()
    {
        ActiveStatuses.Clear();
        ActivePlatforms.Clear();
        SelectedGenreFilter = "All";
        SelectedPriceFilter = "All";
        SelectedRatingFilter = "All";
        SelectedPlaytimeFilter = "All";
        _filter = string.Empty;
        Notify(nameof(Filter));
        Notify(nameof(HasActiveFilters));
        ApplyFilter();
    }

    // ── Sorting ───────────────────────────────────────────────────────────────

    public GameSortField CurrentSortField { get; private set; } = GameSortField.Default;
    public SortDirection CurrentSortDirection { get; private set; } = SortDirection.Ascending;
    public bool IsSorted => CurrentSortField != GameSortField.Default;

    public string CurrentSortDescription => CurrentSortField switch
    {
        GameSortField.Default              => "Default (Store Order)",
        GameSortField.Title                => CurrentSortDirection == SortDirection.Ascending ? "Title (A → Z)" : "Title (Z → A)",
        GameSortField.HoursPlayed          => CurrentSortDirection == SortDirection.Ascending ? "Hours (Lowest)" : "Hours (Highest)",
        GameSortField.Rating               => CurrentSortDirection == SortDirection.Ascending ? "Rating (Lowest)" : "Rating (Highest)",
        GameSortField.Price                => CurrentSortDirection == SortDirection.Ascending ? "Price (Lowest)" : "Price (Highest)",
        GameSortField.ReleaseYear          => CurrentSortDirection == SortDirection.Ascending ? "Year (Oldest)" : "Year (Newest)",
        GameSortField.AverageCompleteHours => CurrentSortDirection == SortDirection.Ascending ? "Est. Length (Shortest)" : "Est. Length (Longest)",
        _                                  => "Default"
    };

    public void SetSort(GameSortField field, SortDirection? direction = null)
    {
        if (field == CurrentSortField && direction is null)
        {
            CurrentSortDirection = CurrentSortDirection == SortDirection.Ascending
                ? SortDirection.Descending
                : SortDirection.Ascending;
        }
        else
        {
            CurrentSortField = field;
            CurrentSortDirection = direction ?? (field is GameSortField.Rating or GameSortField.HoursPlayed or GameSortField.ReleaseYear
                ? SortDirection.Descending
                : SortDirection.Ascending);
        }

        ApplyFilter();
        Notify(nameof(CurrentSortField));
        Notify(nameof(CurrentSortDirection));
        Notify(nameof(CurrentSortDescription));
        Notify(nameof(IsSorted));
    }

    public void ToggleSortDirection()
    {
        CurrentSortDirection = CurrentSortDirection == SortDirection.Ascending
            ? SortDirection.Descending
            : SortDirection.Ascending;
        ApplyFilter();
        Notify(nameof(CurrentSortDirection));
        Notify(nameof(CurrentSortDescription));
    }

    public void ResetSort()
    {
        CurrentSortField = GameSortField.Default;
        CurrentSortDirection = SortDirection.Ascending;
        ApplyFilter();
        Notify(nameof(CurrentSortField));
        Notify(nameof(CurrentSortDirection));
        Notify(nameof(CurrentSortDescription));
        Notify(nameof(IsSorted));
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
            Notify(nameof(SelectedPublisher));
            Notify(nameof(SelectedCompletionEstimate));
            Notify(nameof(SelectedRating));
            Notify(nameof(SelectedReleaseYear));
            Notify(nameof(SelectedDescription));
            ((RelayCommand)RemoveCommand).RaiseCanExecuteChanged();
        }
    }

    /// <inheritdoc/>
    public string Details =>
        _selected is BaseTrackableGame g
            ? $"{g.Title}  ·  {g.Platform}  ·  {g.Status}  ·  {g.HoursPlayed:F0}h  ·  {(g.Price.HasValue ? $"€{g.Price:F2}" : "free")}  ·  {g.Genre ?? "—"}  ·  {g.Developer.Name}"
            : _selected is not null
                ? $"{_selected.DisplayName} [{_selected.Id}]"
                : "No game selected.";

    // ── Per-field detail properties (resolved from Selected) ─────────────────

    private BaseTrackableGame? Game => _selected as BaseTrackableGame;

    public string SelectedPlatform           => Game?.Platform.ToString() ?? "—";
    public string SelectedStatus             => Game?.Status.ToString()   ?? "—";
    public string SelectedHours              => Game is { } g ? $"{g.HoursPlayed:F0} h" : "—";
    public string SelectedPrice              => Game is { Price: { } p } ? $"€{p:F2}" : Game is not null ? "Free" : "—";
    public string SelectedGenre              => Game?.Genre ?? "—";
    public string SelectedDeveloper          => Game?.Developer.Name ?? "—";
    public string SelectedPublisher          => !string.IsNullOrWhiteSpace(Game?.Publisher) ? Game.Publisher : "—";
    public string SelectedCompletionEstimate => Game is { AverageCompleteHours: { } h } ? $"{h:F0} h" : "—";
    public string SelectedRating             => Game?.Rating?.ToString() ?? "Not rated";
    public string SelectedReleaseYear        => Game?.ReleaseYear?.ToString() ?? "—";
    public string SelectedDescription        => !string.IsNullOrWhiteSpace(Game?.Description) ? Game.Description : "No description or notes.";

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
                           double hoursPlayed, decimal? price, int? releaseYear, int? rating,
                           string? publisher = null, double? averageCompleteHours = null, string? description = null)
    {
        // BaseTrackableGame uses init-only setters for most fields, but the
        // mutable ones (Status, HoursPlayed, Rating, Publisher, AverageCompleteHours, Description) can be set directly.
        // For init-only fields we update via reflection so the Core API stays clean.
        SetProp(game, nameof(game.Status),               status);
        SetProp(game, nameof(game.HoursPlayed),          hoursPlayed);
        SetProp(game, nameof(game.Rating),               rating);
        SetProp(game, nameof(game.Publisher),            publisher);
        SetProp(game, nameof(game.AverageCompleteHours), averageCompleteHours);
        SetProp(game, nameof(game.Description),          description);

        // These are init-only — use reflection to mutate in-place.
        SetInit(game, nameof(game.Title),                title);
        SetInit(game, nameof(game.Platform),             platform);
        SetInit(game, nameof(game.Genre),                genre);
        SetInit(game, nameof(game.Price),                price);
        SetInit(game, nameof(game.ReleaseYear),          releaseYear);
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
        Notify(nameof(SelectedPublisher));
        Notify(nameof(SelectedCompletionEstimate));
        Notify(nameof(SelectedRating));
        Notify(nameof(SelectedReleaseYear));
        Notify(nameof(SelectedDescription));

        // Persist the changes.
        _repo?.Update(game);

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
        _repo?.Delete(id);
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

    private bool Matches(IItem item)
    {
        // Text search
        if (!string.IsNullOrEmpty(_filter))
        {
            if (SearchAllFields && item is BaseTrackableGame bg)
            {
                bool matchesAny =
                    item.DisplayName.Contains(_filter, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(bg.Genre) && bg.Genre.Contains(_filter, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(bg.Developer.Name) && bg.Developer.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(bg.Publisher) && bg.Publisher.Contains(_filter, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(bg.Description) && bg.Description.Contains(_filter, StringComparison.OrdinalIgnoreCase));

                if (!matchesAny) return false;
            }
            else
            {
                if (!item.DisplayName.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
        }

        // Status / platform chips and advanced filters (only applied to trackable games)
        if (item is BaseTrackableGame g)
        {
            if (ActiveStatuses.Count > 0 && !ActiveStatuses.Contains(g.Status))
                return false;
            if (ActivePlatforms.Count > 0 && !ActivePlatforms.Contains(g.Platform))
                return false;

            if (!string.IsNullOrWhiteSpace(SelectedGenreFilter) && SelectedGenreFilter != "All")
            {
                if (!string.Equals(g.Genre, SelectedGenreFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (SelectedPriceFilter != "All")
            {
                switch (SelectedPriceFilter)
                {
                    case "Free":
                        if (g.Price is { } p && p > 0) return false;
                        break;
                    case "Paid":
                        if (g.Price is null or <= 0) return false;
                        break;
                    case "<20":
                    case "Under20":
                        if (g.Price is null or > 20) return false;
                        break;
                    case "<50":
                    case "Under50":
                        if (g.Price is null or > 50) return false;
                        break;
                }
            }

            if (SelectedRatingFilter != "All")
            {
                switch (SelectedRatingFilter)
                {
                    case "9+":
                        if (g.Rating is null or < 9) return false;
                        break;
                    case "8+":
                        if (g.Rating is null or < 8) return false;
                        break;
                    case "7+":
                        if (g.Rating is null or < 7) return false;
                        break;
                    case "Unrated":
                        if (g.Rating.HasValue) return false;
                        break;
                }
            }

            if (SelectedPlaytimeFilter != "All")
            {
                switch (SelectedPlaytimeFilter)
                {
                    case "Unplayed":
                        if (g.HoursPlayed > 0) return false;
                        break;
                    case "<10h":
                    case "Under10h":
                        if (g.HoursPlayed >= 10) return false;
                        break;
                    case "10-50h":
                        if (g.HoursPlayed < 10 || g.HoursPlayed > 50) return false;
                        break;
                    case "50h+":
                        if (g.HoursPlayed < 50) return false;
                        break;
                }
            }
        }

        return true;
    }

    private void ApplyFilter()
    {
        var allItems = _store.All();
        var matched = allItems.Where(Matches);

        IEnumerable<IItem> sorted = CurrentSortField switch
        {
            GameSortField.Title => CurrentSortDirection == SortDirection.Ascending
                ? matched.OrderBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase)
                : matched.OrderByDescending(i => i.DisplayName, StringComparer.OrdinalIgnoreCase),

            GameSortField.HoursPlayed => CurrentSortDirection == SortDirection.Ascending
                ? matched.OrderBy(i => (i as BaseTrackableGame)?.HoursPlayed ?? 0)
                : matched.OrderByDescending(i => (i as BaseTrackableGame)?.HoursPlayed ?? 0),

            GameSortField.Rating => CurrentSortDirection == SortDirection.Ascending
                ? matched.OrderBy(i => (i as BaseTrackableGame)?.Rating.HasValue != true)
                         .ThenBy(i => (i as BaseTrackableGame)?.Rating)
                : matched.OrderBy(i => (i as BaseTrackableGame)?.Rating.HasValue != true)
                         .ThenByDescending(i => (i as BaseTrackableGame)?.Rating),

            GameSortField.Price => CurrentSortDirection == SortDirection.Ascending
                ? matched.OrderBy(i => (i as BaseTrackableGame)?.Price ?? 0m)
                : matched.OrderByDescending(i => (i as BaseTrackableGame)?.Price ?? 0m),

            GameSortField.ReleaseYear => CurrentSortDirection == SortDirection.Ascending
                ? matched.OrderBy(i => (i as BaseTrackableGame)?.ReleaseYear.HasValue != true)
                         .ThenBy(i => (i as BaseTrackableGame)?.ReleaseYear)
                : matched.OrderBy(i => (i as BaseTrackableGame)?.ReleaseYear.HasValue != true)
                         .ThenByDescending(i => (i as BaseTrackableGame)?.ReleaseYear),

            GameSortField.AverageCompleteHours => CurrentSortDirection == SortDirection.Ascending
                ? matched.OrderBy(i => (i as BaseTrackableGame)?.AverageCompleteHours.HasValue != true)
                         .ThenBy(i => (i as BaseTrackableGame)?.AverageCompleteHours)
                : matched.OrderBy(i => (i as BaseTrackableGame)?.AverageCompleteHours.HasValue != true)
                         .ThenByDescending(i => (i as BaseTrackableGame)?.AverageCompleteHours),

            _ => matched
        };

        var targetList = sorted.ToList();
        var targetSet = new HashSet<string>(targetList.Select(x => x.Id));

        // Remove items that no longer match (iterate backwards for safe removal).
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            if (!targetSet.Contains(_items[i].Id))
                _items.RemoveAt(i);
        }

        // Align _items with targetList
        for (int i = 0; i < targetList.Count; i++)
        {
            var target = targetList[i];
            if (i < _items.Count && _items[i].Id == target.Id)
            {
                continue;
            }

            int existingIdx = -1;
            for (int j = i + 1; j < _items.Count; j++)
            {
                if (_items[j].Id == target.Id)
                {
                    existingIdx = j;
                    break;
                }
            }

            if (existingIdx >= 0)
            {
                _items.Move(existingIdx, i);
            }
            else
            {
                _items.Insert(i, target);
            }
        }

        Notify(nameof(Count));
    }
}

// ── Sorting enums ─────────────────────────────────────────────────────────────

public enum GameSortField
{
    Default,
    Title,
    HoursPlayed,
    Rating,
    Price,
    ReleaseYear,
    AverageCompleteHours
}

public enum SortDirection
{
    Ascending,
    Descending
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

