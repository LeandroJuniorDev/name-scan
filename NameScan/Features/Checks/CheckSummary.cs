namespace NameScan.Features.Checks;

public sealed record CheckSummary(
    int Available,
    int Occupied,
    int Invalid,
    int Inconclusive,
    int Error)
{
    public static CheckSummary From(IEnumerable<PlatformCheckResult> results)
    {
        var items = results.ToArray();
        return new CheckSummary(
            items.Count(item => item.Status == CheckStatus.Available),
            items.Count(item => item.Status == CheckStatus.Occupied),
            items.Count(item => item.Status == CheckStatus.Invalid),
            items.Count(item => item.Status == CheckStatus.Inconclusive),
            items.Count(item => item.Status == CheckStatus.Error));
    }
}
