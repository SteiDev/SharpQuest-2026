using Capstone.Core.Models;
using Capstone.Core.Models.Enums;
using Capstone.Core.ViewModels;
using Xunit;

namespace Capstone.Tests;

public class CatalogueSortAndFilterTests
{
    private static (GameStore Store, CatalogueViewModel Vm) CreateVmWithGames()
    {
        var store = new GameStore();
        var g1 = new DigitalGame
        {
            Id = "g-1",
            Title = "Zelda: Tears of the Kingdom",
            Platform = Platform.NintendoSwitch,
            Status = GameStatus.Completed,
            Genre = "Action-Adventure",
            Developer = new Developer { Name = "Nintendo EPD" },
            Publisher = "Nintendo",
            HoursPlayed = 85,
            Price = 69.99m,
            ReleaseYear = 2023,
            Rating = 10,
            AverageCompleteHours = 60,
            Description = "Masterpiece sequel to Breath of the Wild."
        };
        var g2 = new DigitalGame
        {
            Id = "g-2",
            Title = "Astro Bot",
            Platform = Platform.PlayStation5,
            Status = GameStatus.Playing,
            Genre = "Platformer",
            Developer = new Developer { Name = "Team Asobi" },
            Publisher = "Sony Interactive Entertainment",
            HoursPlayed = 15,
            Price = 59.99m,
            ReleaseYear = 2024,
            Rating = 9,
            AverageCompleteHours = 12,
            Description = "Delightful platformer celebrating PlayStation history."
        };
        var g3 = new DigitalGame
        {
            Id = "g-3",
            Title = "Hollow Knight",
            Platform = Platform.PC,
            Status = GameStatus.Backlog,
            Genre = "Metroidvania",
            Developer = new Developer { Name = "Team Cherry" },
            Publisher = "Team Cherry",
            HoursPlayed = 0,
            Price = 14.99m,
            ReleaseYear = 2017,
            Rating = null,
            AverageCompleteHours = 30,
            Description = "Challenging 2D action in Hallownest."
        };
        var g4 = new DigitalGame
        {
            Id = "g-4",
            Title = "Trackmania",
            Platform = Platform.PC,
            Status = GameStatus.Playing,
            Genre = "Racing",
            Developer = new Developer { Name = "Nadeo" },
            Publisher = "Ubisoft",
            HoursPlayed = 5,
            Price = null, // Free
            ReleaseYear = 2020,
            Rating = 8,
            AverageCompleteHours = null,
            Description = "Time-attack arcade racer."
        };

        store.Add(g1);
        store.Add(g2);
        store.Add(g3);
        store.Add(g4);

        var vm = new CatalogueViewModel(store);
        return (store, vm);
    }

    [Fact]
    public void Sort_By_Title_Orders_Alphabetically()
    {
        var (_, vm) = CreateVmWithGames();

        vm.SetSort(GameSortField.Title, SortDirection.Ascending);
        var items = vm.Items.Select(i => i.DisplayName).ToList();
        Assert.Equal("Astro Bot", items[0]);
        Assert.Equal("Hollow Knight", items[1]);
        Assert.Equal("Trackmania", items[2]);
        Assert.Equal("Zelda: Tears of the Kingdom", items[3]);

        vm.ToggleSortDirection();
        items = vm.Items.Select(i => i.DisplayName).ToList();
        Assert.Equal("Zelda: Tears of the Kingdom", items[0]);
        Assert.Equal("Trackmania", items[1]);
        Assert.Equal("Hollow Knight", items[2]);
        Assert.Equal("Astro Bot", items[3]);
    }

    [Fact]
    public void Sort_By_HoursPlayed_Orders_By_Hours()
    {
        var (_, vm) = CreateVmWithGames();

        vm.SetSort(GameSortField.HoursPlayed, SortDirection.Descending);
        var items = vm.Items.Select(i => i.DisplayName).ToList();
        Assert.Equal("Zelda: Tears of the Kingdom", items[0]); // 85h
        Assert.Equal("Astro Bot", items[1]);                  // 15h
        Assert.Equal("Trackmania", items[2]);                 // 5h
        Assert.Equal("Hollow Knight", items[3]);              // 0h
    }

    [Fact]
    public void Sort_By_Rating_Puts_Unrated_At_Bottom()
    {
        var (_, vm) = CreateVmWithGames();

        vm.SetSort(GameSortField.Rating, SortDirection.Descending);
        var items = vm.Items.Select(i => i.DisplayName).ToList();
        Assert.Equal("Zelda: Tears of the Kingdom", items[0]); // 10
        Assert.Equal("Astro Bot", items[1]);                  // 9
        Assert.Equal("Trackmania", items[2]);                 // 8
        Assert.Equal("Hollow Knight", items[3]);              // unrated
    }

    [Fact]
    public void Filter_By_Genre_Narrows_List()
    {
        var (_, vm) = CreateVmWithGames();

        vm.SetGenreFilter("Platformer");
        Assert.Single(vm.Items);
        Assert.Equal("Astro Bot", vm.Items.First().DisplayName);
    }

    [Fact]
    public void Filter_By_Price_Free_And_Paid()
    {
        var (_, vm) = CreateVmWithGames();

        vm.SetPriceFilter("Free");
        Assert.Single(vm.Items);
        Assert.Equal("Trackmania", vm.Items.First().DisplayName);

        vm.SetPriceFilter("Under20");
        var names = vm.Items.Select(i => i.DisplayName).ToList();
        Assert.Contains("Hollow Knight", names);
        Assert.DoesNotContain("Zelda: Tears of the Kingdom", names);
    }

    [Fact]
    public void Filter_By_Playtime_Unplayed()
    {
        var (_, vm) = CreateVmWithGames();

        vm.SetPlaytimeFilter("Unplayed");
        Assert.Single(vm.Items);
        Assert.Equal("Hollow Knight", vm.Items.First().DisplayName);
    }

    [Fact]
    public void Deep_Search_Matches_Publisher_And_Description()
    {
        var (_, vm) = CreateVmWithGames();

        vm.SetSearchAllFields(true);

        vm.Filter = "Sony";
        Assert.Single(vm.Items);
        Assert.Equal("Astro Bot", vm.Items.First().DisplayName);

        vm.Filter = "Hallownest";
        Assert.Single(vm.Items);
        Assert.Equal("Hollow Knight", vm.Items.First().DisplayName);
    }

    [Fact]
    public void New_Fields_Are_Exposed_In_Selected_Details()
    {
        var (store, vm) = CreateVmWithGames();
        var zelda = store.FindById("g-1");

        vm.Selected = zelda;

        Assert.Equal("Nintendo", vm.SelectedPublisher);
        Assert.Equal("60 h", vm.SelectedCompletionEstimate);
        Assert.Equal("Masterpiece sequel to Breath of the Wild.", vm.SelectedDescription);
    }
}

