using NameScan.Features.Checks;

namespace NameScan.Platforms;

public abstract class BaseHttpPlatformChecker(HttpClient httpClient) : IPlatformChecker
{
    protected HttpClient HttpClient { get; } = httpClient;

    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken);

    protected static PlatformCheckResult Invalid(string platform, string url, string note) =>
        new(platform, url, CheckStatus.Invalid, ConfidenceLevel.High, note);
}
