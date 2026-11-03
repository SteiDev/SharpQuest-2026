using SharpQuest.Contracts;

namespace Capstone.Tests;

public class SearchMatcherTests
{
    private static SearchMatcher Create() => SearchMatcher.Create();

    // R1: empty/whitespace query matches everything
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_query_matches_any_text(string query)
    {
        var matcher = Create();
        Assert.True(matcher.Matches("Hollow Knight", query));
        Assert.True(matcher.Matches("Dark Souls", query));
    }

    // R2: case-insensitive
    [Fact]
    public void Matching_ignores_case()
    {
        var matcher = Create();
        Assert.True(matcher.Matches("Hollow Knight", "HOLLOW"));
        Assert.True(matcher.Matches("hollow knight", "Hollow"));
    }

    // R3: diacritics ignored
    [Fact]
    public void Matching_ignores_diacritics()
    {
        var matcher = Create();
        Assert.True(matcher.Matches("Café Racer", "cafe"));
        Assert.True(matcher.Matches("Cafe Racer", "café"));
    }

    // R4: all words must match
    [Fact]
    public void All_words_must_match()
    {
        var matcher = Create();
        Assert.True(matcher.Matches("Hollow Knight", "hollow knight"));
        Assert.False(matcher.Matches("Hollow Knight", "hollow souls"));
    }

    // R5: word order doesn't matter
    [Fact]
    public void Word_order_does_not_matter()
    {
        var matcher = Create();
        Assert.True(matcher.Matches("Dark Souls III", "souls dark"));
    }

    // R6: substring match (word can occur inside longer word)
    [Fact]
    public void Matches_inside_longer_word()
    {
        var matcher = Create();
        Assert.True(matcher.Matches("Pikachu", "chu"));
        Assert.True(matcher.Matches("Hollow Knight", "hollow"));
    }

    // R7: null text never matches
    [Fact]
    public void Null_text_never_matches()
    {
        var matcher = Create();
        Assert.False(matcher.Matches(null, "anything"));
    }

    // R8: null query throws ArgumentNullException
    [Fact]
    public void Null_query_throws_ArgumentNullException()
    {
        var matcher = Create();
        Assert.Throws<ArgumentNullException>(() => matcher.Matches("some text", null!));
    }
}
