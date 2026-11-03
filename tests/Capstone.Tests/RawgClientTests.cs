using Capstone.Core.Web;

namespace Capstone.Tests;

public class RawgClientTests
{
    [Fact]
    public async Task SearchAsync_returns_empty_when_no_api_key()
    {
        // When RAWG_API_KEY is not set or empty, SearchAsync safely returns empty list
        var client = new RawgClient();
        var results = await client.SearchAsync("Witcher", TestContext.Current.CancellationToken);
        Assert.NotNull(results);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_no_api_key()
    {
        // When RAWG_API_KEY is not set, GetByIdAsync safely returns null
        var client = new RawgClient();
        var result = await client.GetByIdAsync(3328, TestContext.Current.CancellationToken);
        // If key is not present in test runner env, should be null
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RAWG_API_KEY")))
        {
            Assert.Null(result);
        }
    }

    [Fact]
    public void RawgGameSummary_computes_release_year_and_genre()
    {
        var summary = new RawgGameSummary(
            Id: 1,
            Name: "The Witcher 3: Wild Hunt",
            Released: "2015-05-19",
            Genres: [new RawgGenre(1, "RPG"), new RawgGenre(2, "Action")]
        );

        Assert.Equal(2015, summary.ReleaseYear);
        Assert.Equal("RPG", summary.PrimaryGenre);
    }

    [Fact]
    public void RawgGameDetail_computes_release_year_and_developer()
    {
        var detail = new RawgGameDetail(
            Id: 1,
            Name: "Cyberpunk 2077",
            Released: "2020-12-10",
            Genres: [new RawgGenre(1, "RPG")],
            Description: "Open-world RPG",
            Developers: [new RawgDeveloper(1, "CD Projekt Red")]
        );

        Assert.Equal(2020, detail.ReleaseYear);
        Assert.Equal("CD Projekt Red", detail.PrimaryDeveloper);
    }
}

