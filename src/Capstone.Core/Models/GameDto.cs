using SharpQuest.Contracts;

namespace Capstone.Core.Models;

/// <summary>
/// DTO for JSON serialization of a Game.
/// A record automatically gets structural equality — useful for tests.
/// </summary>
public record GameDto(
    string Id,
    string Title,
    string Platform,
    string Genre,
    int? ReleaseYear,
    double HoursPlayed,
    decimal? Price,
    string Status,
    int? Rating
) : IItem
{
    public string DisplayName => Title;
}
