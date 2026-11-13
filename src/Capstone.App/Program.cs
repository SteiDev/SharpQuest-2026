using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
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

// Populate sample games
var lab04 = new Lab04Wiring();
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

Console.WriteLine($"\n--- Library contains {store.Count} games ---");

// Lab 04: Polymorphic description and classification
Console.WriteLine("\n--- Inventory (Polymorphism & Classification) ---");
foreach (var item in store)
{
    Console.WriteLine($"  [{lab04.Classify(item)}] {lab04.Describe(item)}");
}

// Lab 05: Generic RecentlyPlayed<T>
var recent = new RecentlyPlayed<string>(capacity: 3);
recent.Push("Hollow Knight");
recent.Push("Balatro");
recent.Push("Dark Souls III");
recent.Push("Balatro"); // Balatro moves to front
Console.WriteLine("\n--- Recently Played Games (Newest First) ---");
foreach (var title in recent.ToList())
{
    Console.WriteLine($"  - {title}");
}

// Online check with IGDB API (if requested with --online)
// Requires IGDB_CLIENT_ID and IGDB_CLIENT_SECRET environment variables.
if (args.Contains("--online"))
{
    Console.WriteLine("\n--- Querying IGDB API ---");
    var client = new IgdbClient();
    try
    {
        var results = await client.SearchAsync("Elden Ring");
        if (results.Count > 0)
        {
            var first = results[0];
            Console.WriteLine($"Found on IGDB: {first.Name} (Year: {first.ReleaseYear}, Genre: {first.PrimaryGenre}, Company: {first.PrimaryCompany})");
        }
        else
        {
            Console.WriteLine("No results found or IGDB_CLIENT_ID / IGDB_CLIENT_SECRET environment variables not set.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"IGDB API call failed: {ex.Message}");
    }
}
