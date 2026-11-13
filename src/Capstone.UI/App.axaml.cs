using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Capstone.Core.Models;
using Capstone.Core.ViewModels;
using Capstone.UI.Views;

namespace Capstone.UI;

public class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var store = new GameStore();
            var vm    = new CatalogueViewModel(store);

            // Seed a few games to start with.
            SeedStore(store);

            desktop.MainWindow = new MainWindow { DataContext = vm };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void SeedStore(GameStore store)
    {
        store.Add(new Capstone.Core.Models.PhysicalGame
        {
            Id = "gm-001", Title = "Hollow Knight",
            Platform = Core.Models.Enums.Platform.PC, HoursPlayed = 45, Price = 14.99m,
            Status = Core.Models.Enums.GameStatus.Completed, ReleaseYear = 2017,
            Genre = "Metroidvania",
            Developer = new Core.Models.Developer { Name = "Team Cherry", Country = "Australia" }
        });
        store.Add(new Capstone.Core.Models.DigitalGame
        {
            Id = "gm-002", Title = "Dark Souls III",
            Platform = Core.Models.Enums.Platform.PlayStation4, HoursPlayed = 110, Price = 59.99m,
            Status = Core.Models.Enums.GameStatus.Completed, ReleaseYear = 2016,
            Genre = "Action RPG",
            Developer = new Core.Models.Developer { Name = "FromSoftware", Country = "Japan" }
        });
        store.Add(new Capstone.Core.Models.DigitalGame
        {
            Id = "gm-003", Title = "Balatro",
            Platform = Core.Models.Enums.Platform.NintendoSwitch, HoursPlayed = 75, Price = 14.99m,
            Status = Core.Models.Enums.GameStatus.Playing, ReleaseYear = 2024,
            Genre = "Roguelike Deckbuilder",
            Developer = new Core.Models.Developer { Name = "LocalThunk", Country = "Canada" }
        });
    }
}

