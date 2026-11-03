using Capstone.Core.Models.Interfaces;

namespace Capstone.Core.Models;

/// <summary>DLC (Downloadable Content) attached to a game.</summary>
public class DLC : BaseTrackableGame
{
    public Game PartOf { get; init; } = null!;

    public override string Describe() => $"[DLC] {Title} (part of {PartOf?.Title ?? "unknown"})";
}