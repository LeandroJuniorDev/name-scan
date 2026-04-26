namespace NameScan.Features.Checks;

public enum CheckStreamEventKind
{
    Result,
    Done,
    Error
}

public sealed record CheckStreamEvent(
    CheckStreamEventKind Kind,
    PlatformCheckResult? Result = null,
    string? Query = null,
    IReadOnlyList<string>? Suggestions = null,
    CheckSummary? Summary = null,
    string? Message = null)
{
    public static CheckStreamEvent ResultEvent(PlatformCheckResult result) =>
        new(CheckStreamEventKind.Result, Result: result);

    public static CheckStreamEvent DoneEvent(string query, IReadOnlyList<string> suggestions, CheckSummary summary) =>
        new(CheckStreamEventKind.Done, Query: query, Suggestions: suggestions, Summary: summary);

    public static CheckStreamEvent ErrorEvent(string message) =>
        new(CheckStreamEventKind.Error, Message: message);
}
