namespace Capstone.Core.Models.Interfaces;

/// <summary>
/// Provides value-for-money analysis over a game library.
/// </summary>
public interface IValueAnalyzer
{
    /// <summary>
    /// Cost per hour played. Returns null if the game has zero hours played or has no price.
    /// </summary>
    decimal? CostPerHour(Game game);

    /// <summary>
    /// Estimated hours remaining to finish the game based on average completion hours and hours already played.
    /// Returns null if average completion hours is not set.
    /// </summary>
    double? BacklogHoursRemaining(Game game);
}
