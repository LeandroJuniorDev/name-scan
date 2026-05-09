# NameScan Aspire Stream Observability And Ops Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Finish the next useful Aspire slice by instrumenting the SSE stream lifecycle and documenting the operational flow that now exists in the project.

**Architecture:** Keep `NameScan` as a single product web app. Extend the existing `CheckTelemetry` surface with stream-level metrics and tracing, then wire those signals into `/api/check/stream` without changing the frontend contract. Close the loop with operational documentation in `README.md`, not with extra infrastructure.

**Tech Stack:** .NET 10 SDK, ASP.NET Core, Blazor Web App, xUnit, Aspire AppHost, OpenTelemetry metrics, OpenTelemetry tracing.

---

## File Structure

Expected files touched by this plan:

```text
/NameScan
  /Program.cs
  /Features/Checks
    /CheckTelemetry.cs
/NameScan.Tests
  /Web
    /CheckStreamEndpointTests.cs
/README.md
```

Responsibilities:

- `NameScan/Program.cs`: map the SSE endpoint and emit stream lifecycle logs, metrics, and tracing events.
- `NameScan/Features/Checks/CheckTelemetry.cs`: centralize stream-level telemetry APIs alongside the existing check-level APIs.
- `NameScan.Tests/Web/CheckStreamEndpointTests.cs`: protect the stream contract plus the new observability surface.
- `README.md`: explain how to run the app with Aspire, where observability now lives, and what is and is not covered for deployment.

## Scope Notes

This plan intentionally does **not** add Redis, queues, databases, or extra services. It also does **not** change the SSE payload shape, the UI copy, or the streaming behavior. The remaining Aspire gap after this plan should be mostly editorial or future hosting-specific work.

### Task 1: Instrument The SSE Stream Lifecycle

**Files:**
- Modify: `NameScan.Tests/Web/CheckStreamEndpointTests.cs`
- Modify: `NameScan/Features/Checks/CheckTelemetry.cs`
- Modify: `NameScan/Program.cs`

- [ ] **Step 1: Write the failing stream telemetry test**

Add these members to `NameScan.Tests/Web/CheckStreamEndpointTests.cs`:

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;
```

Add this test below `StreamEndpoint_ReturnsErrorEventForInvalidNickname`:

```csharp
[Fact]
public async Task StreamEndpoint_RecordsStreamTelemetryForInvalidNickname()
{
    var client = _factory.CreateClient();
    var measurements = new List<MetricMeasurement>();
    var activities = new List<Activity>();

    using var meterListener = new MeterListener();
    meterListener.InstrumentPublished = (instrument, listener) =>
    {
        if (instrument.Meter.Name == CheckTelemetry.MeterName)
        {
            listener.EnableMeasurementEvents(instrument);
        }
    };
    meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        measurements.Add(new MetricMeasurement(instrument.Name, measurement, tags.ToArray())));
    meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        measurements.Add(new MetricMeasurement(instrument.Name, measurement, tags.ToArray())));
    meterListener.Start();

    using var activityListener = new ActivityListener
    {
        ShouldListenTo = source => source.Name == CheckTelemetry.ActivitySourceName,
        Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
        ActivityStopped = activity => activities.Add(activity)
    };

    ActivitySource.AddActivityListener(activityListener);

    var content = await client.GetStringAsync("/api/check/stream?nickname=!");

    Assert.Contains("event: error", content);
    Assert.Contains(measurements, item => item.Name == "namescan.streams.started" && item.Value == 1);
    Assert.Contains(
        measurements,
        item => item.Name == "namescan.streams.completed"
            && item.Value == 1
            && item.HasTag("outcome", "validation_error"));
    Assert.Contains(measurements, item => item.Name == "namescan.stream.duration" && item.Value >= 0);
    Assert.Contains(
        activities,
        activity => activity.OperationName == "namescan.stream"
            && activity.GetTagItem("namescan.nickname")?.ToString() == "!"
            && activity.GetTagItem("namescan.outcome")?.ToString() == "validation_error");
}
```

Add this helper record at the bottom of the file:

```csharp
private sealed record MetricMeasurement(string Name, double Value, KeyValuePair<string, object?>[] Tags)
{
    public bool HasTag(string key, string value) =>
        Tags.Any(tag => tag.Key == key && string.Equals(tag.Value?.ToString(), value, StringComparison.Ordinal));
}
```

- [ ] **Step 2: Run the targeted test and verify RED**

Run:

```bash
dotnet test NameScan.sln --filter StreamEndpoint_RecordsStreamTelemetryForInvalidNickname
```

Expected:

- The test fails because `CheckTelemetry` does not yet expose stream counters, histograms, or activities.

- [ ] **Step 3: Add stream telemetry primitives to `CheckTelemetry`**

Update `NameScan/Features/Checks/CheckTelemetry.cs` with these members:

```csharp
private static readonly Counter<long> StreamsStarted = Meter.CreateCounter<long>("namescan.streams.started");
private static readonly Counter<long> StreamsCompleted = Meter.CreateCounter<long>("namescan.streams.completed");
private static readonly Counter<long> StreamBootstrapErrors = Meter.CreateCounter<long>("namescan.stream.bootstrap_errors");
private static readonly Histogram<double> StreamDuration = Meter.CreateHistogram<double>("namescan.stream.duration", unit: "ms");
```

Add these methods near the existing check-level APIs:

```csharp
public static Activity? StartStreamActivity(string nickname, string protocol)
{
    StreamsStarted.Add(1);

    var activity = ActivitySource.StartActivity("namescan.stream", ActivityKind.Server);
    activity?.SetTag("namescan.nickname", nickname);
    activity?.SetTag("namescan.protocol", protocol);
    return activity;
}

public static void RecordStreamCompleted(string outcome, TimeSpan duration, string protocol)
{
    StreamsCompleted.Add(
        1,
        new KeyValuePair<string, object?>("outcome", outcome),
        new KeyValuePair<string, object?>("protocol", protocol));

    StreamDuration.Record(
        duration.TotalMilliseconds,
        new KeyValuePair<string, object?>("outcome", outcome),
        new KeyValuePair<string, object?>("protocol", protocol));
}

public static void RecordStreamBootstrapError(string protocol)
{
    StreamBootstrapErrors.Add(1, new KeyValuePair<string, object?>("protocol", protocol));
}
```

- [ ] **Step 4: Wire the endpoint lifecycle in `Program.cs`**

Replace the current `/api/check/stream` mapping in `NameScan/Program.cs` with this shape:

```csharp
app.MapGet("/api/check/stream", async (
    string? nickname,
    HandleCheckService checkService,
    HttpContext httpContext,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var requestedNickname = nickname ?? string.Empty;
    var protocol = httpContext.Request.Protocol;
    var stopwatch = Stopwatch.StartNew();
    using var streamActivity = CheckTelemetry.StartStreamActivity(requestedNickname, protocol);

    logger.LogInformation(
        "NameScan stream started for nickname {Nickname} over {Protocol}",
        requestedNickname,
        protocol);

    try
    {
        httpContext.Response.Headers.CacheControl = "no-cache";
        if (string.Equals(protocol, "HTTP/1.1", StringComparison.OrdinalIgnoreCase))
        {
            httpContext.Response.Headers.Connection = "keep-alive";
        }

        httpContext.Response.ContentType = "text/event-stream; charset=utf-8";

        await foreach (var streamEvent in checkService.StreamAsync(nickname, cancellationToken))
        {
            var eventName = streamEvent.Kind switch
            {
                CheckStreamEventKind.Result => "result",
                CheckStreamEventKind.Done => "done",
                CheckStreamEventKind.Error => "error",
                _ => "error"
            };

            if (streamEvent.Kind == CheckStreamEventKind.Result && streamEvent.Result is not null)
            {
                logger.LogInformation(
                    "NameScan stream emitted result for {Platform} with status {Status}",
                    streamEvent.Result.Platform,
                    streamEvent.Result.Status);
            }

            if (streamEvent.Kind == CheckStreamEventKind.Error)
            {
                streamActivity?.SetTag("namescan.outcome", "validation_error");
            }

            if (streamEvent.Kind == CheckStreamEventKind.Done)
            {
                streamActivity?.SetTag("namescan.outcome", "completed");
            }

            var json = JsonSerializer.Serialize(streamEvent, JsonOptions);
            await httpContext.Response.WriteAsync($"event: {eventName}\n", cancellationToken);
            await httpContext.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await httpContext.Response.Body.FlushAsync(cancellationToken);
        }

        var outcome = streamActivity?.GetTagItem("namescan.outcome")?.ToString() ?? "completed";
        logger.LogInformation(
            "NameScan stream completed for nickname {Nickname} with outcome {Outcome}",
            requestedNickname,
            outcome);

        CheckTelemetry.RecordStreamCompleted(outcome, stopwatch.Elapsed, protocol);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
        streamActivity?.SetTag("namescan.outcome", "cancelled");
        logger.LogInformation(
            "NameScan stream cancelled for nickname {Nickname}",
            requestedNickname);
        CheckTelemetry.RecordStreamCompleted("cancelled", stopwatch.Elapsed, protocol);
        throw;
    }
    catch (Exception exception)
    {
        streamActivity?.SetTag("namescan.outcome", "bootstrap_error");
        streamActivity?.SetStatus(ActivityStatusCode.Error, exception.Message);
        logger.LogError(
            exception,
            "NameScan stream failed before completion for nickname {Nickname}",
            requestedNickname);
        CheckTelemetry.RecordStreamBootstrapError(protocol);
        CheckTelemetry.RecordStreamCompleted("bootstrap_error", stopwatch.Elapsed, protocol);
        throw;
    }
});
```

Add the missing `using` at the top of `Program.cs`:

```csharp
using System.Diagnostics;
```

- [ ] **Step 5: Run the targeted stream tests and verify GREEN**

Run:

```bash
dotnet test NameScan.sln --filter "StreamEndpoint_RecordsStreamTelemetryForInvalidNickname|CheckStreamEndpointTests"
```

Expected:

- All `CheckStreamEndpointTests` pass.
- The new stream telemetry test passes.

- [ ] **Step 6: Commit the stream instrumentation**

Run:

```bash
git add NameScan/Program.cs NameScan/Features/Checks/CheckTelemetry.cs NameScan.Tests/Web/CheckStreamEndpointTests.cs
git commit -m "feat: instrument SSE stream lifecycle"
```

Expected:

- A commit records the stream-level metrics, tracing, and correlated logs.

### Task 2: Document The Operational Aspire Flow

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Add a short observability section to the README**

Update `README.md` by inserting this section after `## Execução`:

```md
## Observabilidade Com Aspire

Ao subir o projeto por `NameScan.AppHost`, o dashboard do Aspire passa a concentrar:

- métricas do runtime ASP.NET Core;
- health checks de desenvolvimento em `/health` e `/alive`;
- telemetria do fluxo de verificação do NameScan;
- telemetria do ciclo de vida do stream SSE em `/api/check/stream`.

Os sinais de domínio atuais incluem:

- início e conclusão de verificações;
- duração total da verificação;
- duração por plataforma;
- resultados por status e confiança;
- início e conclusão do stream SSE.
```

- [ ] **Step 2: Add a small deployment note without inventing infrastructure**

Append this section near the end of `README.md`:

```md
## Hospedagem E Deploy

Nesta etapa, o projeto está preparado para rodar localmente com Aspire e para evoluir a configuração operacional sem mudar a arquitetura do MVP.

Compromissos atuais:

- `NameScan` continua sendo o único app de produto;
- `NameScan.AppHost` é o ponto preferencial para execução local com observabilidade;
- `NameScan` ainda pode ser executado isoladamente para desenvolvimento focado;
- não há dependências obrigatórias novas de banco, fila ou cache distribuído.

O caminho de deploy futuro deve preservar esse desenho simples e tratar recursos extras como decisões explícitas, não como requisito artificial da adoção do Aspire.
```

- [ ] **Step 3: Review the README diff for scope discipline**

Check:

- The README still describes a single web app.
- No new infrastructure is promised.
- The copy remains in Portuguese.

- [ ] **Step 4: Commit the documentation**

Run:

```bash
git add README.md
git commit -m "docs: document Aspire observability flow"
```

Expected:

- The operational story for Aspire becomes visible without inflating the product scope.

### Task 3: Validate The Slice End-To-End

**Files:**
- Test only: `NameScan.Tests/Web/CheckStreamEndpointTests.cs`
- Test only: `NameScan.Tests/Web/HealthEndpointsTests.cs`
- Test only: `NameScan.Tests/Features/Checks/HandleCheckServiceTests.cs`

- [ ] **Step 1: Run the focused regression suite**

Run:

```bash
dotnet test NameScan.sln --filter "HandleCheckServiceTests|CheckStreamEndpointTests|HealthEndpointsTests"
```

Expected:

- All focused regression tests pass.
- The previous Aspire observability work for `HandleCheckService` remains green.
- Health endpoint behavior stays unchanged.

- [ ] **Step 2: Run a solution build**

Run:

```bash
dotnet build NameScan.sln
```

Expected:

- Build succeeds with no new warnings introduced by the stream instrumentation.

- [ ] **Step 3: Review the final git state**

Run:

```bash
git status --short
```

Expected:

- Only the files from this plan appear as modified before the final publish flow.

- [ ] **Step 4: Publish with the established repo workflow**

Run:

```bash
git push -u origin <branch-name>
gh pr create --draft --base main --head <branch-name> --title "[codex] Instrument Aspire SSE stream lifecycle" --body-file /private/tmp/namescan-stream-pr-body.md
```

Expected:

- The branch is published.
- A draft PR describes the stream lifecycle telemetry and README updates.
