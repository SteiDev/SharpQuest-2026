using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Capstone.Core.Models;
using Capstone.Core.Models.Storage;
using Capstone.Core.ViewModels;
using Capstone.UI.Views;

namespace Capstone.UI;

public class App : Application
{
    // Kept alive for the lifetime of the process so the connection stays open.
    private GameRepository? _repo;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // ── Database path: ~/.local/share/SharpQuest/sharpquest.db ──────────
            var dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SharpQuest");
            Directory.CreateDirectory(dataDir);
            var dbPath = Path.Combine(dataDir, "sharpquest.db");

            _repo = new GameRepository(dbPath);

            var store = new GameStore();

            // Load persisted games; seed only if the database is brand new.
            var persisted = _repo.LoadAll();
            if (persisted.Count == 0)
                SeedStore(store, _repo);
            else
                foreach (var g in persisted)
                    store.Add(g);

            var vm = new CatalogueViewModel(store, _repo);

            // Persist every new game added through the store's ItemAdded event.
            store.ItemAdded += item =>
            {
                if (item is Capstone.Core.Models.Interfaces.BaseTrackableGame g)
                    _repo.Insert(g);
            };

            desktop.MainWindow = new MainWindow { DataContext = vm };

            // Dispose the DB connection when the app exits cleanly.
            desktop.ShutdownRequested += (_, _) => _repo?.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Seed the store AND the database on first launch.</summary>
    private static void SeedStore(GameStore store, GameRepository repo)
    {
        void AddAndPersist(Capstone.Core.Models.Interfaces.BaseTrackableGame game)
        {
            store.Add(game);   // raises ItemAdded → repo.Insert is wired after this method returns,
            repo.Insert(game); // so we call Insert directly here for the seed.
        }

        AddAndPersist(new PhysicalGame
        {
            Id = "gm-001", Title = "Hollow Knight",
            Platform = Core.Models.Enums.Platform.PC, HoursPlayed = 45, Price = 14.99m,
            Status = Core.Models.Enums.GameStatus.Completed, ReleaseYear = 2017,
            Genre = "Metroidvania",
            Developer = new Developer { Name = "Team Cherry", Country = "Australia" }
        });
        AddAndPersist(new DigitalGame
        {
            Id = "gm-002", Title = "Dark Souls III",
            Platform = Core.Models.Enums.Platform.PlayStation4, HoursPlayed = 110, Price = 59.99m,
            Status = Core.Models.Enums.GameStatus.Completed, ReleaseYear = 2016,
            Genre = "Action RPG",
            Developer = new Developer { Name = "FromSoftware", Country = "Japan" }
        });
        AddAndPersist(new DigitalGame
        {
            Id = "gm-003", Title = "Balatro",
            Platform = Core.Models.Enums.Platform.NintendoSwitch, HoursPlayed = 75, Price = 14.99m,
            Status = Core.Models.Enums.GameStatus.Playing, ReleaseYear = 2024,
            Genre = "Roguelike Deckbuilder",
            Developer = new Developer { Name = "LocalThunk", Country = "Canada" }
        });
    }
}

