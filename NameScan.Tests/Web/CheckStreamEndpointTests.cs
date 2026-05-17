using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NameScan.Features.Checks;
using NameScan.Platforms;

namespace NameScan.Tests.Web;

public sealed class CheckStreamEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CheckStreamEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StreamEndpoint_ReturnsEventStreamContentType()
    {
        var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/check/stream?nickname=!", HttpCompletionOption.ResponseHeadersRead);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("no-cache", response.Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task StreamEndpoint_ReturnsKeepAliveConnectionHeaderForHttp11()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/check/stream?nickname=!")
        {
            Version = HttpVersion.Version11
        };

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("keep-alive", response.Headers.Connection);
    }

    [Fact]
    public async Task StreamEndpoint_DoesNotReturnConnectionHeaderForHttp2()
    {
        var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/check/stream?nickname=!")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Empty(response.Headers.Connection);
    }

    [Fact]
    public async Task StreamEndpoint_ReturnsResultAndDoneEventsForValidNickname()
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPlatformRegistry>();
                services.AddSingleton<IPlatformRegistry>(
                    new FakePlatformRegistry(
                    [
                        new FakePlatformChecker("GitHub", CheckStatus.Available),
                        new FakePlatformChecker("Instagram", CheckStatus.Occupied)
                    ]));
            });
        }).CreateClient();

        var content = await client.GetStringAsync("/api/check/stream?nickname=minhamarca");

        Assert.Contains("event: result", content);
        Assert.Contains("event: done", content);
        Assert.Contains("data:", content);
        Assert.DoesNotContain("event: error", content);
        Assert.Contains("\"GitHub\"", content);
        Assert.True(
            content.LastIndexOf("event: result", StringComparison.Ordinal) <
            content.LastIndexOf("event: done", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StreamEndpoint_ReturnsErrorEventForInvalidNickname()
    {
        var client = _factory.CreateClient();

        var content = await client.GetStringAsync("/api/check/stream?nickname=!");

        Assert.Contains("event: error", content);
        Assert.Contains("data:", content);
        Assert.Contains("Use apenas letras sem acento", content);
    }

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

    [Theory]
    [InlineData("/_content/MudBlazor/MudBlazor.min.js")]
    [InlineData("/js/check-stream.js")]
    public async Task StaticAssets_ServeRequiredJavaScriptFiles(string path)
    {
        var client = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production")).CreateClient();

        using var response = await client.GetAsync(path);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("javascript", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record MetricMeasurement(string Name, double Value, KeyValuePair<string, object?>[] Tags)
    {
        public bool HasTag(string key, string value) =>
            Tags.Any(tag => tag.Key == key && string.Equals(tag.Value?.ToString(), value, StringComparison.Ordinal));
    }
}
