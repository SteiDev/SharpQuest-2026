using Capstone.Core.Models.Interfaces;

namespace Capstone.Core.Models;

/// <summary>A game owned physically (disc, cartridge).</summary>
public class PhysicalGame : BaseTrackableGame
{
    public string? StoreLocation { get; init; }

    public override string Describe() => $"[Physical] {Title} on {Platform} — {HoursPlayed}h played";
}
