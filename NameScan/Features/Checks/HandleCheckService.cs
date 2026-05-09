using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NameScan.Features.Suggestions;
using NameScan.Platforms;
using NameScan.Validation;

namespace NameScan.Features.Checks;

public sealed class HandleCheckService(
    NicknameValidator validator,
    IPlatformRegistry platformRegistry,
    SuggestionService suggestionService,
    IMemoryCache cache,
    ILogger<HandleCheckService> logger)
{
    private static readonly TimeSpan PlatformTimeout = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(2);

    public async IAsyncEnumerable<CheckStreamEvent> StreamAsync(
        string? nickname,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var validation = validator.Validate(nickname);
        var requestedNickname = validation.NormalizedNickname ?? nickname ?? string.Empty;
        var checkers = validation.IsValid ? platformRegistry.GetAll() : [];
        var checkStopwatch = Stopwatch.StartNew();
        using var checkActivity = CheckTelemetry.StartCheckActivity(requestedNickname, checkers.Count);

        if (!validation.IsValid)
        {
            checkActivity?.SetTag("namescan.outcome", "invalid");
            CheckTelemetry.RecordCheckCompleted("invalid", checkStopwatch.Elapsed);
            yield return CheckStreamEvent.ErrorEvent(validation.ErrorMessage!);
            yield break;
        }

        var normalized = validation.NormalizedNickname!;
        var results = new ConcurrentBag<PlatformCheckResult>();
        var tasks = checkers
            .Select(checker => CheckWithCacheAsync(checker, normalized, cancellationToken))
            .ToList();

        while (tasks.Count > 0)
        {
            var completed = await Task.WhenAny(tasks);
            tasks.Remove(completed);

            var observation = await completed;
            var result = observation.Result;
            results.Add(result);

            logger.LogInformation(
                "NameScan platform result {Platform} {Status} {Confidence}",
                result.Platform,
                result.Status,
                result.Confidence);
            checkActivity?.AddEvent(new ActivityEvent(
                "namescan.result",
                tags: new ActivityTagsCollection
                {
                    ["namescan.platform"] = result.Platform,
                    ["namescan.status"] = result.Status.ToString(),
                    ["namescan.confidence"] = result.Confidence.ToString()
                }));
            CheckTelemetry.RecordPlatformResult(result, observation.Duration, observation.CacheHit);

            yield return CheckStreamEvent.ResultEvent(result);
        }

        var orderedResults = results.OrderBy(result => PlatformOrder(result.Platform)).ToArray();
        var suggestions = suggestionService.Generate(normalized);
        var summary = CheckSummary.From(orderedResults);
        checkActivity?.SetTag("namescan.outcome", "completed");
        checkActivity?.SetTag("namescan.available", summary.Available);
        checkActivity?.SetTag("namescan.occupied", summary.Occupied);
        checkActivity?.SetTag("namescan.invalid", summary.Invalid);
        checkActivity?.SetTag("namescan.inconclusive", summary.Inconclusive);
        checkActivity?.SetTag("namescan.error", summary.Error);
        CheckTelemetry.RecordCheckCompleted("completed", checkStopwatch.Elapsed, summary);
        yield return CheckStreamEvent.DoneEvent(normalized, suggestions, summary);
    }

    private async Task<ObservedPlatformCheckResult> CheckWithCacheAsync(
        IPlatformChecker checker,
        string nickname,
        CancellationToken requestCancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var cacheKey = $"check:{checker.Id}:{nickname}";
        if (cache.TryGetValue(cacheKey, out PlatformCheckResult? cached) && cached is not null)
        {
            using var cachedActivity = CheckTelemetry.StartPlatformCheckActivity(checker.Name, nickname, cacheHit: true);
            cachedActivity?.SetTag("namescan.status", cached.Status.ToString());
            return new ObservedPlatformCheckResult(cached, stopwatch.Elapsed, CacheHit: true);
        }

        using var activity = CheckTelemetry.StartPlatformCheckActivity(checker.Name, nickname, cacheHit: false);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestCancellationToken);
        timeout.CancelAfter(PlatformTimeout);

        try
        {
            var result = await checker.CheckAsync(nickname, timeout.Token).WaitAsync(timeout.Token);
            if (ShouldCache(result))
            {
                cache.Set(cacheKey, result, CacheTtl);
            }

            activity?.SetTag("namescan.status", result.Status.ToString());
            activity?.SetTag("namescan.confidence", result.Confidence.ToString());
            return new ObservedPlatformCheckResult(result, stopwatch.Elapsed, CacheHit: false);
        }
        catch (OperationCanceledException) when (requestCancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException) when (!requestCancellationToken.IsCancellationRequested)
        {
            var result = new PlatformCheckResult(checker.Name, string.Empty, CheckStatus.Inconclusive, ConfidenceLevel.Low, "Tempo limite atingido.");
            activity?.SetTag("namescan.status", result.Status.ToString());
            activity?.SetTag("namescan.confidence", result.Confidence.ToString());
            return new ObservedPlatformCheckResult(result, stopwatch.Elapsed, CacheHit: false);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Platform checker failed for {Platform}", checker.Name);
            var result = new PlatformCheckResult(checker.Name, string.Empty, CheckStatus.Error, ConfidenceLevel.Low, "Falha técnica temporária.");
            activity?.SetTag("namescan.status", result.Status.ToString());
            activity?.SetTag("namescan.confidence", result.Confidence.ToString());
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            return new ObservedPlatformCheckResult(result, stopwatch.Elapsed, CacheHit: false);
        }
    }

    private static bool ShouldCache(PlatformCheckResult result) =>
        result.Status is CheckStatus.Available or CheckStatus.Occupied or CheckStatus.Invalid;

    private static int PlatformOrder(string platform) =>
        platform switch
        {
            "Instagram" => 0,
            "TikTok" => 1,
            "X" => 2,
            "YouTube" => 3,
            "GitHub" => 4,
            "Twitch" => 5,
            ".com" => 6,
            ".com.br" => 7,
            _ => 99
        };

    private sealed record ObservedPlatformCheckResult(
        PlatformCheckResult Result,
        TimeSpan Duration,
        bool CacheHit);
}
