using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Capstone.Core.Web;

/// <summary>
/// Fetches game metadata from the IGDB API (https://api-docs.igdb.com).
/// IGDB uses Twitch OAuth2: credentials are read from IGDB_CLIENT_ID and
/// IGDB_CLIENT_SECRET environment variables — never hard-coded.
/// </summary>
public class IgdbClient
{
    private readonly HttpClient _http;
    private readonly string? _clientId;
    private readonly string? _clientSecret;

    // Cached OAuth2 token
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private const string ApiBase  = "https://api.igdb.com/v4/";
    private const string TokenUrl = "https://id.twitch.tv/oauth2/token";

    public IgdbClient(HttpClient? http = null)
    {
        _http         = http ?? new HttpClient();
        _clientId     = Environment.GetEnvironmentVariable("IGDB_CLIENT_ID");
        _clientSecret = Environment.GetEnvironmentVariable("IGDB_CLIENT_SECRET");
    }

    // ── Token management ──────────────────────────────────────────────────────

    private async Task<string?> GetTokenAsync(CancellationToken ct)
    {
        if (_accessToken is not null && DateTime.UtcNow < _tokenExpiry)
            return _accessToken;

        if (string.IsNullOrWhiteSpace(_clientId) || string.IsNullOrWhiteSpace(_clientSecret))
            return null;

        try
        {
            var url = $"{TokenUrl}?client_id={_clientId}&client_secret={_clientSecret}&grant_type=client_credentials";
            var response = await _http.PostAsync(url, content: null, ct);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<TwitchTokenResponse>(cancellationToken: ct);
            if (token is null) return null;

            _accessToken = token.AccessToken;
            // Refresh 60 s before actual expiry to avoid edge-case failures
            _tokenExpiry = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 60);
            return _accessToken;
        }
        catch
        {
            return null;
        }
    }

    // ── Shared request helper ─────────────────────────────────────────────────

    private async Task<IReadOnlyList<IgdbGame>> PostQueryAsync(string apicalypseBody, CancellationToken ct)
    {
        var token = await GetTokenAsync(ct);
        if (token is null || string.IsNullOrWhiteSpace(_clientId)) return [];

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ApiBase + "games")
            {
                Content = new StringContent(apicalypseBody)
            };
            request.Headers.Add("Client-ID", _clientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<IReadOnlyList<IgdbGame>>(cancellationToken: ct) ?? [];
        }
        catch
        {
            return [];
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Searches for games by title. Returns up to 10 results, or an empty list
    /// when credentials are missing or the call fails.
    /// </summary>
    public async Task<IReadOnlyList<IgdbGame>> SearchAsync(string title, CancellationToken ct = default)
    {
        var query = $"search \"{title.Replace("\"", "\\\"")}\"; " +
                    "fields name,first_release_date,genres.name,involved_companies.company.name; " +
                    "limit 10;";
        return await PostQueryAsync(query, ct);
    }

    /// <summary>
    /// Fetches a single game by its IGDB numeric id. Returns null when not found
    /// or when credentials are missing.
    /// </summary>
    public async Task<IgdbGame?> GetByIdAsync(int igdbId, CancellationToken ct = default)
    {
        var query = $"where id = {igdbId}; " +
                    "fields name,first_release_date,genres.name,involved_companies.company.name,summary;";
        var results = await PostQueryAsync(query, ct);
        return results.FirstOrDefault();
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

/// <summary>A game entry returned by the IGDB games endpoint.</summary>
public record IgdbGame(
    [property: JsonPropertyName("id")]                int               Id,
    [property: JsonPropertyName("name")]              string            Name,
    [property: JsonPropertyName("first_release_date")]long?             FirstReleaseDate,
    [property: JsonPropertyName("genres")]            IReadOnlyList<IgdbGenre>?           Genres,
    [property: JsonPropertyName("involved_companies")]IReadOnlyList<IgdbInvolvedCompany>? Companies,
    [property: JsonPropertyName("summary")]           string?           Summary
)
{
    /// <summary>The first listed genre name, or null.</summary>
    public string? PrimaryGenre => Genres?.FirstOrDefault()?.Name;

    /// <summary>Release year derived from the Unix timestamp IGDB returns.</summary>
    public int? ReleaseYear => FirstReleaseDate.HasValue
        ? DateTimeOffset.FromUnixTimeSeconds(FirstReleaseDate.Value).Year
        : null;

    /// <summary>The first developer/publisher name listed, or null.</summary>
    public string? PrimaryCompany => Companies?.FirstOrDefault()?.Company?.Name;
}

public record IgdbGenre(
    [property: JsonPropertyName("id")]   int    Id,
    [property: JsonPropertyName("name")] string Name
);

public record IgdbInvolvedCompany(
    [property: JsonPropertyName("id")]      int           Id,
    [property: JsonPropertyName("company")] IgdbCompany?  Company
);

public record IgdbCompany(
    [property: JsonPropertyName("id")]   int    Id,
    [property: JsonPropertyName("name")] string Name
);

/// <summary>Twitch OAuth2 token response.</summary>
internal record TwitchTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")]   int    ExpiresIn
);

