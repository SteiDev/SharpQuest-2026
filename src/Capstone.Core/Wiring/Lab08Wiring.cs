using System.Text.Json;
using System.Text.Json.Serialization;
using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

// ── Persistence DTOs ──────────────────────────────────────────────────────────

/// <summary>
/// The on-disk format: a list of envelopes each carrying a kind discriminator and the item's data.
/// </summary>
internal sealed class SaveFile
{
    [JsonPropertyName("items")]
    public List<ItemEnvelope> Items { get; set; } = new();
}

/// <summary>
/// One item in the file. <see cref="Kind"/> is the concrete type name so we know what to build.
/// <see cref="Data"/> is re-parsed on load using the right target type.
/// </summary>
internal sealed class ItemEnvelope
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}

// ── Wiring ────────────────────────────────────────────────────────────────────

public class Lab08Wiring : ILab08Wiring
{
    private readonly Lab02Wiring _lab02 = new();
    private readonly Lab04Wiring _lab04 = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented     = false,
        PropertyNameCaseInsensitive = true,
    };

    // Known concrete types by name — same set as Lab 04's KindTypes
    private static readonly IReadOnlyDictionary<string, Type> KindMap =
        new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            [nameof(PhysicalGame)] = typeof(PhysicalGame),
            [nameof(DigitalGame)]  = typeof(DigitalGame),
            [nameof(Game)]         = typeof(Game),
        };

    public IItemStore CreateStore() => new GameStore();

    public IItem CreateSample(int seed) => _lab02.CreateSample(seed);

    public IReadOnlyList<Type> KindTypes => _lab04.KindTypes;

    public IItem CreateOfKind(int kindIndex, int seed) => _lab04.CreateOfKind(kindIndex, seed);

    // ── SaveAsync ─────────────────────────────────────────────────────────────

    public async Task SaveAsync(IItemStore store, string path)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(path);
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path must not be blank.", nameof(path));

        // Create parent directory if it doesn't exist
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var file = new SaveFile();
        foreach (var item in store.All())
        {
            var kind = item.GetType().Name;
            var dataJson = JsonSerializer.SerializeToElement(item, item.GetType(), Options);
            file.Items.Add(new ItemEnvelope { Kind = kind, Data = dataJson });
        }

        var json = JsonSerializer.Serialize(file, Options);
        await File.WriteAllTextAsync(path, json);
    }

    // ── LoadAsync ─────────────────────────────────────────────────────────────

    public async Task<IItemStore> LoadAsync(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path must not be blank.", nameof(path));

        // Missing file = nothing saved yet = empty store (not an error)
        if (!File.Exists(path))
            return new GameStore();

        string json;
        try
        {
            json = await File.ReadAllTextAsync(path);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Could not read file: {ex.Message}", ex);
        }

        SaveFile? file;
        bool hasItemsKey;
        try
        {
            using var doc = JsonDocument.Parse(json);
            hasItemsKey = doc.RootElement.ValueKind == JsonValueKind.Object
                       && doc.RootElement.TryGetProperty("items", out _);
            file = JsonSerializer.Deserialize<SaveFile>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"File is not valid JSON: {ex.Message}", ex);
        }

        // Valid JSON but wrong shape (e.g. {"hello":"world"}) → missing "items" key
        if (file is null || !hasItemsKey)
            throw new InvalidDataException("File does not contain a recognised save format.");

        var store = new GameStore();
        foreach (var envelope in file.Items)
        {
            if (!KindMap.TryGetValue(envelope.Kind, out var targetType))
                throw new InvalidDataException($"Unknown item kind '{envelope.Kind}'.");

            IItem? item;
            try
            {
                item = (IItem?)envelope.Data.Deserialize(targetType, Options);
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Could not deserialise '{envelope.Kind}': {ex.Message}", ex);
            }

            if (item is null)
                throw new InvalidDataException($"Item of kind '{envelope.Kind}' deserialised as null.");

            store.Add(item);
        }

        return store;
    }
}
