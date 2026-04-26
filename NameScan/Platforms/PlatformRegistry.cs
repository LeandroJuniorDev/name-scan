namespace NameScan.Platforms;

public sealed class PlatformRegistry(HttpClient httpClient) : IPlatformRegistry
{
    public IReadOnlyList<IPlatformChecker> GetAll() =>
    [
        new SocialPlatformChecker("instagram", "Instagram", nickname => new Uri($"https://www.instagram.com/{nickname}/"), httpClient),
        new SocialPlatformChecker("tiktok", "TikTok", nickname => new Uri($"https://www.tiktok.com/@{nickname}"), httpClient),
        new SocialPlatformChecker("x", "X", nickname => new Uri($"https://x.com/{nickname}"), httpClient),
        new SocialPlatformChecker("youtube", "YouTube", nickname => new Uri($"https://www.youtube.com/@{nickname}"), httpClient),
        new GitHubChecker(httpClient),
        new SocialPlatformChecker("twitch", "Twitch", nickname => new Uri($"https://www.twitch.tv/{nickname}"), httpClient),
        new DotComDomainChecker(),
        new DotComBrDomainChecker()
    ];
}
