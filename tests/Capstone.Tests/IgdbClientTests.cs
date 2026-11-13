using Capstone.Core.Web;

namespace Capstone.Tests;

public class IgdbClientTests
{
    // ── Client behaviour when credentials are absent ──────────────────────────

    [Fact]
    public async Task SearchAsync_returns_non_null_collection_regardless_of_credentials()
    {
        var client = new IgdbClient();
        var results = await client.SearchAsync("Elden Ring", TestContext.Current.CancellationToken);
        // Must not throw and must return a list (empty when no credentials)
        Assert.NotNull(results);
    }

    [Fact]
    public async Task SearchAsync_returns_empty_when_no_credentials_set()
    {
        // Skip if real creds happen to be present in this environment
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("IGDB_CLIENT_ID")))
            return;

        var client = new IgdbClient();
        var results = await client.SearchAsync("Hades", TestContext.Current.CancellationToken);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_no_credentials_set()
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("IGDB_CLIENT_ID")))
            return;

        var client = new IgdbClient();
        var result = await client.GetByIdAsync(1942, TestContext.Current.CancellationToken); // 1942 = The Witcher 3 on IGDB
        Assert.Null(result);
    }

    // ── DTO behaviour (no network required) ──────────────────────────────────

    [Fact]
    public void IgdbGame_computes_release_year_from_unix_timestamp()
    {
        // 1431561600 = 2015-05-14 UTC (The Witcher 3 release date on IGDB)
        var game = new IgdbGame(
            Id: 1942,
            Name: "The Witcher 3: Wild Hunt",
            FirstReleaseDate: 1431561600L,
            Genres: [new IgdbGenre(12, "Role-playing (RPG)")],
            Companies: [new IgdbInvolvedCompany(1, new IgdbCompany(23, "CD Projekt RED"))],
            Summary: "Open-world RPG"
        );

        Assert.Equal(2015, game.ReleaseYear);
    }

    [Fact]
    public void IgdbGame_returns_primary_genre()
    {
        var game = new IgdbGame(
            Id: 1,
            Name: "Hades",
            FirstReleaseDate: null,
            Genres: [new IgdbGenre(31, "Adventure"), new IgdbGenre(32, "Indie")],
            Companies: null,
            Summary: null
        );

        Assert.Equal("Adventure", game.PrimaryGenre);
    }

    [Fact]
    public void IgdbGame_returns_primary_company_name()
    {
        var game = new IgdbGame(
            Id: 103298,
            Name: "Hades",
            FirstReleaseDate: null,
            Genres: null,
            Companies: [new IgdbInvolvedCompany(1, new IgdbCompany(500, "Supergiant Games"))],
            Summary: null
        );

        Assert.Equal("Supergiant Games", game.PrimaryCompany);
    }

    [Fact]
    public void IgdbGame_handles_null_genres_and_companies_gracefully()
    {
        var game = new IgdbGame(
            Id: 99,
            Name: "Unknown Game",
            FirstReleaseDate: null,
            Genres: null,
            Companies: null,
            Summary: null
        );

        Assert.Null(game.PrimaryGenre);
        Assert.Null(game.PrimaryCompany);
        Assert.Null(game.ReleaseYear);
    }

    [Theory]
    [InlineData(1431561600L, 2015)]   // The Witcher 3
    [InlineData(1546300800L, 2019)]   // 2019-01-01
    [InlineData(1704067200L, 2024)]   // 2024-01-01
    public void IgdbGame_computes_correct_year_for_known_timestamps(long unixSeconds, int expectedYear)
    {
        var game = new IgdbGame(99, "Test", unixSeconds, null, null, null);
        Assert.Equal(expectedYear, game.ReleaseYear);
    }
}
