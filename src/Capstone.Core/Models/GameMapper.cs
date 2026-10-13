using System.Text.Json;
using Capstone.Core.Models.Enums;
using SharpQuest.Contracts;

namespace Capstone.Core.Models;

/// <summary>
/// Maps Game/GameDto objects to and from JSON.
/// </summary>
public class GameMapper : IItemMapper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public string ToJson(IItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var dto = item switch
        {
            Game g => new GameDto(
                g.Id,
                g.Title,
                g.Platform.ToString(),
                g.Genre ?? string.Empty,
                g.ReleaseYear,
                g.HoursPlayed,
                g.Price,
                g.Status.ToString(),
                g.Rating),
            GameDto d => d,
            _ => throw new ArgumentException($"Cannot serialize {item.GetType().Name}")
        };

        return JsonSerializer.Serialize(dto, Options);
    }

    public IItem? FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

            var dto = JsonSerializer.Deserialize<GameDto>(json, Options);
            if (dto is null || string.IsNullOrWhiteSpace(dto.Id)) return null;
            return dto;
        }
        catch
        {
            return null;
        }
    }
}
