using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;
using SharpQuest.Contracts;

namespace Capstone.UI.Views;

// ── Row view-model ────────────────────────────────────────────────────────────

/// <summary>One row in the analyzer table — plain data, no framework dependency.</summary>
public sealed class AnalyzerRow
{
    public string Title           { get; init; } = string.Empty;
    public string Platform        { get; init; } = string.Empty;
    public string Status          { get; init; } = string.Empty;
    public string HoursPlayed     { get; init; } = string.Empty;
    public string Price           { get; init; } = string.Empty;
    public string CostPerHour     { get; init; } = string.Empty;
    public string BacklogRemaining { get; init; } = string.Empty;

    // Visual helpers (colour-coded cost and status pill)
    public IBrush CostColour        { get; init; } = Brushes.Gray;
    public IBrush StatusBackground  { get; init; } = Brushes.Transparent;
    public IBrush StatusForeground  { get; init; } = Brushes.Gray;

    // Raw value used for sorting
    public decimal? RawCostPerHour { get; init; }
}

// ── Dialog ────────────────────────────────────────────────────────────────────

public partial class ValueAnalyzerDialog : Window
{
    public ValueAnalyzerDialog(IItemStore store)
    {
        InitializeComponent();
        Build(store);
    }

    // Parameterless ctor required by the XAML loader.
    public ValueAnalyzerDialog() : this(new GameStore()) { }

    private void Build(IItemStore store)
    {
        // Collect all trackable games — DigitalGame, Game, PhysicalGame, etc.
        // The IValueAnalyzer interface only accepts Game, so we compute the
        // same formulas directly from BaseTrackableGame to cover every subtype.
        var games = store.All()
            .OfType<BaseTrackableGame>()
            .ToList();

        // ── Summary numbers ──────────────────────────────────────────────────
        decimal totalSpent    = games.Where(g => g.Price.HasValue).Sum(g => g.Price!.Value);
        double  totalHours    = games.Sum(g => g.HoursPlayed);
        double  backlogTotal  = games
            .Select(g => CostPerHour_BacklogHrs(g).backlog)
            .Where(h => h.HasValue)
            .Sum(h => h!.Value);

        decimal? avgCph = null;
        if (totalHours > 0 && totalSpent > 0)
            avgCph = totalSpent / (decimal)totalHours;

        TotalSpentLabel.Text     = $"€{totalSpent:F2}";
        TotalHoursLabel.Text     = $"{totalHours:F0} h";
        AvgCostPerHourLabel.Text = avgCph.HasValue ? $"€{avgCph:F2}" : "—";
        BacklogHoursLabel.Text   = backlogTotal > 0 ? $"{backlogTotal:F0} h" : "—";
        GamesCountLabel.Text     = games.Count.ToString();

        // ── Build rows ───────────────────────────────────────────────────────
        var rows = games
            .Select(g => BuildRow(g))
            .OrderBy(r => r.RawCostPerHour ?? decimal.MaxValue)   // nulls (no price/hours) go last
            .ThenBy(r => r.Title)
            .ToList();

        GameRows.ItemsSource = rows;
    }

    private static (decimal? cph, double? backlog) CostPerHour_BacklogHrs(BaseTrackableGame g)
    {
        decimal? cph = g.HoursPlayed > 0 && g.Price.HasValue
            ? g.Price.Value / (decimal)g.HoursPlayed
            : null;

        double? backlog = g.AverageCompleteHours.HasValue
            ? Math.Max(0, g.AverageCompleteHours.Value - g.HoursPlayed)
            : null;

        return (cph, backlog);
    }

    private static AnalyzerRow BuildRow(BaseTrackableGame g)
    {
        var (cph, bhr) = CostPerHour_BacklogHrs(g);

        var (statusBg, statusFg) = StatusColours(g.Status);

        return new AnalyzerRow
        {
            Title            = g.Title,
            Platform         = g.Platform.ToString(),
            Status           = g.Status.ToString(),
            HoursPlayed      = $"{g.HoursPlayed:F0} h",
            Price            = g.Price.HasValue ? $"€{g.Price:F2}" : "Free",
            CostPerHour      = cph.HasValue ? $"€{cph:F2}" : "—",
            BacklogRemaining = bhr.HasValue ? $"{bhr:F0} h" : "—",
            RawCostPerHour   = cph,
            CostColour       = CostColour(cph),
            StatusBackground = new SolidColorBrush(Color.Parse(statusBg)),
            StatusForeground = new SolidColorBrush(Color.Parse(statusFg)),
        };
    }

    // Cost colour: green (great value) → yellow → red (expensive per hour)
    private static IBrush CostColour(decimal? cph) => cph switch
    {
        null           => new SolidColorBrush(Color.Parse("#585b70")),
        <= 0.50m       => new SolidColorBrush(Color.Parse("#a6e3a1")),   // green  ≤ €0.50/hr
        <= 1.50m       => new SolidColorBrush(Color.Parse("#f9e2af")),   // yellow ≤ €1.50/hr
        _              => new SolidColorBrush(Color.Parse("#f38ba8")),   // red    > €1.50/hr
    };

    private static (string bg, string fg) StatusColours(GameStatus s) => s switch
    {
        GameStatus.Playing   => ("#1e3a2f", "#a6e3a1"),
        GameStatus.Completed => ("#1e2a4a", "#89b4fa"),
        GameStatus.Backlog   => ("#3a3220", "#f9e2af"),
        GameStatus.Wishlist  => ("#2a1e3f", "#cba6f7"),
        GameStatus.Abandoned => ("#3d1f28", "#f38ba8"),
        _                    => ("#313244", "#a6adc8"),
    };

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}

