using System.Collections.Concurrent;
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
        var invalidCharacterMessage = GetInvalidCharacterMessage(nickname);
        if (invalidCharacterMessage is not null)
        {
            yield return CheckStreamEvent.ErrorEvent(invalidCharacterMessage);
            yield break;
        }

        var validation = validator.Validate(nickname);
        if (!validation.IsValid)
        {
            yield return CheckStreamEvent.ErrorEvent(validation.ErrorMessage!);
            yield break;
        }

        var normalized = validation.NormalizedNickname;
        var results = new ConcurrentBag<PlatformCheckResult>();
        var tasks = platformRegistry.GetAll()
            .Select(checker => CheckWithCacheAsync(checker, normalized, cancellationToken))
            .ToList();

        while (tasks.Count > 0)
        {
            var completed = await Task.WhenAny(tasks);
            tasks.Remove(completed);

            var result = await completed;
            results.Add(result);

            logger.LogInformation(
                "NameScan platform result {Platform} {Status} {Confidence}",
                result.Platform,
                result.Status,
                result.Confidence);

            yield return CheckStreamEvent.ResultEvent(result);
        }

        var orderedResults = results.OrderBy(result => PlatformOrder(result.Platform)).ToArray();
        var suggestions = suggestionService.Generate(normalized);
        yield return CheckStreamEvent.DoneEvent(normalized, suggestions, CheckSummary.From(orderedResults));
    }

    private async Task<PlatformCheckResult> CheckWithCacheAsync(
        IPlatformChecker checker,
        string nickname,
        CancellationToken requestCancellationToken)
    {
        var cacheKey = $"check:{checker.Id}:{nickname}";
        if (cache.TryGetValue(cacheKey, out PlatformCheckResult? cached) && cached is not null)
        {
            return cached;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestCancellationToken);
        timeout.CancelAfter(PlatformTimeout);

        try
        {
            var result = await checker.CheckAsync(nickname, timeout.Token);
            cache.Set(cacheKey, result, CacheTtl);
            return result;
        }
        catch (OperationCanceledException) when (!requestCancellationToken.IsCancellationRequested)
        {
            return new PlatformCheckResult(checker.Name, string.Empty, CheckStatus.Inconclusive, ConfidenceLevel.Low, "Tempo limite atingido.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Platform checker failed for {Platform}", checker.Name);
            return new PlatformCheckResult(checker.Name, string.Empty, CheckStatus.Error, ConfidenceLevel.Low, "Falha técnica temporária.");
        }
    }

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

    private static string? GetInvalidCharacterMessage(string? nickname)
    {
        var normalized = (nickname ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-'))
            ? "Use apenas letras sem acento, números, ponto, underline ou hífen."
            : null;
    }
}
