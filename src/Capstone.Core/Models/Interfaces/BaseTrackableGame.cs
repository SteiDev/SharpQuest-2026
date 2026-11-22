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
    public string? Description { get; set; }
    public int? ReleaseYear { get; init; }
    public int? ReleaseMonth { get; init; }
    public Platform Platform { get; init; } = Platform.Other;

    /// <summary>Hours already played.</summary>
    public double HoursPlayed { get; set; }

    /// <summary>Community / HLTB average hours to complete, for backlog estimation.</summary>
    public double? AverageCompleteHours { get; set; }

    /// <summary>Genre tag (e.g. "RPG", "FPS").</summary>
    public string? Genre { get; init; }

    /// <summary>Publisher (e.g. "Sony Interactive Entertainment", "Devolver Digital").</summary>
    public string? Publisher { get; set; }

    /// <summary>Marks the game as a favourite so it floats to the top of every list.</summary>
    public bool IsFavorite { get; set; }

    /// <summary>Returns a human-readable description of this game entry.</summary>
    public abstract string Describe();
}