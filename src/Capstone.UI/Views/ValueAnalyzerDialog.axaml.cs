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
    public string Title            { get; init; } = string.Empty;
    public string Platform         { get; init; } = string.Empty;
    public string Status           { get; init; } = string.Empty;
    public string HoursPlayed      { get; init; } = string.Empty;
    public string Price            { get; init; } = string.Empty;
    public string CostPerHour      { get; init; } = string.Empty;
    public string BacklogRemaining { get; init; } = string.Empty;

    // Visual helpers (colour-coded cost and status pill)
    public IBrush CostColour       { get; init; } = Brushes.Gray;
    public IBrush StatusBackground { get; init; } = Brushes.Transparent;
    public IBrush StatusForeground { get; init; } = Brushes.Gray;

    // Raw values used for sorting
    public decimal? RawCostPerHour   { get; init; }
    public decimal? RawPrice         { get; init; }
    public double   RawHours         { get; init; }
    public double?  RawBacklogHrs    { get; init; }
    public string   RawStatus        { get; init; } = string.Empty;
}

// ── Dialog ────────────────────────────────────────────────────────────────────

public partial class ValueAnalyzerDialog : Window
{
    // All rows, unsorted — sort is applied on top without rebuilding them.
    private List<AnalyzerRow> _allRows = new();

    // Current sort state
    private string _sortColumn    = "CostPerHour";   // default sort
    private bool   _sortAscending = true;

    // Map column tag → the sort-indicator TextBlock
    private Dictionary<string, TextBlock> _indicators = new();

    public ValueAnalyzerDialog(IItemStore store)
    {
        InitializeComponent();
        Build(store);
    }

    // Parameterless ctor required by the XAML loader.
    public ValueAnalyzerDialog() : this(new GameStore()) { }

    private void Build(IItemStore store)
    {
        // Wire up sort-indicator TextBlocks by name
        _indicators = new Dictionary<string, TextBlock>
        {
            ["Game"]       = SortGame,
            ["Status"]     = SortStatus,
            ["Hours"]      = SortHours,
            ["Price"]      = SortPrice,
            ["CostPerHour"]= SortCostPerHour,
            ["BacklogHrs"] = SortBacklogHrs,
        };

        // Collect all trackable games — DigitalGame, Game, PhysicalGame, etc.
        var games = store.All()
            .OfType<BaseTrackableGame>()
            .ToList();

        // ── Summary numbers ──────────────────────────────────────────────────
        decimal totalSpent   = games.Where(g => g.Price.HasValue).Sum(g => g.Price!.Value);
        double  totalHours   = games.Sum(g => g.HoursPlayed);
        double  backlogTotal = games
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
        _allRows = games.Select(BuildRow).ToList();

        ApplySort();
    }

    // ── Column header click ───────────────────────────────────────────────────

    private void OnColumnHeaderClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string col) return;

        if (_sortColumn == col)
            _sortAscending = !_sortAscending;   // same column → flip direction
        else
        {
            _sortColumn    = col;
            _sortAscending = true;              // new column → start ascending
        }

        ApplySort();
    }

    private void ApplySort()
    {
        // Sort the rows
        IEnumerable<AnalyzerRow> sorted = _sortColumn switch
        {
            "Game"       => Order(_allRows, r => r.Title),
            "Status"     => Order(_allRows, r => r.RawStatus),
            "Hours"      => Order(_allRows, r => r.RawHours),
            "Price"      => Order(_allRows, r => r.RawPrice ?? decimal.MaxValue),
            "CostPerHour"=> Order(_allRows, r => r.RawCostPerHour ?? decimal.MaxValue),
            "BacklogHrs" => Order(_allRows, r => r.RawBacklogHrs ?? double.MaxValue),
            _            => _allRows,
        };

        GameRows.ItemsSource = sorted.ToList();

        // Update all indicator TextBlocks
        foreach (var (col, tb) in _indicators)
        {
            if (col == _sortColumn)
                tb.Text = _sortAscending ? " ▲" : " ▼";
            else
                tb.Text = string.Empty;
        }
    }

    private IEnumerable<AnalyzerRow> Order<TKey>(
        IEnumerable<AnalyzerRow> rows, Func<AnalyzerRow, TKey> key)
        where TKey : IComparable<TKey>
        => _sortAscending
            ? rows.OrderBy(key).ThenBy(r => r.Title)
            : rows.OrderByDescending(key).ThenBy(r => r.Title);

    // ── Row builder ───────────────────────────────────────────────────────────

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
            // Raw sort keys
            RawCostPerHour   = cph,
            RawPrice         = g.Price,
            RawHours         = g.HoursPlayed,
            RawBacklogHrs    = bhr,
            RawStatus        = g.Status.ToString(),
            // Visuals
            CostColour       = CostColour(cph),
            StatusBackground = new SolidColorBrush(Color.Parse(statusBg)),
            StatusForeground = new SolidColorBrush(Color.Parse(statusFg)),
        };
    }

    // Cost colour: green (great value) → yellow → red (expensive per hour)
    private static IBrush CostColour(decimal? cph) => cph switch
    {
        null     => new SolidColorBrush(Color.Parse("#585b70")),
        <= 0.50m => new SolidColorBrush(Color.Parse("#a6e3a1")),   // green  ≤ €0.50/hr
        <= 1.50m => new SolidColorBrush(Color.Parse("#f9e2af")),   // yellow ≤ €1.50/hr
        _        => new SolidColorBrush(Color.Parse("#f38ba8")),   // red    > €1.50/hr
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
