using Capstone.Core.Models.Interfaces;

namespace Capstone.Core.Models;

/// <summary>A game developer / publisher.</summary>
public class Developer
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Country { get; init; }
    public int? FoundedYear { get; init; }
    public ICollection<BaseTrackableGame> Games { get; init; } = new List<BaseTrackableGame>();
}