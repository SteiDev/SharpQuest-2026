using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;

namespace Capstone.Core.Models;

/// <summary>
/// Value-for-money analyzer: computes cost-per-hour and backlog hours.
/// </summary>
public class ValueAnalyzer : IValueAnalyzer
{
    /// <inheritdoc/>
    public decimal? CostPerHour(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        if (game.HoursPlayed <= 0) return null;
        if (game.Price is null) return null;
        return game.Price.Value / (decimal)game.HoursPlayed;
    }

    /// <inheritdoc/>
    public double? BacklogHoursRemaining(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        if (game.AverageCompleteHours is null) return null;
        var remaining = game.AverageCompleteHours.Value - game.HoursPlayed;
        return remaining < 0 ? 0 : remaining;
    }
}
