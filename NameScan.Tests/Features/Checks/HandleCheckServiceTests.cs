using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NameScan.Features.Checks;
using NameScan.Features.Suggestions;
using NameScan.Platforms;
using NameScan.Validation;

namespace NameScan.Tests.Features.Checks;

public sealed class HandleCheckServiceTests
{
    [Fact]
    public async Task StreamAsync_ReturnsInvalidEventForInvalidNickname()
    {
        var service = CreateService([new FakeChecker("GitHub", CheckStatus.Available)]);

        var events = await CollectAsync(service.StreamAsync("!", CancellationToken.None));

        var error = Assert.Single(events);
        Assert.Equal(CheckStreamEventKind.Error, error.Kind);
        Assert.Equal("Use apenas letras sem acento, números, ponto, underline ou hífen.", error.Message);
    }

    [Fact]
    public async Task StreamAsync_EmitsResultForEachCheckerAndDoneEvent()
    {
        var service = CreateService(
        [
            new FakeChecker("GitHub", CheckStatus.Available),
            new FakeChecker("Instagram", CheckStatus.Occupied)
        ]);

        var events = await CollectAsync(service.StreamAsync("minhamarca", CancellationToken.None));

        Assert.Equal(3, events.Count);
        Assert.Equal(CheckStreamEventKind.Result, events[0].Kind);
        Assert.Equal(CheckStreamEventKind.Result, events[1].Kind);
        Assert.Equal(CheckStreamEventKind.Done, events[2].Kind);
        Assert.Equal(1, events[2].Summary!.Available);
        Assert.Equal(1, events[2].Summary!.Occupied);
        Assert.NotNull(events[2].Suggestions);
        Assert.Contains("minhamarcaapp", events[2].Suggestions!);
    }

    [Fact]
    public async Task StreamAsync_IsolatesCheckerFailure()
    {
        var service = CreateService(
        [
            new ThrowingChecker("TikTok"),
            new FakeChecker("GitHub", CheckStatus.Available)
        ]);

        var events = await CollectAsync(service.StreamAsync("minhamarca", CancellationToken.None));

        var results = events.Where(item => item.Kind == CheckStreamEventKind.Result).Select(item => item.Result!).ToArray();

        Assert.Contains(results, result => result.Platform == "TikTok" && result.Status == CheckStatus.Error);
        Assert.Contains(results, result => result.Platform == "GitHub" && result.Status == CheckStatus.Available);
    }

    [Fact]
    public async Task StreamAsync_ReusesCachedPlatformResults()
    {
        var checker = new CountingChecker("GitHub", CheckStatus.Available);
        var service = CreateService([checker]);

        _ = await CollectAsync(service.StreamAsync("minhamarca", CancellationToken.None));
        _ = await CollectAsync(service.StreamAsync("minhamarca", CancellationToken.None));

        Assert.Equal(1, checker.CallCount);
    }

    [Fact]
    public async Task StreamAsync_ReturnsInconclusiveWhenCheckerTimesOut()
    {
        var service = CreateService([new TimeoutChecker("TikTok")]);

        var events = await CollectAsync(service.StreamAsync("minhamarca", CancellationToken.None));

        var result = Assert.Single(events.Where(item => item.Kind == CheckStreamEventKind.Result).Select(item => item.Result));
        Assert.NotNull(result);
        Assert.Equal("TikTok", result!.Platform);
        Assert.Equal(CheckStatus.Inconclusive, result.Status);
        Assert.Equal(ConfidenceLevel.Low, result.Confidence);
        Assert.Equal("Tempo limite atingido.", result.Note);
    }

    private static HandleCheckService CreateService(IReadOnlyList<IPlatformChecker> checkers) =>
        new(
            new NicknameValidator(),
            new FakeRegistry(checkers),
            new SuggestionService(),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<HandleCheckService>.Instance);

    private static async Task<List<CheckStreamEvent>> CollectAsync(IAsyncEnumerable<CheckStreamEvent> events)
    {
        var collected = new List<CheckStreamEvent>();
        await foreach (var streamEvent in events)
        {
            collected.Add(streamEvent);
        }

        return collected;
    }

    private sealed class FakeRegistry(IReadOnlyList<IPlatformChecker> checkers) : IPlatformRegistry
    {
        public IReadOnlyList<IPlatformChecker> GetAll() => checkers;
    }

    private sealed class FakeChecker(string name, CheckStatus status) : IPlatformChecker
    {
        public string Id => name.ToLowerInvariant();
        public string Name => name;

        public Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken) =>
            Task.FromResult(new PlatformCheckResult(Name, $"https://example.com/{nickname}", status, ConfidenceLevel.High, "Teste."));
    }

    private sealed class ThrowingChecker(string name) : IPlatformChecker
    {
        public string Id => name.ToLowerInvariant();
        public string Name => name;

        public Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Falha simulada.");
    }

    private sealed class CountingChecker(string name, CheckStatus status) : IPlatformChecker
    {
        public int CallCount { get; private set; }

        public string Id => name.ToLowerInvariant();
        public string Name => name;

        public Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new PlatformCheckResult(Name, $"https://example.com/{nickname}", status, ConfidenceLevel.High, "Teste."));
        }
    }

    private sealed class TimeoutChecker(string name) : IPlatformChecker
    {
        public string Id => name.ToLowerInvariant();
        public string Name => name;

        public async Task<PlatformCheckResult> CheckAsync(string nickname, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new PlatformCheckResult(Name, $"https://example.com/{nickname}", CheckStatus.Available, ConfidenceLevel.High, "Teste.");
        }
    }
}
