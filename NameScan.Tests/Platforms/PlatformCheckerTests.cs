using System.Net;
using NameScan.Features.Checks;
using NameScan.Platforms;

namespace NameScan.Tests.Platforms;

public sealed class PlatformCheckerTests
{
    [Fact]
    public async Task SocialChecker_ReturnsOccupiedForSuccess()
    {
        var checker = new SocialPlatformChecker(
            "instagram",
            "Instagram",
            nickname => new Uri($"https://instagram.com/{nickname}"),
            TestHttpClientFactory.Create(HttpStatusCode.OK));

        var result = await checker.CheckAsync("minhamarca", CancellationToken.None);

        Assert.Equal(CheckStatus.Occupied, result.Status);
        Assert.Equal(ConfidenceLevel.High, result.Confidence);
        Assert.Equal("https://instagram.com/minhamarca", result.Url);
    }

    [Fact]
    public async Task SocialChecker_ReturnsAvailableForNotFound()
    {
        var checker = new SocialPlatformChecker(
            "github",
            "GitHub",
            nickname => new Uri($"https://github.com/{nickname}"),
            TestHttpClientFactory.Create(HttpStatusCode.NotFound));

        var result = await checker.CheckAsync("minhamarca", CancellationToken.None);

        Assert.Equal(CheckStatus.Available, result.Status);
        Assert.Equal(ConfidenceLevel.High, result.Confidence);
    }

    [Fact]
    public async Task SocialChecker_ReturnsInconclusiveForAmbiguousRedirect()
    {
        var checker = new SocialPlatformChecker(
            "x",
            "X",
            nickname => new Uri($"https://x.com/{nickname}"),
            TestHttpClientFactory.Create(HttpStatusCode.Redirect));

        var result = await checker.CheckAsync("minhamarca", CancellationToken.None);

        Assert.Equal(CheckStatus.Inconclusive, result.Status);
        Assert.Equal(ConfidenceLevel.Low, result.Confidence);
    }

    [Fact]
    public async Task GitHubChecker_ReturnsInvalidForDot()
    {
        var checker = new GitHubChecker(TestHttpClientFactory.Create(HttpStatusCode.OK));

        var result = await checker.CheckAsync("minha.marca", CancellationToken.None);

        Assert.Equal(CheckStatus.Invalid, result.Status);
    }

    [Fact]
    public void PlatformRegistry_ContainsRequiredEightTargetsInOrder()
    {
        var registry = new PlatformRegistry(new HttpClient());

        Assert.Equal(
        [
            "Instagram",
            "TikTok",
            "X",
            "YouTube",
            "GitHub",
            "Twitch",
            ".com",
            ".com.br"
        ], registry.GetAll().Select(checker => checker.Name));
    }
}
