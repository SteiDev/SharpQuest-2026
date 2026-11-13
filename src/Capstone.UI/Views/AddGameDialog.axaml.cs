using Avalonia.Controls;
using Avalonia.Interactivity;
using Capstone.Core.Models;
using Capstone.Core.Models.Enums;

namespace Capstone.UI.Views;

public partial class AddGameDialog : Window
{
    private static int _nextId = 100;

    public AddGameDialog()
    {
        InitializeComponent();

        PlatformBox.ItemsSource = Enum.GetValues<Platform>();
        PlatformBox.SelectedIndex = 0;

        StatusBox.ItemsSource = Enum.GetValues<GameStatus>();
        StatusBox.SelectedIndex = 0;
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close(null);

    private void OnAdd(object? sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text?.Trim();
        if (string.IsNullOrEmpty(title))
        {
            ErrorText.Text = "Title is required.";
            ErrorText.IsVisible = true;
            return;
        }

        decimal? price = null;
        if (!string.IsNullOrWhiteSpace(PriceBox.Text))
        {
            if (!decimal.TryParse(PriceBox.Text, out var p))
            {
                ErrorText.Text = "Price must be a number (e.g. 29.99).";
                ErrorText.IsVisible = true;
                return;
            }
            price = p;
        }

        int? year = null;
        if (!string.IsNullOrWhiteSpace(YearBox.Text))
        {
            if (!int.TryParse(YearBox.Text, out var y))
            {
                ErrorText.Text = "Year must be an integer (e.g. 2024).";
                ErrorText.IsVisible = true;
                return;
            }
            year = y;
        }

        var platform = PlatformBox.SelectedItem is Platform p2 ? p2 : Platform.Other;
        var status   = StatusBox.SelectedItem   is GameStatus s ? s : GameStatus.Backlog;

        var game = new DigitalGame
        {
            Id          = $"man-{_nextId++:D3}",
            Title       = title,
            Platform    = platform,
            Status      = status,
            Price       = price,
            ReleaseYear = year,
            Genre       = string.IsNullOrWhiteSpace(GenreBox.Text) ? null : GenreBox.Text.Trim(),
            Developer   = new Developer { Name = DevBox.Text?.Trim() ?? string.Empty }
        };

        Close(game);
    }
}

