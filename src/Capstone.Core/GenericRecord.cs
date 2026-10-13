namespace Capstone.Core.Models;

public abstract class GenericRecord
{
    private string title { get; set; } = string.Empty;
    private string description { get; set; } = string.Empty;
    private IEnumerable<string> publishers { get; set; } = new List<string>();
    private int releaseYear { get; set; } = 0;
    private int releaseMonth { get; set; } = 0;
}