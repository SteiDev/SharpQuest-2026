using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;
using Microsoft.Data.Sqlite;

namespace Capstone.Core.Models.Storage;

/// <summary>
/// SQLite-backed persistence for the game library.
/// The database file lives next to the executable in <c>sharpquest.db</c>.
/// </summary>
public sealed class GameRepository : IDisposable
{
    private readonly SqliteConnection _conn;

    // ── Construction / schema ─────────────────────────────────────────────────

    public GameRepository(string dbPath)
    {
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Games (
                Id                   TEXT    PRIMARY KEY NOT NULL,
                Title                TEXT    NOT NULL,
                Kind                 TEXT    NOT NULL DEFAULT 'Digital',
                Platform             TEXT    NOT NULL DEFAULT 'Other',
                Status               TEXT    NOT NULL DEFAULT 'Backlog',
                Genre                TEXT,
                DeveloperName        TEXT    NOT NULL DEFAULT '',
                HoursPlayed          REAL    NOT NULL DEFAULT 0,
                Price                REAL,
                ReleaseYear          INTEGER,
                Rating               INTEGER,
                Publisher            TEXT,
                AverageCompleteHours REAL,
                Description          TEXT,
                IsFavorite           INTEGER NOT NULL DEFAULT 0
            );
            """;
        cmd.ExecuteNonQuery();

        TryAddColumn("Publisher", "TEXT");
        TryAddColumn("AverageCompleteHours", "REAL");
        TryAddColumn("Description", "TEXT");
        TryAddColumn("IsFavorite", "INTEGER NOT NULL DEFAULT 0");
    }

    private void TryAddColumn(string col, string type)
    {
        try
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = $"ALTER TABLE Games ADD COLUMN {col} {type};";
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Column already exists
        }
    }

    // ── Read ──────────────────────────────────────────────────────────────────

    /// <summary>Loads all persisted games, in insertion order.</summary>
    public IReadOnlyList<BaseTrackableGame> LoadAll()
    {
        var list = new List<BaseTrackableGame>();

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT Id,Title,Kind,Platform,Status,Genre,DeveloperName,HoursPlayed,Price,ReleaseYear,Rating,Publisher,AverageCompleteHours,Description,IsFavorite FROM Games ORDER BY rowid;";

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var kind     = reader.GetString(2);
            var platform = ParseEnum<Platform>(reader.GetString(3));
            var status   = ParseEnum<GameStatus>(reader.GetString(4));

            BaseTrackableGame game = kind == "Physical"
                ? new PhysicalGame()
                : new DigitalGame();

            SetInit(game, nameof(game.Id),                   reader.GetString(0));
            SetInit(game, nameof(game.Title),                reader.GetString(1));
            SetInit(game, nameof(game.Platform),             platform);
            SetProp(game, nameof(game.Status),               status);
            SetInit(game, nameof(game.Genre),                reader.IsDBNull(5)  ? null : reader.GetString(5));
            SetInit(game, "Developer",                       new Developer { Name = reader.GetString(6) });
            SetProp(game, nameof(game.HoursPlayed),           reader.GetDouble(7));
            SetInit(game, nameof(game.Price),                reader.IsDBNull(8)  ? (decimal?)null : (decimal)reader.GetDouble(8));
            SetInit(game, nameof(game.ReleaseYear),           reader.IsDBNull(9)  ? (int?)null : reader.GetInt32(9));
            SetProp(game, nameof(game.Rating),                reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10));
            SetProp(game, nameof(game.Publisher),             reader.IsDBNull(11) ? null : reader.GetString(11));
            SetProp(game, nameof(game.AverageCompleteHours), reader.IsDBNull(12) ? (double?)null : reader.GetDouble(12));
            SetProp(game, nameof(game.Description),          reader.IsDBNull(13) ? null : reader.GetString(13));
            SetProp(game, nameof(game.IsFavorite),            !reader.IsDBNull(14) && reader.GetInt32(14) != 0);

            list.Add(game);
        }

        return list;
    }

    // ── Write ─────────────────────────────────────────────────────────────────

    /// <summary>Inserts a new game row.</summary>
    public void Insert(BaseTrackableGame game)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Games (Id,Title,Kind,Platform,Status,Genre,DeveloperName,HoursPlayed,Price,ReleaseYear,Rating,Publisher,AverageCompleteHours,Description,IsFavorite)
            VALUES ($id,$title,$kind,$platform,$status,$genre,$dev,$hours,$price,$year,$rating,$publisher,$completionHours,$description,$isFavorite);
            """;
        BindParams(cmd, game);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Updates all mutable columns for an existing game.</summary>
    public void Update(BaseTrackableGame game)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            UPDATE Games SET
                Title=$title, Kind=$kind, Platform=$platform, Status=$status,
                Genre=$genre, DeveloperName=$dev, HoursPlayed=$hours,
                Price=$price, ReleaseYear=$year, Rating=$rating,
                Publisher=$publisher, AverageCompleteHours=$completionHours, Description=$description,
                IsFavorite=$isFavorite
            WHERE Id=$id;
            """;
        BindParams(cmd, game);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Deletes a game by Id.</summary>
    public void Delete(string id)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Games WHERE Id=$id;";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void BindParams(SqliteCommand cmd, BaseTrackableGame game)
    {
        cmd.Parameters.AddWithValue("$id",       game.Id);
        cmd.Parameters.AddWithValue("$title",    game.Title);
        cmd.Parameters.AddWithValue("$kind",     game is PhysicalGame ? "Physical" : "Digital");
        cmd.Parameters.AddWithValue("$platform", game.Platform.ToString());
        cmd.Parameters.AddWithValue("$status",   game.Status.ToString());
        cmd.Parameters.AddWithValue("$genre",    (object?)game.Genre ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$dev",      game.Developer.Name);
        cmd.Parameters.AddWithValue("$hours",    game.HoursPlayed);
        cmd.Parameters.AddWithValue("$price",    game.Price.HasValue ? (object)(double)game.Price.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("$year",            (object?)game.ReleaseYear ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$rating",          (object?)game.Rating ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$publisher",       (object?)game.Publisher ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$completionHours", (object?)game.AverageCompleteHours ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$description",     (object?)game.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$isFavorite",      game.IsFavorite ? 1 : 0);
    }

    private static T ParseEnum<T>(string s) where T : struct, Enum =>
        Enum.TryParse<T>(s, ignoreCase: true, out var v) ? v : default;

    private static void SetProp(object obj, string name, object? value)
    {
        var prop = obj.GetType().GetProperty(name,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        prop?.SetValue(obj, value);
    }

    // init-only setters are still settable by reflection at runtime.
    private static void SetInit(object obj, string name, object? value) =>
        SetProp(obj, name, value);

    public void Dispose() => _conn.Dispose();
}

