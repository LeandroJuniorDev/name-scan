using NameScan.Features.Checks;
using NameScan.Platforms;

namespace NameScan.Tests;

public sealed class FakePlatformRegistry(IReadOnlyList<IPlatformChecker> checkers) : IPlatformRegistry
{
    public IReadOnlyList<IPlatformChecker> GetAll() => checkers;
}

public sealed class FakePlatformChecker(string name, CheckStatus status) : IPlatformChecker
{
    public string Id => name.ToLowerInvariant();
    public string Name => name;

    public Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken) =>
        Task.FromResult(new PlatformCheckResult(Name, $"https://example.com/{nickname}", status, ConfidenceLevel.High, "Teste."));
}
