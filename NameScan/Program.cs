using System.Diagnostics;
using System.Text.Json;
using NameScan.Features.Checks;
using NameScan.Features.Reporting;
using NameScan.Features.Suggestions;
using NameScan.Platforms;
using NameScan.Validation;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<NicknameValidator>();
builder.Services.AddSingleton<SuggestionService>();
builder.Services.AddSingleton<ReportFormatter>();
builder.Services.AddSingleton<IPlatformRegistry>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    return new PlatformRegistry(httpClientFactory.CreateClient());
});
builder.Services.AddScoped<HandleCheckService>();

var app = builder.Build();
var JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.MapDefaultEndpoints();

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

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

app.MapRazorComponents<NameScan.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
