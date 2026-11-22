using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;

namespace Capstone.Core.Web;

/// <summary>
/// Fetches a Steam user's owned games via the public Steam Web API
/// (IPlayerService/GetOwnedGames v0001).
///
/// Requires:
///   • A Steam Web API key  (get one at https://steamcommunity.com/dev/apikey)
///   • The target Steam64 ID  (the 17-digit numeric account id)
///
/// The game's profile must be public, or the request returns an empty list.
/// No OAuth is needed — the API key is passed as a query parameter.
/// </summary>
public class SteamImporter
{
    private readonly HttpClient _http;

    public SteamImporter(HttpClient? http = null)
        => _http = http ?? new HttpClient();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all owned games for the given Steam user.
    /// Throws <see cref="SteamImportException"/> on API or network errors.
    /// Returns an empty list when the profile is private or has no games.
    /// </summary>
    public async Task<IReadOnlyList<SteamGame>> GetOwnedGamesAsync(
        string apiKey,
        string steamId64,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("Steam API key must not be blank.", nameof(apiKey));
        if (string.IsNullOrWhiteSpace(steamId64))
            throw new ArgumentException("Steam ID must not be blank.", nameof(steamId64));

        var url = "https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/" +
                  $"?key={Uri.EscapeDataString(apiKey)}" +
                  $"&steamid={Uri.EscapeDataString(steamId64)}" +
                  "&include_appinfo=1" +
                  "&include_played_free_games=1" +
                  "&format=json";

        SteamOwnedGamesRoot? root;
        try
        {
            root = await _http.GetFromJsonAsync<SteamOwnedGamesRoot>(url, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new SteamImportException($"Network error: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new SteamImportException($"Unexpected error: {ex.Message}", ex);
        }

        return root?.Response?.Games ?? [];
    }

    // ── Conversion helper ─────────────────────────────────────────────────────

    /// <summary>
    /// Converts a <see cref="SteamGame"/> to a <see cref="DigitalGame"/> ready to
    /// add to the SharpQuest library.
    /// </summary>
    public static DigitalGame ToDigitalGame(SteamGame steam, GameStatus defaultStatus = GameStatus.Backlog)
    {
        // Guess status from playtime: >0 min played → "Playing" unless already done.
        var status = steam.PlaytimeForever > 0 ? GameStatus.Playing : defaultStatus;

        return new DigitalGame
        {
            Id          = $"steam-{steam.AppId}",
            Title       = steam.Name,
            Platform    = Platform.PC,
            Status      = status,
            HoursPlayed = Math.Round(steam.PlaytimeForever / 60.0, 1),
            StoreFront  = "Steam",
            Description = $"Steam App ID: {steam.AppId}",
            Developer   = new Models.Developer { Name = string.Empty },
        };
    }
}

// ── Exception ─────────────────────────────────────────────────────────────────

public class SteamImportException : Exception
{
    public SteamImportException(string message, Exception? inner = null)
        : base(message, inner) { }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record SteamOwnedGamesRoot(
    [property: JsonPropertyName("response")] SteamOwnedGamesResponse? Response
);

public record SteamOwnedGamesResponse(
    [property: JsonPropertyName("game_count")] int               GameCount,
    [property: JsonPropertyName("games")]      IReadOnlyList<SteamGame>? Games
);

public record SteamGame(
    [property: JsonPropertyName("appid")]            int    AppId,
    [property: JsonPropertyName("name")]             string Name,
    [property: JsonPropertyName("playtime_forever")] int    PlaytimeForever   // minutes
)
{
    /// <summary>Hours played, rounded to one decimal place.</summary>
    public double HoursPlayed => Math.Round(PlaytimeForever / 60.0, 1);

    /// <summary>Row label shown in the selection list.</summary>
    public string DisplayLabel =>
        PlaytimeForever > 0
            ? $"{Name}  ({HoursPlayed}h played)"
            : Name;
}

