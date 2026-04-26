using NameScan.Features.Checks;

namespace NameScan.Platforms;

public interface IPlatformChecker
{
    string Id { get; }
    string Name { get; }
    Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken);
}
