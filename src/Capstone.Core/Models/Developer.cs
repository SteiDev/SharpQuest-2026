using Capstone.Core.Models.Interfaces;

namespace Capstone.Core.Models;

public class Developer
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Country { get; set; } = "Unknown";

    public int? FoundedYear { get; set; }

    public ICollection<BaseTrackableGame> Games { get; set; } = new List<BaseTrackableGame>();
}