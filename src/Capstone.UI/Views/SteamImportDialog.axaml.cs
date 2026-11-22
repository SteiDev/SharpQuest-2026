using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Capstone.Core.Models;
using Capstone.Core.Web;

namespace Capstone.UI.Views;

/// <summary>
/// Dialog that lets the user enter their Steam API key + Steam64 ID,
/// fetch their owned games, pick which to add, and return the selection
/// as a list of <see cref="DigitalGame"/> objects.
/// </summary>
public partial class SteamImportDialog : Window
{
    public SteamImportDialog()
    {
        InitializeComponent();
        PreFillFromEnv();
    }

    // ── Pre-fill credentials from environment / .env ──────────────────────────

    private void PreFillFromEnv()
    {
        var key     = Environment.GetEnvironmentVariable("STEAM_API_KEY");
        var steamId = Environment.GetEnvironmentVariable("STEAM_ID");

        if (!string.IsNullOrWhiteSpace(key))
            ApiKeyBox.Text = key;

        if (!string.IsNullOrWhiteSpace(steamId))
            SteamIdBox.Text = steamId;
    }

    // ── Toggle API key visibility ─────────────────────────────────────────────

    private void OnToggleApiKeyVisibility(object? sender, RoutedEventArgs e)
    {
        ApiKeyBox.PasswordChar = ApiKeyBox.PasswordChar == '\0' ? '●' : '\0';
    }

    // ── Fetch library ─────────────────────────────────────────────────────────

    private async void OnFetchClick(object? sender, RoutedEventArgs e)
    {
        var key     = ApiKeyBox.Text?.Trim()   ?? string.Empty;
        var steamId = SteamIdBox.Text?.Trim()  ?? string.Empty;

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(steamId))
        {
            StatusText.Text = "Please fill in both the API key and Steam64 ID.";
            return;
        }

        FetchBtn.IsEnabled = false;
        ImportBtn.IsEnabled = false;
        GamesList.ItemsSource = null;
        StatusText.Text = "Fetching library from Steam…";
        SelectionLabel.Text = string.Empty;

        try
        {
            var importer = new SteamImporter();
            var games    = await importer.GetOwnedGamesAsync(key, steamId);

            if (games.Count == 0)
            {
                StatusText.Text =
                    "No games found. Check that your profile is public and the credentials are correct.";
                return;
            }

            // Sort: played first (descending hours), then alphabetical
            var sorted = games
                .OrderByDescending(g => g.PlaytimeForever)
                .ThenBy(g => g.Name)
                .ToList();

            GamesList.ItemsSource = sorted;

            // Pre-select all games
            GamesList.SelectAll();

            StatusText.Text = $"Found {games.Count} game{(games.Count == 1 ? "" : "s")} — select which to import.";
            UpdateSelectionLabel();
        }
        catch (SteamImportException ex)
        {
            StatusText.Text = $"Steam error: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            FetchBtn.IsEnabled = true;
        }
    }

    // ── Selection helpers ─────────────────────────────────────────────────────

    private void OnSelectAll(object? sender, RoutedEventArgs e)
    {
        GamesList.SelectAll();
        UpdateSelectionLabel();
    }

    private void OnSelectNone(object? sender, RoutedEventArgs e)
    {
        GamesList.SelectedItems?.Clear();
        UpdateSelectionLabel();
    }

    private void OnSelectPlayedOnly(object? sender, RoutedEventArgs e)
    {
        if (GamesList.ItemsSource is not IEnumerable<SteamGame> items) return;

        GamesList.SelectedItems?.Clear();
        foreach (var game in items)
        {
            if (game.PlaytimeForever > 0)
                GamesList.SelectedItems?.Add(game);
        }
        UpdateSelectionLabel();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
        => UpdateSelectionLabel();

    private void UpdateSelectionLabel()
    {
        var count = GamesList.SelectedItems?.Count ?? 0;
        SelectionLabel.Text = count > 0
            ? $"{count} game{(count == 1 ? "" : "s")} selected"
            : string.Empty;
        ImportBtn.IsEnabled = count > 0;
    }

    // ── Import ────────────────────────────────────────────────────────────────

    private void OnImport(object? sender, RoutedEventArgs e)
    {
        var selected = GamesList.SelectedItems?
            .OfType<SteamGame>()
            .Select(sg => SteamImporter.ToDigitalGame(sg))
            .ToList()
            ?? [];

        Close(selected);
    }

    // ── Cancel / keyboard ─────────────────────────────────────────────────────

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close(null);
        }
    }
}

