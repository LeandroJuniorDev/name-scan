using System.Text.RegularExpressions;
using NameScan.Features.Checks;

namespace NameScan.Platforms;

public sealed partial class GitHubChecker(HttpClient httpClient)
    : BaseHttpPlatformChecker(httpClient)
{
    public override string Id => "github";
    public override string Name => "GitHub";

    public override async Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken)
    {
        var uri = new Uri($"https://github.com/{nickname}");

        if (!GitHubName().IsMatch(nickname) || nickname.StartsWith('-') || nickname.EndsWith('-'))
        {
            return Invalid(Name, uri.ToString(), "GitHub permite letras, números e hífen, sem hífen no início ou fim.");
        }

        var checker = new SocialPlatformChecker(Id, Name, value => new Uri($"https://github.com/{value}"), HttpClient);
        return await checker.CheckAsync(nickname, cancellationToken);
    }

    [GeneratedRegex("^[a-z0-9-]+$")]
    private static partial Regex GitHubName();
}
