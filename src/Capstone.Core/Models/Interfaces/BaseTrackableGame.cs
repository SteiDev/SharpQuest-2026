using Capstone.Core.Models.Enums;
using SharpQuest.Contracts;

namespace Capstone.Core.Models.Interfaces;

/// <summary>
/// Base class for anything in the game library that can be tracked.
/// Implements IItem so it can be stored and found in an IItemStore.
/// </summary>
public abstract class BaseTrackableGame : IItem
{
    /// <summary>Unique identifier for this game entry.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable name (the game title).</summary>
    public string Title { get; init; } = string.Empty;

    /// <inheritdoc/>
    public string DisplayName => Title;

    public Developer Developer { get; init; } = new Developer();
    public GameStatus Status { get; set; } = GameStatus.Backlog;
    public decimal? Price { get; init; }
    public int? Rating { get; set; }   // null = haven't rated it
    public string? Description { get; init; }
    public int? ReleaseYear { get; init; }
    public int? ReleaseMonth { get; init; }
    public Platform Platform { get; init; } = Platform.Other;

    /// <summary>Hours already played.</summary>
    public double HoursPlayed { get; set; }

    /// <summary>Community / HLTB average hours to complete, for backlog estimation.</summary>
    public double? AverageCompleteHours { get; init; }

    /// <summary>Genre tag (e.g. "RPG", "FPS").</summary>
    public string? Genre { get; init; }
}