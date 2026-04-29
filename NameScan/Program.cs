using System.Text.Json;
using NameScan.Features.Checks;
using NameScan.Features.Reporting;
using NameScan.Features.Suggestions;
using NameScan.Platforms;
using NameScan.Validation;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/api/check/stream", async (
    string? nickname,
    HandleCheckService checkService,
    HttpContext httpContext,
    CancellationToken cancellationToken) =>
{
    httpContext.Response.Headers.CacheControl = "no-cache";
    httpContext.Response.Headers.Connection = "keep-alive";
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

        var json = JsonSerializer.Serialize(streamEvent, JsonOptions);
        await httpContext.Response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await httpContext.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await httpContext.Response.Body.FlushAsync(cancellationToken);
    }
});

app.MapRazorComponents<NameScan.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
