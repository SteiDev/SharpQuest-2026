namespace Capstone.Core.Models;

public interface ITrackableGame {
    string Title { get; }
    GameStatus Status { get; }
    double HoursPlayed { get; }

    decimal computeCostPerHour();
}