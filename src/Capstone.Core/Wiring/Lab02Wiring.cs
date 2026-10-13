using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
using SharpQuest.Contracts;

namespace Capstone.Core.Wiring;

/// <summary>
/// Wiring for Lab 02 — creates store, mapper, and sample game items
/// so the contract tests can exercise our domain logic.
/// </summary>
public class Lab02Wiring : ILab02Wiring
{
    // A table of 100 game titles to make deterministic samples from seed 1-100.
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
        "Terraria",
        "No Man's Sky",
        "Subnautica",
        "Outer Wilds",
        "Return of the Obra Dinn",
        "Undertale",
        "Deltarune Chapter 2",
        "Spiritfarer",
        "Ori and the Will of the Wisps",
        "Cuphead",
        "Shovel Knight",
        "A Short Hike",
        "What Remains of Edith Finch",
        "Firewatch",
        "Inside",
        "Limbo",
        "Little Nightmares",
        "Oxenfree",
        "Night in the Woods",
        "Gris",
        "Journey",
        "Flower",
        "Abzu",
        "Shadow of the Colossus",
        "Ico",
        "The Last Guardian",
        "NieR: Automata",
        "NieR Replicant",
        "Drakengard 3",
        "Control",
        "Alan Wake 2",
        "Quantum Break",
        "Max Payne 3",
        "Remedy Connected Universe",
        "Bioshock Infinite",
        "Bioshock",
        "System Shock 2",
        "Deus Ex",
        "Thief",
        "Dishonored 2",
        "Prey (2017)",
        "Arkane Anthology",
        "Vampire: The Masquerade",
        "Disco Punk",
        "Planescape Torment",
        "Fallout: New Vegas",
        "The Elder Scrolls V: Skyrim",
        "Oblivion Remastered",
        "Kingdom Come: Deliverance",
        "Gothic",
        "Gothic 2",
        "Pathfinder: Wrath of the Righteous",
        "Pillars of Eternity II",
        "Tyranny",
        "Wasteland 3",
        "XCOM 2",
        "Into the Breach",
        "FTL: Faster Than Light",
        "Slay the Spire",
        "Monster Train",
        "Dicey Dungeons",
        "Inscryption",
        "Buckshot Roulette",
        "Balatro",
        "Vampire Survivors",
        "20 Minutes Till Dawn",
        "Enter the Gungeon",
        "Dead Cells",
        "Rogue Legacy 2",
        "Hades II",
        "Returnal",
        "Sifu",
        "Ghostrunner",
        "Katana ZERO",
        "Hotline Miami",
        "My Friend Pedro",
        "Superhot",
        "Superhot Mind Control Delete",
        "Thumper",
        "Sayonara Wild Hearts",
        "Pistol Whip"
    ];

    private static readonly Platform[] Platforms =
    [
        Platform.PC, Platform.PlayStation5, Platform.Xbox, Platform.NintendoSwitch, Platform.PlayStation4
    ];

    public IItemStore CreateStore() => new GameStore();

    public IItemMapper CreateMapper() => new GameMapper();

    public IItem CreateSample(int seed)
    {
        // Seed is 1-100, titles array has 100 entries.
        var index = ((seed - 1) % Titles.Length + Titles.Length) % Titles.Length;
        var title = Titles[index];
        var platform = Platforms[seed % Platforms.Length];
        var id = $"gm-{seed:D3}";

        return new Game
        {
            Id = id,
            Title = title,
            Platform = platform,
            Status = Models.Enums.GameStatus.Backlog,
            HoursPlayed = seed % 50,
            Price = 29.99m + (seed % 40),
            ReleaseYear = 2015 + (seed % 10),
            Genre = "Action",
            Developer = new Models.Developer { Name = "Sample Developer" }
        };
    }
}