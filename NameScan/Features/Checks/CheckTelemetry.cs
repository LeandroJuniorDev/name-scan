using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace NameScan.Features.Checks;

public static class CheckTelemetry
{
    public static readonly string MeterName = typeof(CheckTelemetry).Assembly.GetName().Name ?? "NameScan";
    public static readonly string ActivitySourceName = typeof(CheckTelemetry).Assembly.GetName().Name ?? "NameScan";

    private static readonly Meter Meter = new(MeterName);
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Counter<long> ChecksStarted = Meter.CreateCounter<long>("namescan.checks.started");
    private static readonly Counter<long> ChecksCompleted = Meter.CreateCounter<long>("namescan.checks.completed");
    private static readonly Counter<long> PlatformResults = Meter.CreateCounter<long>("namescan.platform.results");
    private static readonly Histogram<double> CheckDuration = Meter.CreateHistogram<double>("namescan.check.duration", unit: "ms");
    private static readonly Histogram<double> PlatformCheckDuration = Meter.CreateHistogram<double>("namescan.platform_check.duration", unit: "ms");

    public static Activity? StartCheckActivity(string nickname, int checkerCount)
    {
        ChecksStarted.Add(1);

        var activity = ActivitySource.StartActivity("namescan.check", ActivityKind.Internal);
        activity?.SetTag("namescan.nickname", nickname);
        activity?.SetTag("namescan.checker_count", checkerCount);
        return activity;
    }

    public static Activity? StartPlatformCheckActivity(string platform, string nickname, bool cacheHit)
    {
        var activity = ActivitySource.StartActivity("namescan.platform_check", ActivityKind.Internal);
        activity?.SetTag("namescan.platform", platform);
        activity?.SetTag("namescan.nickname", nickname);
        activity?.SetTag("namescan.cache_hit", cacheHit);
        return activity;
    }

    public static void RecordCheckCompleted(string outcome, TimeSpan duration, CheckSummary? summary = null)
    {
        ChecksCompleted.Add(1, new KeyValuePair<string, object?>("outcome", outcome));
        CheckDuration.Record(duration.TotalMilliseconds, new KeyValuePair<string, object?>("outcome", outcome));

        if (summary is null)
        {
            return;
        }

        CheckDuration.Record(
            duration.TotalMilliseconds,
            new KeyValuePair<string, object?>("available", summary.Available),
            new KeyValuePair<string, object?>("occupied", summary.Occupied),
            new KeyValuePair<string, object?>("invalid", summary.Invalid),
            new KeyValuePair<string, object?>("inconclusive", summary.Inconclusive),
            new KeyValuePair<string, object?>("error", summary.Error));
    }

    public static void RecordPlatformResult(PlatformCheckResult result, TimeSpan duration, bool cacheHit)
    {
        PlatformResults.Add(
            1,
            new KeyValuePair<string, object?>("platform", result.Platform),
            new KeyValuePair<string, object?>("status", result.Status.ToString()),
            new KeyValuePair<string, object?>("confidence", result.Confidence.ToString()),
            new KeyValuePair<string, object?>("cache_hit", cacheHit));

        PlatformCheckDuration.Record(
            duration.TotalMilliseconds,
            new KeyValuePair<string, object?>("platform", result.Platform),
            new KeyValuePair<string, object?>("status", result.Status.ToString()),
            new KeyValuePair<string, object?>("cache_hit", cacheHit));
    }
}
