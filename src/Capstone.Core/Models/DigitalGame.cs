using Capstone.Core.Models.Interfaces;

namespace Capstone.Core.Models;

/// <summary>A game owned digitally (download, key).</summary>
public class DigitalGame : BaseTrackableGame
{
    public string? StoreFront { get; init; }

    public override string Describe() => $"[Digital] {Title} on {Platform} — {HoursPlayed}h, {(Price.HasValue ? $"£{Price:F2}" : "free")}";
}
