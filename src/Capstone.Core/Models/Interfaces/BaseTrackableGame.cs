using Capstone.Core.Models.Enums;

namespace Capstone.Core.Models.Interfaces;

public abstract class BaseTrackableGame
{
    private string Title { get; } = string.Empty;
    private Developer Developer { get; } = new Developer();
    private GameStatus? Status { get; }
    private decimal? Price { get; }
    private int? Rating { get; }
    private string? Description { get; }
    private decimal? CostPerHour { get; }
    private int? ReleaseYear { get; }
    private int? ReleaseMonth { get; }
    private Platform Platform { get; } = Platform.Other;
    private double? AverageCompleteHours { get; }
}