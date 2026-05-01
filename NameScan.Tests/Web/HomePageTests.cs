using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;
using NameScan.Components.Pages;
using NameScan.Features.Checks;
using NameScan.Features.Reporting;
using System.Reflection;
using System.Text.Json;

namespace NameScan.Tests.Web;

public sealed class HomePageTests
{
    [Fact]
    public async Task Home_RendersBrazilFirstSearchExperience()
    {
        await using var context = CreateContext();
        var component = context.Render<Home>();
        var markup = component.Markup;

        Assert.Contains("NameScan", markup);
        Assert.Contains("Verificar", markup);
        Assert.Contains("Apoia.se", markup);
        Assert.Contains("Os resultados são estimativas", markup);
    }

    [Fact]
    public async Task Home_StopsLoadingAndShowsFallbackMessage_WhenStreamErrorsWithoutPayload()
    {
        await using var context = CreateContext();
        var component = context.Render<Home>();

        component.Find("input").Input("minhamarca");
        await component.Find("button").ClickAsync(new MouseEventArgs());

        Assert.Contains("mud-progress-linear", component.Markup);

        await component.InvokeAsync(() => component.Instance.OnStreamError("{}"));

        Assert.DoesNotContain("mud-progress-linear", component.Markup);
        Assert.Equal("Não foi possível concluir a verificação.", GetPrivateField<string?>(component.Instance, "_validationMessage"));
    }

    [Fact]
    public async Task Home_ShowsValidationMessage_WhenNicknameIsMissing()
    {
        await using var context = CreateContext();
        var component = context.Render<Home>();

        await component.Find("button").ClickAsync(new MouseEventArgs());

        Assert.Contains("Digite um nickname para verificar.", component.Markup);
        Assert.DoesNotContain("_validationMessage", component.Markup);
    }

    [Fact]
    public async Task Home_DoesNotRenderOpenLink_WhenResultUrlIsEmpty()
    {
        await using var context = CreateContext();
        var component = context.Render<Home>();
        var payload = JsonSerializer.Serialize(
            CheckStreamEvent.ResultEvent(
                new PlatformCheckResult(
                    "GitHub",
                    string.Empty,
                    CheckStatus.Available,
                    ConfidenceLevel.High,
                    "Disponível")));

        await component.InvokeAsync(() => component.Instance.OnStreamResult(payload));

        Assert.DoesNotContain(">Abrir</a>", component.Markup);
    }

    [Fact]
    public async Task Home_LogsSearchStart_WhenCheckBegins()
    {
        await using var context = CreateContext();
        var logger = new TestLogger<Home>();
        context.Services.AddSingleton<ILogger<Home>>(logger);
        var component = context.Render<Home>();

        component.Find("input").Input("minhamarca");
        await component.Find("button").ClickAsync(new MouseEventArgs());

        Assert.Contains(logger.Entries, entry => entry.Message == "NameScan search started");
    }

    [Fact]
    public async Task Home_ShowsFallbackMessageAndStopsLoading_WhenStreamBootstrapFails()
    {
        await using var context = CreateContext();
        context.Services.AddSingleton<IJSRuntime>(new SelectiveJsRuntime("nameScanStream.start", new JSException("bootstrap failed")));
        var component = context.Render<Home>();

        component.Find("input").Input("minhamarca");
        await component.Find("button").ClickAsync(new MouseEventArgs());

        Assert.DoesNotContain("mud-progress-linear", component.Markup);
        Assert.Equal("Não foi possível iniciar a verificação.", GetPrivateField<string?>(component.Instance, "_validationMessage"));
    }

    [Fact]
    public async Task Home_LogsReportCopy_WhenCopyingReport()
    {
        await using var context = CreateContext();
        var logger = new TestLogger<Home>();
        context.Services.AddSingleton<ILogger<Home>>(logger);
        var component = context.Render<Home>();

        await component.InvokeAsync(() => component.Instance.OnStreamResult(JsonSerializer.Serialize(
            CheckStreamEvent.ResultEvent(
                new PlatformCheckResult(
                    "GitHub",
                    "https://example.com/minhamarca",
                    CheckStatus.Available,
                    ConfidenceLevel.High,
                    "Disponível")))));

        await component.FindAll("button").Single(button => button.TextContent.Contains("Copiar relatório")).ClickAsync(new MouseEventArgs());

        Assert.Contains(logger.Entries, entry => entry.Message == "NameScan report copied");
    }

    [Fact]
    public async Task Home_LogsResultLinkClick_WhenOpenLinkIsClicked()
    {
        await using var context = CreateContext();
        var logger = new TestLogger<Home>();
        context.Services.AddSingleton<ILogger<Home>>(logger);
        var component = context.Render<Home>();

        await component.InvokeAsync(() => component.Instance.OnStreamResult(JsonSerializer.Serialize(
            CheckStreamEvent.ResultEvent(
                new PlatformCheckResult(
                    "GitHub",
                    "https://example.com/minhamarca",
                    CheckStatus.Available,
                    ConfidenceLevel.High,
                    "Disponível")))));

        await component.FindAll("a").Single(link => link.TextContent.Contains("Abrir")).ClickAsync(new MouseEventArgs());

        Assert.Contains(logger.Entries, entry => entry.Message == "NameScan result link clicked GitHub");
    }

    [Fact]
    public async Task Home_LogsSupportClick_WhenApoiaSeLinkIsClicked()
    {
        await using var context = CreateContext();
        var logger = new TestLogger<Home>();
        context.Services.AddSingleton<ILogger<Home>>(logger);
        var component = context.Render<Home>();

        await component.FindAll("a").Single(link => link.TextContent.Contains("Apoia.se")).ClickAsync(new MouseEventArgs());

        Assert.Contains(logger.Entries, entry => entry.Message == "NameScan support link clicked");
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddMudServices();
        context.Services.AddSingleton<ReportFormatter>();
        return context;
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field.GetValue(instance)!;
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message);

    private sealed class SelectiveJsRuntime(string failingIdentifier, Exception exception) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            identifier == failingIdentifier
                ? ValueTask.FromException<TValue>(exception)
                : ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            identifier == failingIdentifier
                ? ValueTask.FromException<TValue>(exception)
                : ValueTask.FromResult(default(TValue)!);
    }
}
