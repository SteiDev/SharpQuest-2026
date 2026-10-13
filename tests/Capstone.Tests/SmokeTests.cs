using Capstone.Core.Models;
using Capstone.Core.Models.Enums;

namespace Capstone.Tests;

// Session 3 — tests over your own domain logic.
// Feature chosen: Value for Money — cost per hour, backlog hours, edge cases.
public class ValueAnalyzerTests
{
    private readonly ValueAnalyzer _analyzer = new();

    private static Game MakeGame(
        double hoursPlayed = 0,
        decimal? price = null,
        double? averageCompleteHours = null,
        GameStatus status = GameStatus.Backlog) =>
        new Game
        {
            Id = "gm-001",
            Title = "Test Game",
            HoursPlayed = hoursPlayed,
            Price = price,
            AverageCompleteHours = averageCompleteHours,
            Status = status,
            Developer = new Developer { Name = "Test Dev" }
        };

    // --- CostPerHour tests ---

    [Fact]
    public void CostPerHour_returns_null_when_zero_hours_played()
    {
        // A game with 0 hours played has undefined cost-per-hour (avoid division by zero).
        var game = MakeGame(hoursPlayed: 0, price: 59.99m);

        var result = _analyzer.CostPerHour(game);

        Assert.Null(result);
    }

    [Fact]
    public void CostPerHour_returns_null_when_price_is_null()
    {
        // Free-to-play games or gifted games have no price — no cost per hour.
        var game = MakeGame(hoursPlayed: 10, price: null);

        var result = _analyzer.CostPerHour(game);

        Assert.Null(result);
    }

    [Fact]
    public void CostPerHour_is_correct_for_played_game_with_price()
    {
        // €60 over 20 hours = €3.00/hour
        var game = MakeGame(hoursPlayed: 20, price: 60m);

        var result = _analyzer.CostPerHour(game);

        Assert.NotNull(result);
        Assert.Equal(3.00m, result!.Value);
    }

    [Fact]
    public void CostPerHour_rounds_correctly_for_fractional_hours()
    {
        // €30 over 7 hours ≈ 4.2857... — should be decimal, not truncated
        var game = MakeGame(hoursPlayed: 7, price: 30m);

        var result = _analyzer.CostPerHour(game);

        Assert.NotNull(result);
        Assert.True(result!.Value > 4m && result.Value < 5m,
            $"Expected cost/hour between 4 and 5 for 30 / 7, got {result.Value}");
    }

    // --- BacklogHoursRemaining tests ---

    [Fact]
    public void BacklogHoursRemaining_returns_null_when_average_not_set()
    {
        // If we don't know how long the game takes, we can't estimate backlog.
        var game = MakeGame(hoursPlayed: 5, averageCompleteHours: null);

        var result = _analyzer.BacklogHoursRemaining(game);

        Assert.Null(result);
    }

    [Fact]
    public void BacklogHoursRemaining_is_zero_when_already_overplayed()
    {
        // Played more than average — backlog hours can't be negative, clamp to 0.
        var game = MakeGame(hoursPlayed: 80, averageCompleteHours: 50);

        var result = _analyzer.BacklogHoursRemaining(game);

        Assert.NotNull(result);
        Assert.Equal(0, result!.Value);
    }

    [Fact]
    public void BacklogHoursRemaining_is_average_when_zero_hours_played()
    {
        // Brand-new game in the backlog: all hours remain.
        var game = MakeGame(hoursPlayed: 0, averageCompleteHours: 40);

        var result = _analyzer.BacklogHoursRemaining(game);

        Assert.NotNull(result);
        Assert.Equal(40, result!.Value);
    }

    [Fact]
    public void BacklogHoursRemaining_subtracts_hours_already_played()
    {
        // 100-hour RPG, played 25 hours: 75 hours left.
        var game = MakeGame(hoursPlayed: 25, averageCompleteHours: 100);

        var result = _analyzer.BacklogHoursRemaining(game);

        Assert.NotNull(result);
        Assert.Equal(75, result!.Value);
    }
}

// Session 3 smoke test (kept from original).
public class SmokeTests
{
    [Fact]
    public void Arithmetic_still_works()
    {
        Assert.Equal(4, 2 + 2);
    }
}
