using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;
using Capstone.Core.Services;
using Capstone.Core.ViewModels;
using Capstone.Core.Web;
using Capstone.UI.DevConsole;
using SharpQuest.Contracts;

namespace Capstone.UI.Views;

public partial class MainWindow : Window
{
    private DevConsoleViewModel? _consoleVm;

    // Tracks which status/platform chip buttons are currently active
    // so we can toggle their CSS class without binding overhead.
    private readonly Dictionary<Button, GameStatus> _statusChips   = new();
    private readonly Dictionary<Button, Platform>   _platformChips = new();
    private readonly List<Button>                   _sortButtons   = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        // Register status chips
        _statusChips[ChipBacklog]   = GameStatus.Backlog;
        _statusChips[ChipPlaying]   = GameStatus.Playing;
        _statusChips[ChipCompleted] = GameStatus.Completed;
        _statusChips[ChipAbandoned] = GameStatus.Abandoned;
        _statusChips[ChipWishlist]  = GameStatus.Wishlist;

        // Register platform chips
        _platformChips[ChipPC]      = Platform.PC;
        _platformChips[ChipPS4]     = Platform.PlayStation4;
        _platformChips[ChipPS5]     = Platform.PlayStation5;
        _platformChips[ChipXbox]    = Platform.XboxOne;
        _platformChips[ChipSwitch]  = Platform.NintendoSwitch;
        _platformChips[ChipSwitch2] = Platform.NintendoSwitch2;
        _platformChips[ChipAndroid] = Platform.Android;
        _platformChips[ChipIOS]     = Platform.iOS;
        _platformChips[ChipOther]   = Platform.Other;

        // Register sort buttons
        _sortButtons.AddRange([SortDefaultBtn, SortTitleBtn, SortHoursBtn, SortRatingBtn, SortPriceBtn, SortYearBtn, SortLengthBtn]);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is CatalogueViewModel vm)
        {
            RefreshGenreDropdown();

            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(CatalogueViewModel.Count) ||
                    args.PropertyName == nameof(CatalogueViewModel.Filter))
                {
                    UpdateFilterCountLabel();
                }
            };

            var storeField = typeof(CatalogueViewModel)
                .GetField("_store", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var store = storeField?.GetValue(vm) as IItemStore;

            if (store is not null)
            {
                _consoleVm = new DevConsoleViewModel(store);
                DevConsole.DataContext = _consoleVm;

                _consoleVm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(DevConsoleViewModel.IsVisible))
                    {
                        DevConsole.IsVisible = _consoleVm.IsVisible;
                        if (_consoleVm.IsVisible)
                            DevConsole.FocusInput();
                    }
                };

                DevConsole.IsVisible = false;
            }
        }
    }

    private CatalogueViewModel? Vm => DataContext as CatalogueViewModel;

    // ── Keyboard shortcut: Esc for Settings, F12 or ~ toggles console ───────

    private async void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            await OpenSettingsAsync();
            return;
        }

        bool isToggle = e.Key == Key.F12 ||
                        (e.Key == Key.OemTilde) ||
                        (e.Key == Key.Oem3);

        if (isToggle)
        {
            _consoleVm?.Toggle();
            e.Handled = true;
        }
    }

    // ── Filter bar toggle ─────────────────────────────────────────────────────

    private void OnFilterToggleClick(object? sender, RoutedEventArgs e)
    {
        FilterBar.IsVisible = !FilterBar.IsVisible;
        FilterToggleBtn.Content = FilterBar.IsVisible ? "⊟ Filter" : "⊞ Filter";
    }

    // ── Sort bar toggle & actions ─────────────────────────────────────────────

    private void OnSortToggleClick(object? sender, RoutedEventArgs e)
    {
        SortBar.IsVisible = !SortBar.IsVisible;
        SortToggleBtn.Content = SortBar.IsVisible ? "⊟ Sort" : "⇅ Sort";
    }

    private void OnSortFieldClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || Vm is null) return;
        var tag = btn.Tag?.ToString();
        if (Enum.TryParse<GameSortField>(tag, out var field))
        {
            Vm.SetSort(field);
            UpdateSortUI();
        }
    }

    private void OnSortDirectionClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is null) return;
        Vm.ToggleSortDirection();
        UpdateSortUI();
    }

    private void OnResetSortClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is null) return;
        Vm.ResetSort();
        UpdateSortUI();
    }

    private void UpdateSortUI()
    {
        if (Vm is null) return;
        foreach (var btn in _sortButtons)
        {
            var tag = btn.Tag?.ToString();
            if (Enum.TryParse<GameSortField>(tag, out var field) && field == Vm.CurrentSortField)
                btn.Classes.Add("active");
            else
                btn.Classes.Remove("active");
        }
        CurrentSortLabel.Text = Vm.CurrentSortDescription;
        SortDirectionBtn.Content = Vm.CurrentSortDirection == SortDirection.Ascending ? "▲ Ascending" : "▼ Descending";
    }

    // ── Status chip click ─────────────────────────────────────────────────────

    private void OnStatusChipClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || Vm is null) return;
        if (!_statusChips.TryGetValue(btn, out var status)) return;

        Vm.ToggleStatus(status);

        bool isActive = Vm.ActiveStatuses.Contains(status);
        if (isActive) btn.Classes.Add("active");
        else          btn.Classes.Remove("active");

        UpdateFilterCountLabel();
    }

    // ── Platform chip click ───────────────────────────────────────────────────

    private void OnPlatformChipClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || Vm is null) return;
        if (!_platformChips.TryGetValue(btn, out var platform)) return;

        Vm.TogglePlatform(platform);

        bool isActive = Vm.ActivePlatforms.Contains(platform);
        if (isActive) btn.Classes.Add("active");
        else          btn.Classes.Remove("active");

        UpdateFilterCountLabel();
    }

    // ── Advanced filter actions ───────────────────────────────────────────────

    private void RefreshGenreDropdown()
    {
        if (Vm is null) return;
        var genres = Vm.GetDistinctGenres();
        GenreFilterBox.ItemsSource = genres;
        if (GenreFilterBox.SelectedIndex < 0 && genres.Count > 0)
            GenreFilterBox.SelectedIndex = 0;
    }

    private void OnGenreFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Vm is null) return;
        var selected = GenreFilterBox.SelectedItem as string;
        Vm.SetGenreFilter(selected);
        UpdateFilterCountLabel();
    }

    private void OnPriceFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Vm is null) return;
        var tag = (PriceFilterBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
        Vm.SetPriceFilter(tag);
        UpdateFilterCountLabel();
    }

    private void OnRatingFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Vm is null) return;
        var tag = (RatingFilterBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
        Vm.SetRatingFilter(tag);
        UpdateFilterCountLabel();
    }

    private void OnPlaytimeFilterChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (Vm is null) return;
        var tag = (PlaytimeFilterBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
        Vm.SetPlaytimeFilter(tag);
        UpdateFilterCountLabel();
    }

    private void OnSearchScopeChanged(object? sender, RoutedEventArgs e)
    {
        if (Vm is null) return;
        Vm.SetSearchAllFields(SearchAllFieldsBox.IsChecked == true);
        UpdateFilterCountLabel();
    }

    // ── Clear filters ─────────────────────────────────────────────────────────

    private void OnClearFiltersClick(object? sender, RoutedEventArgs e)
    {
        Vm?.ClearFilters();

        foreach (var btn in _statusChips.Keys)   btn.Classes.Remove("active");
        foreach (var btn in _platformChips.Keys) btn.Classes.Remove("active");

        if (GenreFilterBox.ItemsSource is IEnumerable<string> items && items.Any())
            GenreFilterBox.SelectedIndex = 0;
        PriceFilterBox.SelectedIndex = 0;
        RatingFilterBox.SelectedIndex = 0;
        PlaytimeFilterBox.SelectedIndex = 0;
        SearchAllFieldsBox.IsChecked = false;

        UpdateFilterCountLabel();
    }

    private void UpdateFilterCountLabel()
    {
        if (Vm is null) return;
        int total = Vm.ActiveStatuses.Count + Vm.ActivePlatforms.Count;
        if (!string.IsNullOrEmpty(Vm.SelectedGenreFilter) && Vm.SelectedGenreFilter != "All") total++;
        if (Vm.SelectedPriceFilter != "All") total++;
        if (Vm.SelectedRatingFilter != "All") total++;
        if (Vm.SelectedPlaytimeFilter != "All") total++;
        if (!string.IsNullOrEmpty(Vm.Filter)) total++;

        ActiveFilterCount.Text = total > 0
            ? $"{total} filter{(total == 1 ? "" : "s")} active  ·  {Vm.Count} result{(Vm.Count == 1 ? "" : "s")}"
            : string.Empty;
    }

    // ── Settings ──────────────────────────────────────────────────────────────

    private bool _isSettingsOpen = false;

    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        await OpenSettingsAsync();
    }

    private async Task OpenSettingsAsync()
    {
        if (_isSettingsOpen) return;
        try
        {
            _isSettingsOpen = true;
            var dialog = new SettingsDialog(() =>
            {
                if (_consoleVm is not null)
                {
                    _consoleVm.IsVisible = true;
                    DevConsole.IsVisible = true;
                    DevConsole.FocusInput();
                }
            });
            await dialog.ShowDialog(this);
        }
        finally
        {
            _isSettingsOpen = false;
        }
    }

    // ── Export / Import ───────────────────────────────────────────────────────

    private void OnImportExportClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;

        var exportItem      = new MenuItem { Header = "⬆ Export…" };
        var importJsonItem  = new MenuItem { Header = "⬇ Import from file…" };
        var importSteamItem = new MenuItem { Header = "🎮 Import from Steam…" };

        exportItem.Click      += async (_, _) => await DoExportAsync();
        importJsonItem.Click  += async (_, _) => await DoImportAsync();
        importSteamItem.Click += async (_, _) => await DoSteamImportAsync();

        var menu = new ContextMenu
        {
            Items = { exportItem, new Separator(), importJsonItem, importSteamItem }
        };

        menu.Open(btn);
    }

    private async Task DoExportAsync()
    {
        if (Vm is null) return;

        var file = await TopLevel.GetTopLevel(this)!.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title             = "Export Library",
                SuggestedFileName = "sharpquest-library.json",
                FileTypeChoices   =
                [
                    new FilePickerFileType("JSON file") { Patterns = ["*.json"] }
                ]
            });

        if (file is null) return;

        try
        {
            var path = file.Path.LocalPath;
            await LibraryExporter.ExportAsync(Vm.Items, path);

            var count = Vm.Items.Count();
            SetStatus($"Exported {count} game{(count == 1 ? "" : "s")} to {System.IO.Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            SetStatus($"Export failed: {ex.Message}");
        }
    }

    private async Task DoImportAsync()
    {
        var files = await TopLevel.GetTopLevel(this)!.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title          = "Import Library",
                AllowMultiple  = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("JSON file") { Patterns = ["*.json"] }
                ]
            });

        if (files.Count == 0) return;

        try
        {
            var path     = files[0].Path.LocalPath;
            var imported = await LibraryExporter.ImportAsync(path);

            if (imported.Count == 0)
            {
                SetStatus("Import file contains no games.");
                return;
            }

            // Warn if the library already has games — importing will replace everything.
            if (Vm is not null && Vm.Items.Any())
            {
                var currentCount = Vm.Items.Count();
                var confirm = new ImportConfirmDialog(currentCount, imported.Count);
                var proceed = await confirm.ShowDialog<bool>(this);
                if (!proceed) return;

                Vm.ClearAll();
            }

            foreach (var game in imported)
                Vm?.AddCommand.Execute(game);

            RefreshGenreDropdown();
            SetStatus($"Imported {imported.Count} game{(imported.Count == 1 ? "" : "s")} from {System.IO.Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            SetStatus($"Import failed: {ex.Message}");
        }
    }

    private async Task DoSteamImportAsync()
    {
        var dialog  = new SteamImportDialog();
        var results = await dialog.ShowDialog<List<Capstone.Core.Models.DigitalGame>?>(this);

        if (results is null || results.Count == 0) return;

        // If the library already has games, ask the user whether to merge or replace.
        if (Vm is not null && Vm.Items.Any())
        {
            var currentCount = Vm.Items.Count();
            var confirm      = new SteamMergeConfirmDialog(currentCount, results.Count);
            var mode         = await confirm.ShowDialog<SteamMergeMode>(this);

            if (mode == SteamMergeMode.Cancel) return;

            if (mode == SteamMergeMode.Replace)
                Vm.ClearAll();
            // SteamMergeMode.Merge → just add without clearing
        }

        foreach (var game in results)
            Vm?.AddCommand.Execute(game);

        RefreshGenreDropdown();
        SetStatus($"Imported {results.Count} game{(results.Count == 1 ? "" : "s")} from Steam.");
    }

    // ── Status bar helper ─────────────────────────────────────────────────────

    private System.Threading.CancellationTokenSource? _statusCts;

    private async void SetStatus(string message)
    {
        _statusCts?.Cancel();
        _statusCts = new System.Threading.CancellationTokenSource();
        var token = _statusCts.Token;

        StatusLabel.Text = message;

        try
        {
            await Task.Delay(4000, token);
            if (!token.IsCancellationRequested)
                StatusLabel.Text = string.Empty;
        }
        catch (TaskCanceledException) { /* replaced by a newer message */ }
    }

    // ── Value Analyzer ────────────────────────────────────────────────────────

    private async void OnAnalyzeClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is null) return;

        var storeField = typeof(CatalogueViewModel)
            .GetField("_store", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var store = storeField?.GetValue(Vm) as IItemStore;
        if (store is null) return;

        var dialog = new ValueAnalyzerDialog(store);
        await dialog.ShowDialog(this);
    }

    // ── Add Manual ────────────────────────────────────────────────────────────

    private async void OnAddManualClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new AddGameDialog();
        var result = await dialog.ShowDialog<DigitalGame?>(this);
        if (result is not null)
        {
            Vm?.AddCommand.Execute(result);
            RefreshGenreDropdown();
        }
    }

    // ── Add from IGDB ─────────────────────────────────────────────────────────

    private async void OnAddIgdbClick(object? sender, RoutedEventArgs e)
    {
        var dialog = new IgdbSearchDialog();
        var result = await dialog.ShowDialog<DigitalGame?>(this);
        if (result is not null)
        {
            Vm?.AddCommand.Execute(result);
            RefreshGenreDropdown();
        }
    }

    // ── Edit game (right-click context menu) ──────────────────────────────────

    private async void OnGameListDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (Vm?.Selected is not BaseTrackableGame game) return;
        await OpenEditDialogAsync(game);
    }

    private async void OnEditClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi) return;
        var item = (mi.Parent as ContextMenu)?.DataContext
                   ?? (mi.DataContext);

        if (item is not BaseTrackableGame game) return;

        if (Vm is not null)
            Vm.Selected = game;

        await OpenEditDialogAsync(game);
    }

    private async Task OpenEditDialogAsync(BaseTrackableGame game)
    {
        var dialog = new EditGameDialog(game);
        var result = await dialog.ShowDialog<EditResult?>(this);
        if (result is null) return;

        Vm?.UpdateGame(game,
            result.Title,    result.Platform, result.Status,
            result.Genre,    result.Developer,
            result.HoursPlayed, result.Price, result.ReleaseYear, result.Rating,
            result.Publisher, result.AverageCompleteHours, result.Description);
    }
}

// ── Status dot colour converter ────────────────────────────────────────────────

/// <summary>
/// Maps a GameStatus enum value to a coloured dot brush for the list item indicator.
/// </summary>
public sealed class StatusDotConverter : IValueConverter
{
    public static readonly StatusDotConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is GameStatus status
            ? new SolidColorBrush(Color.Parse(StatusToHex(status)))
            : Brushes.Gray;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static string StatusToHex(GameStatus s) => s switch
    {
        GameStatus.Playing   => "#a6e3a1",
        GameStatus.Completed => "#89b4fa",
        GameStatus.Backlog   => "#f9e2af",
        GameStatus.Wishlist  => "#cba6f7",
        GameStatus.Abandoned => "#f38ba8",
        _                    => "#585b70"
    };
}
