using Avalonia.Controls;
using Avalonia.Interactivity;
using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;

namespace Capstone.UI.Views;

public partial class EditGameDialog : Window
{
    private readonly BaseTrackableGame _game;

    // Parameterless constructor required by Avalonia's XAML loader.
    public EditGameDialog() : this(null!) { }

    public EditGameDialog(BaseTrackableGame game)
    {
        InitializeComponent();

        _game = game;

        // Header
        HeaderTitle.Text = game.Title;

        // Populate enum dropdowns
        PlatformBox.ItemsSource  = Enum.GetValues<Platform>();
        StatusBox.ItemsSource    = Enum.GetValues<GameStatus>();

        // Pre-fill all fields with the current values
        TitleBox.Text         = game.Title;
        GenreBox.Text         = game.Genre ?? string.Empty;
        DevBox.Text           = game.Developer.Name;
        PublisherBox.Text     = game.Publisher ?? string.Empty;
        CompleteHoursBox.Text = game.AverageCompleteHours?.ToString("F0") ?? string.Empty;
        HoursBox.Text         = game.HoursPlayed.ToString("F0");
        RatingBox.Text        = game.Rating?.ToString() ?? string.Empty;
        PriceBox.Text         = game.Price?.ToString("F2") ?? string.Empty;
        YearBox.Text          = game.ReleaseYear?.ToString() ?? string.Empty;
        DescriptionBox.Text   = game.Description ?? string.Empty;

        PlatformBox.SelectedItem = game.Platform;
        StatusBox.SelectedItem   = game.Status;
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            ShowError("Title is required.");
            return;
        }

        if (!double.TryParse(HoursBox.Text, out var hours) || hours < 0)
        {
            ShowError("Hours played must be a non-negative number.");
            return;
        }

        decimal? price = null;
        if (!string.IsNullOrWhiteSpace(PriceBox.Text))
        {
            if (!decimal.TryParse(PriceBox.Text, out var p) || p < 0)
            {
                ShowError("Price must be a non-negative number (e.g. 29.99).");
                return;
            }
            price = p;
        }

        int? year = null;
        if (!string.IsNullOrWhiteSpace(YearBox.Text))
        {
            if (!int.TryParse(YearBox.Text, out var y))
            {
                ShowError("Year must be an integer (e.g. 2024).");
                return;
            }
            year = y;
        }

        int? rating = null;
        if (!string.IsNullOrWhiteSpace(RatingBox.Text))
        {
            if (!int.TryParse(RatingBox.Text, out var r) || r < 1 || r > 10)
            {
                ShowError("Rating must be a whole number between 1 and 10.");
                return;
            }
            rating = r;
        }

        double? completeHours = null;
        if (!string.IsNullOrWhiteSpace(CompleteHoursBox.Text))
        {
            if (!double.TryParse(CompleteHoursBox.Text, out var ch) || ch < 0)
            {
                ShowError("Estimated completion hours must be a non-negative number.");
                return;
            }
            completeHours = ch;
        }

        var platform = PlatformBox.SelectedItem is Platform p2 ? p2 : _game.Platform;
        var status   = StatusBox.SelectedItem   is GameStatus s ? s : _game.Status;

        // Build an edit result record and return it
        Close(new EditResult(
            Title:                title,
            Platform:             platform,
            Status:               status,
            Genre:                string.IsNullOrWhiteSpace(GenreBox.Text) ? null : GenreBox.Text.Trim(),
            Developer:            DevBox.Text?.Trim() ?? string.Empty,
            HoursPlayed:          hours,
            Price:                price,
            ReleaseYear:          year,
            Rating:               rating,
            Publisher:            string.IsNullOrWhiteSpace(PublisherBox.Text) ? null : PublisherBox.Text.Trim(),
            AverageCompleteHours: completeHours,
            Description:          string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim()
        ));
    }

    private void ShowError(string message)
    {
        ErrorText.Text      = message;
        ErrorText.IsVisible = true;
    }
}

/// <summary>Plain data record carrying the validated edits back to the caller.</summary>
public sealed record EditResult(
    string     Title,
    Platform   Platform,
    GameStatus Status,
    string?    Genre,
    string     Developer,
    double     HoursPlayed,
    decimal?   Price,
    int?       ReleaseYear,
    int?       Rating,
    string?    Publisher = null,
    double?    AverageCompleteHours = null,
    string?    Description = null);
