using System.Text.Json;
using System.Text.Json.Serialization;
using Capstone.Core.Models;
using Capstone.Core.Models.Interfaces;
using SharpQuest.Contracts;

namespace Capstone.Core.Services;

// ── DTOs (mirror Lab08Wiring's internal types, but public) ────────────────────

internal sealed class ExportFile
{
    [JsonPropertyName("items")]
    public List<ExportEnvelope> Items { get; set; } = new();
}

internal sealed class ExportEnvelope
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}

// ── Service ───────────────────────────────────────────────────────────────────

/// <summary>
/// Exports and imports the game library as a human-readable JSON file
/// using the same envelope format as Lab08Wiring.
/// </summary>
public static class LibraryExporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly IReadOnlyDictionary<string, Type> KindMap =
        new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            [nameof(PhysicalGame)] = typeof(PhysicalGame),
            [nameof(DigitalGame)]  = typeof(DigitalGame),
            [nameof(Game)]         = typeof(Game),
        };

    // ── Export ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Serialises all items to a JSON file at <paramref name="path"/>.
    /// Parent directories are created if they don't exist.
    /// </summary>
    public static async Task ExportAsync(IEnumerable<IItem> items, string path)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(path);
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path must not be blank.", nameof(path));

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var file = new ExportFile();
        foreach (var item in items)
        {
            var kind     = item.GetType().Name;
            var dataJson = JsonSerializer.SerializeToElement(item, item.GetType(), Options);
            file.Items.Add(new ExportEnvelope { Kind = kind, Data = dataJson });
        }

        var json = JsonSerializer.Serialize(file, Options);
        await File.WriteAllTextAsync(path, json);
    }

    // ── Import ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Deserialises items from a JSON file previously written by <see cref="ExportAsync"/>.
    /// Items with unknown kinds are skipped (logged to stderr) rather than throwing,
    /// so a partially-compatible file still imports cleanly.
    /// Returns an empty list when the file doesn't exist.
    /// </summary>
    public static async Task<IReadOnlyList<BaseTrackableGame>> ImportAsync(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path must not be blank.", nameof(path));

        if (!File.Exists(path))
            return [];

        string json;
        try
        {
            json = await File.ReadAllTextAsync(path);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Could not read file: {ex.Message}", ex);
        }

        ExportFile? file;
        bool hasItemsKey;
        try
        {
            using var doc = JsonDocument.Parse(json);
            hasItemsKey = doc.RootElement.ValueKind == JsonValueKind.Object
                       && doc.RootElement.TryGetProperty("items", out _);
            file = JsonSerializer.Deserialize<ExportFile>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"File is not valid JSON: {ex.Message}", ex);
        }

        if (file is null || !hasItemsKey)
            throw new InvalidDataException("File does not contain a recognised SharpQuest export.");

        var results = new List<BaseTrackableGame>();

        foreach (var envelope in file.Items)
        {
            if (!KindMap.TryGetValue(envelope.Kind, out var targetType))
            {
                Console.Error.WriteLine($"[LibraryExporter] Skipping unknown kind '{envelope.Kind}'.");
                continue;
            }

            BaseTrackableGame? game;
            try
            {
                game = (BaseTrackableGame?)envelope.Data.Deserialize(targetType, Options);
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine($"[LibraryExporter] Skipping '{envelope.Kind}': {ex.Message}");
                continue;
            }

            if (game is not null)
                results.Add(game);
        }

        return results;
    }
}

