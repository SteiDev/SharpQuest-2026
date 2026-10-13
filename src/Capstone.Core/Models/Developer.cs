using Capstone.Core.Models.Interfaces;

namespace Capstone.Core.Models;

public class Developer
{
    private int Id { get; set; }
    private string Name { get; set; } = string.Empty;
    private string? Country { get; set; }
    private int? FoundedYear { get; set; }
    private ICollection<BaseTrackableGame> Games { get; set; } = new List<BaseTrackableGame>();
}