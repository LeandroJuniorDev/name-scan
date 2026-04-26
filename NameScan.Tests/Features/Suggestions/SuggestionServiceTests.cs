using NameScan.Features.Suggestions;

namespace NameScan.Tests.Features.Suggestions;

public sealed class SuggestionServiceTests
{
    [Fact]
    public void Generate_ReturnsDeterministicBrazilFirstSuggestions()
    {
        var service = new SuggestionService();

        var suggestions = service.Generate("minhamarca");

        Assert.Equal(
        [
            "minhamarcaapp",
            "useminhamarca",
            "minhamarcaoficial",
            "minhamarca_io",
            "getminhamarca",
            "minhamarcaHQ",
            "minhamarcaBrasil"
        ], suggestions);
    }

    [Fact]
    public void Generate_DoesNotReturnSuggestionsLongerThanThirtyCharacters()
    {
        var service = new SuggestionService();

        var suggestions = service.Generate("nomemuitolongoquasecomtrinta");

        Assert.All(suggestions, suggestion => Assert.InRange(suggestion.Length, 2, 30));
    }
}
