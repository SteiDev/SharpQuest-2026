using Avalonia.Controls;
using Avalonia.Interactivity;
using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
using Capstone.Core.Web;

namespace Capstone.UI.Views;

public partial class IgdbSearchDialog : Window
{
    public IgdbSearchDialog(string? prefillTitle = null)
    {
        InitializeComponent();

        PlatformBox.ItemsSource = Enum.GetValues<Platform>();
        PlatformBox.SelectedItem = Platform.PC;

        StatusBox.ItemsSource = Enum.GetValues<GameStatus>();
        StatusBox.SelectedItem = GameStatus.Backlog;

        if (!string.IsNullOrWhiteSpace(prefillTitle))
            SearchBox.Text = prefillTitle;
    }

    // ── Search ────────────────────────────────────────────────────────────────

    private async void OnSearch(object? sender, RoutedEventArgs e)
    {
        var query = SearchBox.Text?.Trim();
        if (string.IsNullOrEmpty(query)) return;

        StatusText.Text = "Searching…";
        ResultsList.ItemsSource = null;

        // If the query is a plain integer, treat it as a direct IGDB id lookup.
        var client = new IgdbClient();
        IReadOnlyList<IgdbGame>? results;

        if (int.TryParse(query, out var igdbId))
        {
            var single = await client.GetByIdAsync(igdbId);
            results = single is null ? [] : [single];
        }
        else
        {
            results = await client.SearchAsync(query);
        }

        if (results.Count == 0)
        {
            StatusText.Text = "No results (check .env credentials or try a different title).";
        }
        else
        {
            StatusText.Text = $"{results.Count} result(s) — select one and click Add Selected.";
            ResultsList.ItemsSource = results;
            ResultsList.SelectedIndex = 0;
        }
    }

    // ── Add selected ──────────────────────────────────────────────────────────

    private void OnAddSelected(object? sender, RoutedEventArgs e)
    {
        if (ResultsList.SelectedItem is not IgdbGame igdb)
        {
            StatusText.Text = "Please select a game from the results first.";
            return;
        }

        var platform = PlatformBox.SelectedItem is Platform p ? p : Platform.Other;
        var status   = StatusBox.SelectedItem   is GameStatus s ? s : GameStatus.Backlog;

        var game = new DigitalGame
        {
            Id          = $"igdb-{igdb.Id}",
            Title       = igdb.Name,
            Platform    = platform,
            Status      = status,
            ReleaseYear = igdb.ReleaseYear,
            Genre       = igdb.PrimaryGenre,
            Developer   = new Developer { Name = igdb.PrimaryCompany ?? string.Empty },
            Description = igdb.Summary
        };

        Close(game);
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);
}
