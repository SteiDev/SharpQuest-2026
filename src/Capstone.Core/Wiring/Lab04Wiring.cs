using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
using Capstone.Core.Models.Interfaces;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

public class Lab04Wiring : ILab04Wiring
{
    private static readonly string[] Titles =
    [
        "The Witcher 3: Wild Hunt",
        "Dark Souls III",
        "Hollow Knight",
        "Celeste",
        "Hades",
        "Red Dead Redemption 2",
        "God of War",
        "Sekiro: Shadows Die Twice",
        "Elden Ring",
        "Disco Elysium",
        "Death Stranding",
        "Cyberpunk 2077",
        "Metal Gear Solid V",
        "Bloodborne",
        "Persona 5 Royal",
        "Final Fantasy XVI",
        "Divinity: Original Sin 2",
        "Baldur's Gate 3",
        "Stardew Valley",
        "Minecraft",
    ];

    private static readonly Platform[] Platforms =
        [Platform.PC, Platform.PlayStation5, Platform.Xbox, Platform.NintendoSwitch, Platform.PlayStation4];

    public IItemStore CreateStore() => new GameStore();

    public Type BaseType => typeof(BaseTrackableGame);

    public IReadOnlyList<Type> KindTypes => [typeof(PhysicalGame), typeof(DigitalGame)];

    public IItem CreateOfKind(int kindIndex, int seed)
    {
        var index = ((seed - 1) % Titles.Length + Titles.Length) % Titles.Length;
        var title = Titles[index];
        var platform = Platforms[seed % Platforms.Length];
        var developer = new Developer { Name = "Sample Developer" };

        return kindIndex switch
        {
            0 => new PhysicalGame
            {
                Id = $"phys-{seed:D3}",
                Title = title,
                Platform = platform,
                Status = GameStatus.Backlog,
                HoursPlayed = seed % 50,
                Price = 29.99m + (seed % 40),
                ReleaseYear = 2015 + (seed % 10),
                Genre = "Action",
                Developer = developer
            },
            1 => new DigitalGame
            {
                Id = $"digi-{seed:D3}",
                Title = title,
                Platform = platform,
                Status = GameStatus.Backlog,
                HoursPlayed = seed % 50,
                Price = 19.99m + (seed % 30),
                ReleaseYear = 2015 + (seed % 10),
                Genre = "Action",
                Developer = developer
            },
            _ => throw new ArgumentOutOfRangeException(nameof(kindIndex))
        };
    }

    public string Describe(IItem item) => item switch
    {
        BaseTrackableGame g => g.Describe(),
        _ => throw new ArgumentException($"Unknown item type: {item.GetType().Name}")
    };

    public IReadOnlyList<string> DescribeAll(IItemStore store)
    {
        var result = new List<string>();
        foreach (var item in store.All())
        {
            if (item is BaseTrackableGame g)
                result.Add(g.Describe());
        }
        return result.AsReadOnly();
    }

    public string Classify(IItem? item) => item switch
    {
        null => throw new ArgumentNullException(nameof(item)),
        PhysicalGame => "Physical",
        DigitalGame => "Digital",
        _ => "Unknown"
    };

    public bool TryFindByName(IItemStore store, string? name, out IItem? found)
    {
        found = null;
        if (string.IsNullOrWhiteSpace(name)) return false;
        found = store.All().FirstOrDefault(i => i.DisplayName == name);
        return found is not null;
    }

    public int AddAll(IItemStore store, params IItem[] items)
    {
        var count = 0;
        foreach (var item in items)
        {
            store.Add(item);
            count++;
        }
        return count;
    }
}
