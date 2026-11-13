using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;
using Capstone.Core.Web;
using Capstone.Core.Wiring;

// Load .env file from the project root (or the directory containing the executable).
// Lines starting with # are comments; KEY=VALUE pairs are set as environment variables
// only when the variable isn't already set (so real env vars always take precedence).
LoadDotEnv(Path.Combine(AppContext.BaseDirectory, ".env"));
LoadDotEnv(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

static void LoadDotEnv(string path)
{
    if (!File.Exists(path)) return;
    foreach (var raw in File.ReadAllLines(path))
    {
        var line = raw.Trim();
        if (line.Length == 0 || line.StartsWith('#')) continue;
        var eq = line.IndexOf('=');
        if (eq <= 0) continue;
        var key   = line[..eq].Trim();
        var value = line[(eq + 1)..].Trim().Trim('"').Trim('\'');
        if (!string.IsNullOrEmpty(key) && Environment.GetEnvironmentVariable(key) is null)
            Environment.SetEnvironmentVariable(key, value);
    }
}

var store = new GameStore();

// Lab 06: Event listener
store.ItemAdded += item => Console.WriteLine($"[Event] Added: {item.DisplayName}");

// Seed a few sample games
store.Add(new PhysicalGame
{
    Id = "gm-001",
    Title = "Hollow Knight",
    Platform = Platform.PC,
    HoursPlayed = 45,
    Price = 14.99m,
    Status = GameStatus.Completed,
    ReleaseYear = 2017,
    Genre = "Metroidvania",
    Developer = new Developer { Name = "Team Cherry", Country = "Australia" }
});
store.Add(new DigitalGame
{
    Id = "gm-002",
    Title = "Dark Souls III",
    Platform = Platform.PlayStation4,
    HoursPlayed = 110,
    Price = 59.99m,
    Status = GameStatus.Completed,
    ReleaseYear = 2016,
    Genre = "Action RPG",
    Developer = new Developer { Name = "FromSoftware", Country = "Japan" }
});
store.Add(new DigitalGame
{
    Id = "gm-003",
    Title = "Balatro",
    Platform = Platform.NintendoSwitch,
    HoursPlayed = 75,
    Price = 14.99m,
    Status = GameStatus.Playing,
    ReleaseYear = 2024,
    Genre = "Roguelike Deckbuilder",
    Developer = new Developer { Name = "LocalThunk", Country = "Canada" }
});

// ── REPL ─────────────────────────────────────────────────────────────────────

Console.WriteLine("Game Library — type /help for available commands.");
Console.WriteLine();

var lab04 = new Lab04Wiring();
int nextId = 4;

while (true)
{
    Console.Write("> ");
    var input = Console.ReadLine();
    if (input is null) break;          // EOF (Ctrl-D)
    input = input.Trim();
    if (input.Length == 0) continue;

    var parts = Tokenise(input);
    var command = parts[0].ToLowerInvariant();

    switch (command)
    {
        case "/showgames":
            ShowGames(store, lab04);
            break;

        case "/addmanual":
            AddManual(store, parts[1..], ref nextId);
            break;

        case "/search":
        {
            if (parts.Length < 2) { Error("Usage: /search <text>"); break; }
            var query = string.Join(' ', parts[1..]);
            var hits  = store.Matching(query).ToList();
            if (hits.Count == 0) { Console.WriteLine("  No matches found."); break; }
            foreach (var g in hits) PrintGame(g, lab04);
            break;
        }

        case "/online":
        {
            var query = parts.Length > 1 ? string.Join(' ', parts[1..]) : "Elden Ring";
            await SearchIgdb(query);
            break;
        }

        case "/addigdb":
        {
            if (parts.Length < 2 || !int.TryParse(parts[1], out var igdbId))
            {
                Error("Usage: /addigdb <igdb_id> [type=Physical|Digital] [platform=PC] [status=Backlog]");
                break;
            }
            var opts = ParseKeyValues(parts[2..]);
            bool added = await AddFromIgdb(store, igdbId, opts, nextId);
            if (added) nextId++;
            break;
        }

        case "/help":
            PrintHelp();
            break;

        case "/quit":
        case "/exit":
            Console.WriteLine("Bye!");
            return;

        default:
            Error($"Unknown command '{parts[0]}'. Type /help for a list.");
            break;
    }

    Console.WriteLine();
}

// ── Helpers ───────────────────────────────────────────────────────────────────

static void ShowGames(GameStore store, Lab04Wiring lab04)
{
    if (store.Count == 0) { Console.WriteLine("  Library is empty."); return; }
    Console.WriteLine($"  {"#",-4} {"Type",-10} {"Id",-10} {"Title",-30} {"Platform",-16} {"Status",-12} {"Hours",6} {"Price",8} {"Year",6} {"Genre",-20} {"Developer",-20}");
    Console.WriteLine("  " + new string('─', 130));
    int i = 1;
    foreach (var item in store)
        PrintGame(item, lab04, i++);
}

static void PrintGame(SharpQuest.Contracts.IItem item, Lab04Wiring lab04, int index = 0)
{
    if (item is not BaseTrackableGame g) return;
    var num   = index > 0 ? $"{index,-4}" : "    ";
    var type  = lab04.Classify(g);
    var price = g.Price.HasValue ? $"£{g.Price:F2}" : "free";
    var year  = g.ReleaseYear.HasValue ? g.ReleaseYear.ToString()! : "—";
    Console.WriteLine($"  {num} {type,-10} {g.Id,-10} {g.Title,-30} {g.Platform,-16} {g.Status,-12} {g.HoursPlayed,6:F1} {price,8} {year,6} {g.Genre ?? "—",-20} {g.Developer.Name,-20}");
}

static void AddManual(GameStore store, string[] tokens, ref int nextId)
{
    // Parse KEY=VALUE or KEY VALUE pairs (both forms accepted).
    var kv = ParseKeyValues(tokens);

    bool isPhysical = kv.TryGetValue("type", out var t) && t.Equals("physical", StringComparison.OrdinalIgnoreCase);

    var id    = kv.GetValueOrDefault("id",    $"gm-{nextId:D3}");
    var title = kv.GetValueOrDefault("title", "(untitled)");

    Platform platform = Platform.Other;
    if (kv.TryGetValue("platform", out var pStr))
        Enum.TryParse(pStr, ignoreCase: true, out platform);

    GameStatus status = GameStatus.Backlog;
    if (kv.TryGetValue("status", out var sStr))
        Enum.TryParse(sStr, ignoreCase: true, out status);

    decimal? price = null;
    if (kv.TryGetValue("price", out var prStr) && decimal.TryParse(prStr, out var prVal))
        price = prVal;

    double hoursPlayed = 0;
    if (kv.TryGetValue("hoursplayed", out var hStr) && double.TryParse(hStr, out var hVal))
        hoursPlayed = hVal;

    int? releaseYear = null;
    if (kv.TryGetValue("releaseyear", out var ryStr) && int.TryParse(ryStr, out var ryVal))
        releaseYear = ryVal;

    var genre = kv.GetValueOrDefault("genre", null!);
    var devName = kv.GetValueOrDefault("developer", string.Empty);
    var devCountry = kv.GetValueOrDefault("country", null!);

    var developer = new Developer { Name = devName, Country = devCountry };

    BaseTrackableGame game = isPhysical
        ? new PhysicalGame
            { Id = id, Title = title, Platform = platform, Status = status, Price = price,
              HoursPlayed = hoursPlayed, ReleaseYear = releaseYear, Genre = genre, Developer = developer }
        : new DigitalGame
            { Id = id, Title = title, Platform = platform, Status = status, Price = price,
              HoursPlayed = hoursPlayed, ReleaseYear = releaseYear, Genre = genre, Developer = developer };

    try
    {
        store.Add(game);
        nextId++;
    }
    catch (Exception ex)
    {
        Error($"Could not add game: {ex.Message}");
    }
}

static async Task SearchIgdb(string query)
{
    Console.WriteLine($"  Searching IGDB for \"{query}\"...");
    var client  = new IgdbClient();
    var results = await client.SearchAsync(query);
    if (results.Count == 0)
    {
        Console.WriteLine("  No results (or credentials not set — check .env).");
        return;
    }
    foreach (var g in results)
        Console.WriteLine($"  [{g.Id}] {g.Name} ({g.ReleaseYear ?? 0}) — {g.PrimaryGenre ?? "?"} — {g.PrimaryCompany ?? "?"}");
}

/// <returns>True if the game was successfully added to the store.</returns>
static async Task<bool> AddFromIgdb(GameStore store, int igdbId, Dictionary<string, string> opts, int currentNextId)
{
    Console.WriteLine($"  Fetching IGDB id {igdbId}...");
    var client = new IgdbClient();
    IgdbGame? igdb;
    try { igdb = await client.GetByIdAsync(igdbId); }
    catch (Exception ex) { Error($"IGDB request failed: {ex.Message}"); return false; }

    if (igdb is null)
    {
        Error($"No game found with IGDB id {igdbId}. Check the id or your .env credentials.");
        return false;
    }

    // ── Map IGDB fields → local model ────────────────────────────────────────
    bool isPhysical = opts.TryGetValue("type", out var t) &&
                      t.Equals("physical", StringComparison.OrdinalIgnoreCase);

    var id = opts.GetValueOrDefault("id", $"igdb-{igdbId}");

    // Platform: use override if given, otherwise try to guess from the IGDB name
    Platform platform = Platform.Other;
    if (opts.TryGetValue("platform", out var pStr))
        Enum.TryParse(pStr, ignoreCase: true, out platform);

    // Status: default Backlog, overridable
    GameStatus status = GameStatus.Backlog;
    if (opts.TryGetValue("status", out var sStr))
        Enum.TryParse(sStr, ignoreCase: true, out status);

    // Price: optional override
    decimal? price = null;
    if (opts.TryGetValue("price", out var prStr) && decimal.TryParse(prStr, out var prVal))
        price = prVal;

    var developer = new Developer { Name = igdb.PrimaryCompany ?? string.Empty };

    BaseTrackableGame game = isPhysical
        ? new PhysicalGame
        {
            Id          = id,
            Title       = igdb.Name,
            Platform    = platform,
            Status      = status,
            Price       = price,
            ReleaseYear = igdb.ReleaseYear,
            Genre       = igdb.PrimaryGenre,
            Developer   = developer,
            Description = igdb.Summary
        }
        : new DigitalGame
        {
            Id          = id,
            Title       = igdb.Name,
            Platform    = platform,
            Status      = status,
            Price       = price,
            ReleaseYear = igdb.ReleaseYear,
            Genre       = igdb.PrimaryGenre,
            Developer   = developer,
            Description = igdb.Summary
        };

    try
    {
        store.Add(game);
        Console.WriteLine($"  ✓ Added [{game.Id}] {game.Title} ({game.ReleaseYear?.ToString() ?? "?"}) — {game.Genre ?? "?"} by {developer.Name}");
        if (igdb.Summary is { Length: > 0 } summary)
            Console.WriteLine($"    {summary[..Math.Min(120, summary.Length)]}{(summary.Length > 120 ? "…" : "")}");
        return true;
    }
    catch (Exception ex)
    {
        Error($"Could not add to library: {ex.Message}");
        return false;
    }
}

static void PrintHelp()
{
    Console.WriteLine("""
      Commands
      ────────────────────────────────────────────────────────────────────────
      /showgames
          List every game in the library.

      /addmanual KEY=VALUE [KEY=VALUE ...]
          Add a game with the given fields. KEY names (case-insensitive):
            Type        Physical | Digital  (default: Digital)
            Id          unique string id    (auto-assigned if omitted)
            Title       game title
            Platform    PC | PlayStation5 | NintendoSwitch | Xbox360 | …
            Status      Backlog | Playing | Completed | Abandoned | Wishlist
            Price       decimal (e.g. 24.99)
            HoursPlayed decimal (e.g. 12.5)
            ReleaseYear integer (e.g. 2023)
            Genre       free text
            Developer   developer / publisher name
            Country     developer country

          Example:
            /addmanual Type=Digital Title="Hades" Platform=PC Status=Completed Price=24.99 HoursPlayed=42 Genre=Roguelike Developer="Supergiant Games"

      /search <text>
          Find games whose title contains <text>.

      /online [<title>]
          Search IGDB for <title> (default: Elden Ring). Requires .env creds.
          Shows each result's IGDB numeric id, title, year, genre and developer.

      /addigdb <igdb_id> [KEY=VALUE ...]
          Fetch a game by its IGDB numeric id and add it to your library.
          Optional KEY=VALUE overrides (case-insensitive):
            Type        Physical | Digital  (default: Digital)
            Id          unique string id    (default: igdb-<igdb_id>)
            Platform    PC | PlayStation5 | NintendoSwitch | Xbox | …
            Status      Backlog | Playing | Completed | Abandoned | Wishlist
            Price       decimal (e.g. 24.99)

          Example:
            /addigdb 1877 Platform=PC Status=Playing Price=49.99

      /help
          Show this help.

      /quit  or  /exit
          Exit the program.
    """);
}

static void Error(string msg) => Console.WriteLine($"  ✗ {msg}");

/// <summary>
/// Splits the input line into tokens, respecting "quoted strings".
/// </summary>
static string[] Tokenise(string input)
{
    var tokens = new List<string>();
    var sb     = new System.Text.StringBuilder();
    bool inQ   = false;
    char qChar = '"';

    foreach (var ch in input)
    {
        if (inQ)
        {
            if (ch == qChar) inQ = false;
            else sb.Append(ch);
        }
        else if (ch == '"' || ch == '\'')
        {
            inQ   = true;
            qChar = ch;
        }
        else if (ch == ' ')
        {
            if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
        }
        else sb.Append(ch);
    }
    if (sb.Length > 0) tokens.Add(sb.ToString());
    return [.. tokens];
}

/// <summary>
/// Parses KEY=VALUE tokens into a case-normalised dictionary.
/// Also handles KEY VALUE pairs (alternating) as a fallback.
/// </summary>
static Dictionary<string, string> ParseKeyValues(string[] tokens)
{
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i < tokens.Length; i++)
    {
        var eq = tokens[i].IndexOf('=');
        if (eq > 0)
        {
            dict[tokens[i][..eq]] = tokens[i][(eq + 1)..];
        }
        else if (i + 1 < tokens.Length)
        {
            dict[tokens[i]] = tokens[++i];
        }
    }
    return dict;
}


