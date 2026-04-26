namespace NameScan.Features.Suggestions;

public sealed class SuggestionService
{
    public IReadOnlyList<string> Generate(string nickname)
    {
        var candidates = new[]
        {
            $"{nickname}app",
            $"use{nickname}",
            $"{nickname}oficial",
            $"{nickname}_io",
            $"get{nickname}",
            $"{nickname}HQ",
            $"{nickname}Brasil"
        };

        return candidates
            .Where(candidate => candidate.Length is >= 2 and <= 30)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
