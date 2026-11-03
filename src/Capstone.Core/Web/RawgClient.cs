using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Capstone.Core.Web;

/// <summary>
/// Fetches game metadata from the RAWG API (https://rawg.io/apidocs).
/// The API key is read from the RAWG_API_KEY environment variable — never hard-coded.
/// </summary>
public class RawgClient
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;

    public RawgClient(HttpClient? http = null)
    {
        _http = http ?? new HttpClient { BaseAddress = new Uri("https://api.rawg.io/api/") };
        _apiKey = Environment.GetEnvironmentVariable("RAWG_API_KEY");
    }

    /// <summary>
    /// Searches for games by title and returns the first page of results.
    /// Returns an empty list if the API key is not set or the call fails.
    /// </summary>
    public async Task<IReadOnlyList<RawgGameSummary>> SearchAsync(string title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return [];
        try
        {
            var url = $"games?key={_apiKey}&search={Uri.EscapeDataString(title)}&page_size=10";
            var response = await _http.GetFromJsonAsync<RawgPagedResult>(url, ct);
            return response?.Results ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Fetches a specific game by its RAWG id. Returns null on failure.
    /// </summary>
    public async Task<RawgGameDetail?> GetByIdAsync(int rawgId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return null;
        try
        {
            var url = $"games/{rawgId}?key={_apiKey}";
            return await _http.GetFromJsonAsync<RawgGameDetail>(url, ct);
        }
        catch
        {
            return null;
        }
    }
}

public record RawgPagedResult(
    [property: JsonPropertyName("count")] int Count,
    [property: JsonPropertyName("results")] IReadOnlyList<RawgGameSummary> Results
);

public record RawgGameSummary(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("released")] string? Released,
    [property: JsonPropertyName("genres")] IReadOnlyList<RawgGenre> Genres
)
{
    public string? PrimaryGenre => Genres.FirstOrDefault()?.Name;
    public int? ReleaseYear => int.TryParse(Released?.Split('-').FirstOrDefault(), out var y) ? y : null;
}

public record RawgGameDetail(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("released")] string? Released,
    [property: JsonPropertyName("genres")] IReadOnlyList<RawgGenre> Genres,
    [property: JsonPropertyName("description_raw")] string? Description,
    [property: JsonPropertyName("developers")] IReadOnlyList<RawgDeveloper> Developers
)
{
    public string? PrimaryGenre => Genres.FirstOrDefault()?.Name;
    public string? PrimaryDeveloper => Developers.FirstOrDefault()?.Name;
    public int? ReleaseYear => int.TryParse(Released?.Split('-').FirstOrDefault(), out var y) ? y : null;
}

public record RawgGenre(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name
);

public record RawgDeveloper(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name
);
